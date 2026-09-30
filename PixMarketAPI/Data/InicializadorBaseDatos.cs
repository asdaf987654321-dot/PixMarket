using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MySqlConnector;
using PixMarketAPI.Seguridad;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace PixMarketAPI.Data
{
    /// <summary>
    /// Crea o actualiza la base de datos de MySQL a partir del modelo de EF Core,
    /// automáticamente, cada vez que arranca la API.
    ///
    /// Antes había que ejecutar el DDL a mano cada vez que se agregaba una
    /// propiedad al modelo. Por eso /api/ventas devolvía 500 con
    /// "Unknown column 'v.Estado'" y /api/configuracion con
    /// "Table 'pixmarket.configuraciones' doesn't exist".
    ///
    /// Qué hace, en orden:
    ///   1. Crea la base de datos si el servidor MySQL no la tiene.
    ///   2. Si la base está vacía, crea TODO el esquema desde el modelo.
    ///   3. Si ya existe, agrega las tablas, columnas, índices y claves
    ///      foráneas que falten, y ensancha las columnas de texto que
    ///      quedaron cortas (p. ej. el modelo pide varchar(100) y hay varchar(20)).
    ///   4. Inserta los datos mínimos para poder usar la app (un administrador
    ///      y la configuración de la tienda) si la base está vacía.
    ///
    /// El modelo de EF Core es la fuente de verdad: si mañana agregas una
    /// propiedad a una entidad, la columna aparece sola en la base, sin DDL
    /// escrito a mano.
    ///
    /// Todo es idempotente y nunca borra ni modifica datos existentes, así que
    /// se puede ejecutar tantas veces como haga falta.
    /// </summary>
    public static class InicializadorBaseDatos
    {
        // Datos mínimos para poder entrar al panel. Solo se insertan si la
        // tabla usuarios está vacía.
        public const string CorreoAdminPorDefecto = "admin@pixmarket.com";
        public const string ContraseniaAdminPorDefecto = "admin123";

        private static readonly Regex IdentificadorValido =
            new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        private static readonly string[] TiposEnteros =
            { "int", "bigint", "smallint", "tinyint", "mediumint" };

        /// <summary>
        /// Punto de entrada que usa Program.cs al arrancar la API.
        /// </summary>
        public static void Aplicar(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();

            var cambios = Aplicar(scope.ServiceProvider.GetRequiredService<PixContext>(), logger);

            if (cambios.Count == 0)
            {
                logger.LogInformation(
                    "MySQL: el esquema ya estaba actualizado con el modelo.");
            }
            else
            {
                logger.LogInformation(
                    "MySQL: {Cantidad} cambio(s) aplicado(s) automáticamente.",
                    cambios.Count);

                foreach (var cambio in cambios)
                {
                    logger.LogInformation("   + {Cambio}", cambio);
                }
            }
        }

        /// <summary>
        /// Crea la base de datos si el servidor MySQL no la tiene.
        ///
        /// Se llama antes de registrar el DbContext porque MySQL rechaza la
        /// conexión si se intenta seleccionar una base que todavía no existe.
        /// </summary>
        public static void AsegurarBaseDeDatos(string? cadena)
        {
            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    "No está configurada la cadena de conexión \"DefaultConnection\".");
            }

            var nombreBase = new MySqlConnectionStringBuilder(cadena).Database;

            if (string.IsNullOrWhiteSpace(nombreBase) ||
                !IdentificadorValido.IsMatch(nombreBase))
            {
                throw new InvalidOperationException(
                    $"El nombre de base de datos \"{nombreBase}\" no es válido.");
            }

            // Se conecta SIN base de datos: si la base no existe, MySQL rechaza
            // cualquier conexión que intente seleccionarla.
            var sinBase = new MySqlConnectionStringBuilder(cadena)
            {
                Database = string.Empty
            };

            using var conexion = new MySqlConnection(sinBase.ConnectionString);
            conexion.Open();

            using var comando = conexion.CreateCommand();
            comando.CommandText =
                $"CREATE DATABASE IF NOT EXISTS `{nombreBase}` " +
                "CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";

            comando.ExecuteNonQuery();
        }

        /// <summary>
        /// Sincroniza el esquema y los datos mínimos. Devuelve la lista de
        /// cambios aplicados (vacía si no hubo nada que hacer).
        /// </summary>
        public static IList<string> Aplicar(PixContext context, ILogger logger)
        {
            var cadena = context.Database.GetConnectionString();

            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    "No está configurada la cadena de conexión \"DefaultConnection\".");
            }

            // 1) Crear la base si el servidor MySQL está vacío.
            AsegurarBaseDeDatos(cadena);

            // Solo el nombre: la cadena de conexión contiene la contraseña y no
            // debe terminar en el registro de la aplicación.
            logger.LogInformation(
                "MySQL: base de datos \"{Base}\" verificada.",
                new MySqlConnectionStringBuilder(cadena).Database);

            var cambios = new List<string>();

            using var conexion = new MySqlConnection(cadena);
            conexion.Open();

            using var comando = conexion.CreateCommand();

            if (TieneTablas(comando))
            {
                // 3) La base ya existe: agregar solo lo que falte.
                cambios.AddRange(SincronizarEsquema(context, comando, logger));
            }
            else
            {
                // 2) Base vacía: EF Core crea todo el esquema desde el modelo.
                context.Database.EnsureCreated();

                cambios.Add("esquema completo creado desde el modelo de EF Core");
            }

            // 4) Datos mínimos para poder usar la aplicación.
            cambios.AddRange(SembrarDatosIniciales(conexion, comando, logger));

            return cambios;
        }

        // =====================================================
        // ESQUEMA
        // =====================================================

        private static IList<string> SincronizarEsquema(
            PixContext context,
            DbCommand comando,
            ILogger logger)
        {
            var cambios = new List<string>();

            var modelo = context.Model.GetRelationalModel();
            var claves = LeerClavesForaneas(context);

            foreach (var tabla in OrdenarPorDependencias(modelo.Tables, claves))
            {
                if (!ExisteTabla(comando, tabla.Name))
                {
                    CrearTabla(comando, tabla, claves, logger);
                    cambios.Add($"tabla creada: {tabla.Name}");
                    continue;
                }

                cambios.AddRange(SincronizarTabla(comando, tabla, claves, logger));
            }

            return cambios;
        }

        private static IList<string> SincronizarTabla(
            DbCommand comando,
            ITable tabla,
            List<ClaveForanea> claves,
            ILogger logger)
        {
            var cambios = new List<string>();

            // El modelo llama a las tablas "Ventas" pero la base puede tenerlas
            // en minúsculas ("ventas"). En Linux MySQL los nombres de tabla
            // distinguen mayúsculas, así que el DDL tiene que usar el nombre
            // real que ya está en la base.
            var nombre = NombreRealDeTabla(comando, tabla.Name) ?? tabla.Name;

            var existentes = LeerColumnas(comando, tabla.Name);

            // Columnas.
            foreach (var columna in tabla.Columns)
            {
                if (!existentes.TryGetValue(columna.Name, out var actual))
                {
                    Ejecutar(
                        comando,
                        $"ALTER TABLE `{nombre}` ADD COLUMN {DefinirColumna(columna)};",
                        logger);

                    cambios.Add($"columna agregada: {nombre}.{columna.Name} {columna.StoreType}");
                    continue;
                }

                // La columna existe pero quedó más corta que lo que pide el
                // modelo (por ejemplo varchar(20) cuando el modelo dice 100).
                // Ensanchar nunca pierde datos.
                if (columna.MaxLength.HasValue &&
                    actual.Longitud.HasValue &&
                    columna.MaxLength.Value > actual.Longitud.Value)
                {
                    var nulabilidad = actual.Nulo ? "NULL" : "NOT NULL";

                    Ejecutar(
                        comando,
                        $"ALTER TABLE `{nombre}` MODIFY COLUMN " +
                        $"`{columna.Name}` {columna.StoreType} {nulabilidad};",
                        logger);

                    cambios.Add(
                        $"columna ensanchada: {nombre}.{columna.Name} " +
                        $"{actual.Columna} -> {columna.StoreType}");
                }
            }

            cambios.AddRange(SincronizarClaves(comando, tabla, nombre, claves, logger));

            return cambios;
        }

        private static IList<string> SincronizarClaves(
            DbCommand comando,
            ITable tabla,
            string nombre,
            List<ClaveForanea> claves,
            ILogger logger)
        {
            var cambios = new List<string>();

            // Clave primaria. En MySQL el índice de la clave primaria siempre se
            // llama "PRIMARY", no el nombre que le da EF al modelo (PK_tabla),
            // así que se busca por tipo de restricción.
            if (tabla.PrimaryKey is { } clavePrimaria &&
                !ExisteClavePrimaria(comando, tabla.Name))
            {
                var columnas = string.Join(", ", clavePrimaria.Columns.Select(c => $"`{c.Name}`"));

                Ejecutar(
                    comando,
                    $"ALTER TABLE `{nombre}` ADD PRIMARY KEY ({columnas});",
                    logger);

                cambios.Add($"clave primaria: {nombre} ({columnas})");
            }

            // Índices.
            foreach (var indice in tabla.Indexes)
            {
                if (ExisteIndice(comando, tabla.Name, indice.Name))
                {
                    continue;
                }

                var columnas = string.Join(", ", indice.Columns.Select(c => $"`{c.Name}`"));
                var unico = indice.IsUnique ? "UNIQUE " : string.Empty;

                try
                {
                    Ejecutar(
                        comando,
                        $"ALTER TABLE `{nombre}` ADD {unico}INDEX `{indice.Name}` ({columnas});",
                        logger);

                    cambios.Add($"índice: {indice.Name} en {nombre} ({columnas})");
                }
                catch (MySqlException ex)
                {
                    logger.LogWarning(
                        ex,
                        "No se pudo crear el índice {Indice} en {Tabla}. " +
                        "La API sigue funcionando sin él.",
                        indice.Name,
                        nombre);
                }
            }

            // Claves foráneas. Se comparan por columnas, no por nombre, porque
            // el nombre que usa EF no coincide con el de la base.
            foreach (var clave in claves.Where(c => c.Tabla == tabla.Name))
            {
                if (ColumnasYaForaneas(comando, tabla.Name).Contains(clave.Columnas[0]))
                {
                    continue;
                }

                // La tabla principal tiene que existir antes de referenciarla.
                if (!ExisteTabla(comando, clave.TablaPrincipal))
                {
                    continue;
                }

                var principal = NombreRealDeTabla(comando, clave.TablaPrincipal)
                    ?? clave.TablaPrincipal;

                try
                {
                    Ejecutar(
                        comando,
                        $"ALTER TABLE `{nombre}` ADD CONSTRAINT `{clave.Nombre}` " +
                        $"FOREIGN KEY ({ColumnasDe(clave.Columnas)}) " +
                        $"REFERENCES `{principal}` ({ColumnasDe(clave.ColumnasPrincipales)})" +
                        AccionDeBorrado(clave.Comportamiento) + ";",
                        logger);

                    cambios.Add(
                        $"clave foránea: {nombre}.{ColumnasDe(clave.Columnas)} -> {principal}");
                }
                catch (MySqlException ex)
                {
                    // La integridad referencial es una mejora opcional: si hay
                    // datos huérfanos la base no la acepta, pero la API sigue
                    // funcionando sin ella.
                    logger.LogWarning(
                        ex,
                        "No se pudo crear la clave foránea {Clave} en {Tabla}. " +
                        "Seguramente hay datos huérfanos; la API funciona igual.",
                        clave.Nombre,
                        nombre);
                }
            }

            return cambios;
        }

        private static void CrearTabla(
            DbCommand comando,
            ITable tabla,
            List<ClaveForanea> claves,
            ILogger logger)
        {
            var partes = new List<string>();

            foreach (var columna in tabla.Columns)
            {
                partes.Add($"  {DefinirColumna(columna)}{EsAutoincremento(tabla, columna)}");
            }

            if (tabla.PrimaryKey is { } clavePrimaria)
            {
                partes.Add($"  PRIMARY KEY ({ColumnasDe(clavePrimaria.Columns.Select(c => c.Name))})");
            }

            foreach (var indice in tabla.Indexes)
            {
                var unico = indice.IsUnique ? "UNIQUE " : string.Empty;
                partes.Add($"  {unico}KEY `{indice.Name}` ({ColumnasDe(indice.Columns.Select(c => c.Name))})");
            }

            foreach (var clave in claves.Where(c => c.Tabla == tabla.Name))
            {
                partes.Add(
                    $"  CONSTRAINT `{clave.Nombre}` " +
                    $"FOREIGN KEY ({ColumnasDe(clave.Columnas)}) " +
                    $"REFERENCES `{clave.TablaPrincipal}` ({ColumnasDe(clave.ColumnasPrincipales)})" +
                    AccionDeBorrado(clave.Comportamiento));
            }

            Ejecutar(
                comando,
                $"CREATE TABLE `{tabla.Name}` (\n{string.Join(",\n", partes)}\n) " +
                "ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;",
                logger);
        }

        /// <summary>
        /// MySQL no admite "ADD COLUMN IF NOT EXISTS", así que el orden importa:
        /// primero las tablas de las que dependen las demás (usuarios antes que
        /// ventas) y después las que las referencian.
        /// </summary>
        private static List<ITable> OrdenarPorDependencias(
            IEnumerable<ITable> tablas,
            List<ClaveForanea> claves)
        {
            var pendientes = tablas.ToList();
            var ordenadas = new List<ITable>();

            while (pendientes.Count > 0)
            {
                var candidatas = pendientes
                    .Where(t => claves
                        .Where(c => c.Tabla == t.Name)
                        .All(c => c.TablaPrincipal == t.Name ||
                                  ordenadas.Any(o => o.Name == c.TablaPrincipal)))
                    .ToList();

                if (candidatas.Count == 0)
                {
                    // Referencia circular: se agrega el resto en cualquier orden.
                    candidatas = pendientes.Take(1).ToList();
                }

                foreach (var tabla in candidatas)
                {
                    ordenadas.Add(tabla);
                    pendientes.Remove(tabla);
                }
            }

            return ordenadas;
        }

        /// <summary>
        /// Las claves foráneas se leen del modelo de entidades (y no de
        /// <see cref="ITable.ForeignKeyConstraints"/>) porque ahí está el
        /// <see cref="DeleteBehavior"/>, que el modelo relacional no expone.
        /// </summary>
        private static List<ClaveForanea> LeerClavesForaneas(PixContext context)
        {
            var claves = new List<ClaveForanea>();

            foreach (var entidad in context.Model.GetEntityTypes())
            {
                var tabla = entidad.GetTableName();

                if (string.IsNullOrWhiteSpace(tabla))
                {
                    continue;
                }

                foreach (var foranea in entidad.GetForeignKeys())
                {
                    var columnas = foranea.Properties
                        .Select(p => p.GetColumnName())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToArray();

                    var principales = foranea.PrincipalKey.Properties
                        .Select(p => p.GetColumnName())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToArray();

                    if (columnas.Length == 0 || principales.Length == 0)
                    {
                        continue;
                    }

                    var tablaPrincipal = foranea.PrincipalEntityType.GetTableName();

                    claves.Add(new ClaveForanea(
                        Nombre: $"FK_{tabla}_{tablaPrincipal}_{string.Join("_", columnas)}",
                        Tabla: tabla,
                        Columnas: columnas,
                        TablaPrincipal: tablaPrincipal!,
                        ColumnasPrincipales: principales,
                        Comportamiento: foranea.DeleteBehavior));
                }
            }

            return claves;
        }

        private static string DefinirColumna(IColumn columna)
            => columna.IsNullable
                ? $"`{columna.Name}` {columna.StoreType} NULL"
                : $"`{columna.Name}` {columna.StoreType} NOT NULL";

        private static string EsAutoincremento(ITable tabla, IColumn columna)
        {
            var esEntero = TiposEnteros.Any(t =>
                columna.StoreType.StartsWith(t, StringComparison.OrdinalIgnoreCase));

            return tabla.PrimaryKey is { } clave &&
                   clave.Columns.Count == 1 &&
                   clave.Columns[0].Name == columna.Name &&
                   esEntero
                ? " AUTO_INCREMENT"
                : string.Empty;
        }

        private static string AccionDeBorrado(DeleteBehavior comportamiento) => comportamiento switch
        {
            DeleteBehavior.Cascade => " ON DELETE CASCADE",
            DeleteBehavior.SetNull => " ON DELETE SET NULL",
            DeleteBehavior.Restrict or DeleteBehavior.NoAction => " ON DELETE RESTRICT",
            _ => string.Empty
        };

        private static string ColumnasDe(IEnumerable<string> columnas)
            => string.Join(", ", columnas.Select(c => $"`{c}`"));

        // =====================================================
        // DATOS MÍNIMOS
        // =====================================================

        private static IList<string> SembrarDatosIniciales(
            MySqlConnection conexion,
            DbCommand comando,
            ILogger logger)
        {
            var cambios = new List<string>();

            // Administrador por defecto: sin él no se puede entrar al panel.
            if (Contar(comando, "usuarios") == 0)
            {
                var tablaUsuarios = NombreRealDeTabla(comando, "usuarios") ?? "usuarios";

                using var insertarAdmin = conexion.CreateCommand();
                insertarAdmin.CommandText = $@"
INSERT INTO `{tablaUsuarios}`
    (`Nombre`, `Correo`, `Contrasenia`, `Rol`, `Telefono`, `Estado`, `FechaRegistro`)
VALUES
    (@nombre, @correo, @contrasenia, 'Administrador', '00000000', 'Activo', NOW());";

                insertarAdmin.Parameters.Add(Parametro(insertarAdmin, "@nombre", "Administrador"));
                insertarAdmin.Parameters.Add(Parametro(insertarAdmin, "@correo", CorreoAdminPorDefecto));

                // Se guarda el hash, no la contraseña: en la base de datos
                // tampoco debe quedar en texto plano la clave inicial.
                insertarAdmin.Parameters.Add(
                    Parametro(insertarAdmin, "@contrasenia", Contrasena.Hashear(ContraseniaAdminPorDefecto)));

                insertarAdmin.ExecuteNonQuery();

                cambios.Add($"usuario administrador creado: {CorreoAdminPorDefecto}");

                logger.LogWarning(
                    "La tabla usuarios estaba vacía: se creó el administrador por defecto " +
                    "({Correo} / {Contrasenia}). Cambia esa contraseña antes de publicar el proyecto.",
                    CorreoAdminPorDefecto,
                    ContraseniaAdminPorDefecto);
            }

            // Configuración de la tienda: la pantalla Configuración la lee siempre.
            if (ExisteTabla(comando, "configuraciones") && Contar(comando, "configuraciones") == 0)
            {
                var tablaConfig = NombreRealDeTabla(comando, "configuraciones") ?? "configuraciones";

                using var insertarConfig = conexion.CreateCommand();
                insertarConfig.CommandText = $@"
INSERT INTO `{tablaConfig}`
    (`NombreTienda`, `CorreoContacto`, `Telefono`, `Moneda`, `Direccion`, `Horario`,
     `NotificarStockBajo`, `AvisosNuevosPedidos`, `TiendaPausada`)
VALUES
    ('Block du Booster', NULL, NULL, 'Bs', NULL, NULL, 1, 1, 0);";

                insertarConfig.ExecuteNonQuery();

                cambios.Add("configuración inicial de la tienda creada");
            }

            // Catálogo inicial de cartas: solo se inserta si la tabla items está
            // vacía (igual que el administrador y la configuración). Así la tienda
            // y la página de inicio ya tienen contenido la primera vez que arranca.
            if (ExisteTabla(comando, "items") && Contar(comando, "items") == 0)
            {
                var tablaItems = NombreRealDeTabla(comando, "items") ?? "items";

                foreach (var carta in CatalogoInicial)
                {
                    using var insertarCarta = conexion.CreateCommand();
                    insertarCarta.CommandText = $@"
INSERT INTO `{tablaItems}`
    (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
VALUES
    (@nombre, @juego, @categoria, @rareza, @precio, @stock, @imagen);";

                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@nombre", carta.Nombre));
                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@juego", carta.Juego));
                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@categoria", carta.Categoria));
                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@rareza", carta.Rareza));
                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@precio", carta.Precio));
                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@stock", carta.Stock));
                    insertarCarta.Parameters.Add(Parametro(insertarCarta, "@imagen", carta.ImagenRuta));

                    insertarCarta.ExecuteNonQuery();
                }

                cambios.Add($"catálogo inicial de {CatalogoInicial.Count} cartas creado");
            }

            return cambios;
        }

        // =====================================================
        // AUXILIARES
        // =====================================================

        private static void Ejecutar(DbCommand comando, string sql, ILogger logger)
        {
            comando.CommandText = sql;
            comando.ExecuteNonQuery();

            logger.LogDebug("MySQL: {Sql}", sql);
        }

        private static DbParameter Parametro(DbCommand comando, string nombre, object valor)
        {
            var parametro = comando.CreateParameter();
            parametro.ParameterName = nombre;
            parametro.Value = valor;

            return parametro;
        }

        private static long Contar(DbCommand comando, string tabla)
        {
            // La tabla viene del propio inicializador, no de entrada externa.
            var nombre = NombreRealDeTabla(comando, tabla) ?? tabla;

            comando.CommandText = $"SELECT COUNT(*) FROM `{nombre}`;";
            return Convert.ToInt64(comando.ExecuteScalar());
        }

        private static bool TieneTablas(DbCommand comando)
        {
            comando.CommandText = @"
SELECT COUNT(*) FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE();";

            return Convert.ToInt32(comando.ExecuteScalar()) > 0;
        }

        private static bool ExisteTabla(DbCommand comando, string tabla)
        {
            // LOWER() en los dos lados: el modelo llama "Ventas" a la tabla y la
            // base puede tenerla como "ventas".
            comando.CommandText = @"
SELECT COUNT(*) FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tabla);";

            comando.Parameters.Clear();
            comando.Parameters.Add(Parametro(comando, "@tabla", tabla));

            return Convert.ToInt32(comando.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Devuelve el nombre con el que la tabla está realmente guardada. El
        /// modelo dice "Ventas" pero la base puede tenerla como "ventas", y en
        /// Linux MySQL el DDL distingue mayúsculas de minúsculas.
        /// </summary>
        private static string? NombreRealDeTabla(DbCommand comando, string tabla)
        {
            comando.CommandText = @"
SELECT TABLE_NAME FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tabla)
LIMIT 1;";

            comando.Parameters.Clear();
            comando.Parameters.Add(Parametro(comando, "@tabla", tabla));

            return comando.ExecuteScalar() as string;
        }

        private static bool ExisteClavePrimaria(DbCommand comando, string tabla)
        {
            comando.CommandText = @"
SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tabla)
  AND CONSTRAINT_TYPE = 'PRIMARY KEY';";

            comando.Parameters.Clear();
            comando.Parameters.Add(Parametro(comando, "@tabla", tabla));

            return Convert.ToInt32(comando.ExecuteScalar()) > 0;
        }

        private static bool ExisteIndice(DbCommand comando, string tabla, string indice)
        {
            // Cubre claves primarias, foráneas, índices y restricciones únicas:
            // MySQL los guarda a todos en information_schema.STATISTICS.
            comando.CommandText = @"
SELECT COUNT(*) FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tabla)
  AND LOWER(INDEX_NAME) = LOWER(@indice);";

            comando.Parameters.Clear();
            comando.Parameters.Add(Parametro(comando, "@tabla", tabla));
            comando.Parameters.Add(Parametro(comando, "@indice", indice));

            return Convert.ToInt32(comando.ExecuteScalar()) > 0;
        }

        private static HashSet<string> ColumnasYaForaneas(DbCommand comando, string tabla)
        {
            comando.CommandText = @"
SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tabla)
  AND REFERENCED_TABLE_NAME IS NOT NULL;";

            comando.Parameters.Clear();
            comando.Parameters.Add(Parametro(comando, "@tabla", tabla));

            var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                columnas.Add(lector.GetString(0));
            }

            return columnas;
        }

        private static Dictionary<string, ColumnaSql> LeerColumnas(DbCommand comando, string tabla)
        {
            comando.CommandText = @"
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tabla);";

            comando.Parameters.Clear();
            comando.Parameters.Add(Parametro(comando, "@tabla", tabla));

            var columnas = new Dictionary<string, ColumnaSql>(StringComparer.OrdinalIgnoreCase);

            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                var nombre = lector.GetString(0);

                columnas[nombre] = new ColumnaSql(
                    lector.GetString(1),
                    lector.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase),
                    lector.IsDBNull(3) ? null : Convert.ToInt64(lector.GetValue(3)));
            }

            return columnas;
        }

        private sealed record ColumnaSql(string Columna, bool Nulo, long? Longitud);

        private sealed record ClaveForanea(
            string Nombre,
            string Tabla,
            string[] Columnas,
            string TablaPrincipal,
            string[] ColumnasPrincipales,
            DeleteBehavior Comportamiento);

        // =====================================================
        // CATÁLOGO INICIAL DE CARTAS
        // =====================================================

        /// <summary>
        /// Cartas que se insertan la primera vez que la base queda sin productos.
        /// Las rutas de imagen apuntan a archivos ya subidos en
        /// PixMarketAPI/wwwroot/Imagenes.
        /// </summary>
        private static readonly List<ItemSemilla> CatalogoInicial = new()
        {
            // Yu-Gi-Oh!
            new("Blue-Eyes White Dragon", "Yu-Gi-Oh!", "Monstruo", "Ultra Rare", 320m, 25, "/Imagenes/item-debc8305c316497f9c329e111887879c.jpeg"),
            new("Dark Magician", "Yu-Gi-Oh!", "Monstruo", "Ultra Rare", 280m, 20, "/Imagenes/item-d80d0f13ab4e4b5993e8490843ec0160.png"),
            new("Monstruo Renacido", "Yu-Gi-Oh!", "Trampa", "Super Rare", 120m, 30, "/Imagenes/item-7be6912c7a294c4cb78c8f4747d9e0af.jpeg"),
            new("Ciber Dragón", "Yu-Gi-Oh!", "Monstruo", "Rare", 90m, 40, "/Imagenes/item-678e48add7f841dab2ceefb8f2f535f4.png"),
            new("Dragón Rojo de Ojos Oscuros", "Yu-Gi-Oh!", "Monstruo", "Secret Rare", 540m, 12, "/Imagenes/item-debc8305c316497f9c329e111887879c.jpeg"),

            // Pokémon
            new("Pikachu ex", "Pokémon", "Monstruo", "Ultra Rare", 260m, 15, "/Imagenes/item-436c7700f0924f1687f59ea4c57a844a.jpg"),
            new("Charizard ex", "Pokémon", "Monstruo", "Secret Rare", 450m, 10, "/Imagenes/item-2fc3ca16854c4d8bb5b6721f52da7ca4.png"),
            new("Mewtwo V", "Pokémon", "Monstruo", "Super Rare", 200m, 12, "/Imagenes/item-debc8305c316497f9c329e111887879c.jpeg"),
            new("Zapdos ex", "Pokémon", "Monstruo", "Ultra Rare", 240m, 14, "/Imagenes/item-678e48add7f841dab2ceefb8f2f535f4.png"),
            new("Energía de Fuego", "Pokémon", "Entrenador", "Common", 15m, 80, "/Imagenes/item-d80d0f13ab4e4b5993e8490843ec0160.png"),

            // Magic: The Gathering
            new("Black Lotus", "Magic: The Gathering", "Otros", "Rare", 950m, 5, "/Imagenes/item-436c7700f0924f1687f59ea4c57a844a.jpg"),
            new("Ajani, Mentor de Héroes", "Magic: The Gathering", "Hechizo", "Super Rare", 330m, 8, "/Imagenes/item-2fc3ca16854c4d8bb5b6721f52da7ca4.png"),
            new("Jace, el Escultor de Mentes", "Magic: The Gathering", "Hechizo", "Ultra Rare", 400m, 6, "/Imagenes/item-d80d0f13ab4e4b5993e8490843ec0160.png"),
            new("Bola de Fuego", "Magic: The Gathering", "Hechizo", "Common", 45m, 60, "/Imagenes/item-7be6912c7a294c4cb78c8f4747d9e0af.jpeg")
        };

        private sealed record ItemSemilla(
            string Nombre,
            string Juego,
            string Categoria,
            string Rareza,
            decimal Precio,
            int Stock,
            string ImagenRuta);
    }
}
