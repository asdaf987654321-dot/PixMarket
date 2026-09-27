using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PixMarketAPI.Data;
using PixMarketAPI.Seguridad;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PixMarket API",
        Version = "v1",
        Description = "API REST de Block du Booster: catálogo de cartas, usuarios, ventas e imágenes. " +
            "Interfaz interactiva (Try it out) para probar cada endpoint.",
        Contact = new OpenApiContact
        {
            Name = "Block du Booster",
            Url = new Uri("http://localhost:5029")
        },
        License = new OpenApiLicense
        {
            Name = "Uso interno / académico"
        }
    });

    // Incluye los comentarios XML de los controladores como descripciones.
    var archivoXml = Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

    if (System.IO.File.Exists(archivoXml))
    {
        c.IncludeXmlComments(archivoXml);
    }
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// La base de datos se crea antes de registrar el DbContext: MySQL rechaza la
// conexión si se intenta seleccionar una base que todavía no existe.
try
{
    InicializadorBaseDatos.AsegurarBaseDeDatos(connectionString);
}
catch (Exception ex)
{
    Console.WriteLine(
        $"[Aviso] No se pudo verificar la base de datos al arrancar: {ex.Message}");
}

// ServerVersion.AutoDetect abre una conexión; si MySQL está caído se usa la
// versión 8.0 declarada para que la API pueda iniciar igual y avisar después.
var versionMySql = DetectarVersionMySql(connectionString);

builder.Services.AddDbContext<PixContext>(options =>
    options.UseMySql(connectionString, versionMySql));

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// El binding de formularios usa SIEMPRE la cultura invariante,
// para que los decimales se envíen con punto ("12.50").
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(CultureInfo.InvariantCulture);
    options.SupportedCultures = new[] { CultureInfo.InvariantCulture };
    options.SupportedUICultures = new[] { CultureInfo.InvariantCulture };
});

var app = builder.Build();

// Asegurar la carpeta de imágenes subidas (wwwroot/Imagenes)
var carpetaImagenes = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "Imagenes");
Directory.CreateDirectory(carpetaImagenes);

// Modo línea de comandos: "--volcar-esquema" imprime el CREATE TABLE completo
// que genera el modelo, para regenerar Scripts/crear-bd.sql. Va antes del
// inicializador porque no necesita conectarse a la base de datos, y así la
// salida no se mezcla con los registros de la aplicación.
if (args.Contains("--volcar-esquema", StringComparer.OrdinalIgnoreCase))
{
    using var scopeVolcado = app.Services.CreateScope();
    var contextoVolcado = scopeVolcado.ServiceProvider.GetRequiredService<PixContext>();

    Console.WriteLine(contextoVolcado.Database.GenerateCreateScript());
    return;
}

// Crear o actualizar la base de datos desde el modelo de EF Core: tablas,
// columnas, índices, claves foráneas y los datos mínimos (administrador y
// configuración). Es idempotente, así que no hay que ejecutar nada a mano
// cuando se agrega una propiedad al modelo.
try
{
    InicializadorBaseDatos.Aplicar(app.Services, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogWarning(
        ex,
        "No se pudo verificar el esquema de MySQL. Revisa el servicio de base de datos.");
}

// Modo línea de comandos: "dotnet run -- --actualizar-bd" crea o actualiza la
// base y termina, sin levantar la API. Lo usa Scripts/actualizar-bd.ps1.
if (args.Contains("--actualizar-bd", StringComparer.OrdinalIgnoreCase))
{
    app.Logger.LogInformation("Esquema sincronizado. Cerrando (--actualizar-bd).");
    return;
}

// Modo línea de comandos: "--cifrar-contrasenas" convierte a hash las
// contraseñas que quedaron guardadas en texto plano antes de este cambio.
// Quien no haya entrado nunca con esas cuentas sigue entrando igual: al
// succeeder su primer inicio de sesión se le cifra automáticamente.
if (args.Contains("--cifrar-contrasenas", StringComparer.OrdinalIgnoreCase))
{
    using var scopeCifrado = app.Services.CreateScope();
    var contextoCifrado = scopeCifrado.ServiceProvider.GetRequiredService<PixContext>();

    var pendientes = await contextoCifrado.Usuarios
        .Where(u => u.Contrasenia != null && !u.Contrasenia.StartsWith("PBKDF2$"))
        .ToListAsync();

    foreach (var usuario in pendientes)
    {
        usuario.Contrasenia = Contrasena.Hashear(usuario.Contrasenia!);
    }

    await contextoCifrado.SaveChangesAsync();

    app.Logger.LogInformation(
        "Contraseñas cifradas: {Cantidad}. Las que ya estaban cifradas no se tocan.",
        pendientes.Count);

    return;
}

// Swagger habilitado SIEMPRE (no solo en Development):
// la API es de uso interno (sin autenticación) y la interfaz
// "Try it out" se usa para probar los endpoints.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PixMarket API v1");
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
});

app.UseRequestLocalization();

app.UseStaticFiles();

app.UseCors("PermitirTodo");

app.MapControllers();

app.Run();

// Detecta la versión del servidor MySQL. Si no se puede (MySQL apagado, base
// de datos recién creada sin permiso, etc.) se usa MySQL 8.0 como valor por
// defecto, que es la versión del proyecto.
static ServerVersion DetectarVersionMySql(string? cadenaConexion)
{
    var porDefecto = new MySqlServerVersion(new Version(8, 0, 0));

    if (string.IsNullOrWhiteSpace(cadenaConexion))
    {
        return porDefecto;
    }

    try
    {
        return ServerVersion.AutoDetect(cadenaConexion);
    }
    catch
    {
        return porDefecto;
    }
}
