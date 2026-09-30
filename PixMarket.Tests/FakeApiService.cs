using Microsoft.AspNetCore.Http;
using PixMarket.Models;
using PixMarket.Servicios;
using PixMarketAPI.Seguridad;

namespace PixMarket.Tests;

public class FakeApiService : IPixMarketApiService
{
    public List<Item> Items { get; set; } = new List<Item>();

    public List<UsuarioPrueba> Usuarios { get; set; } = new List<UsuarioPrueba>();

    public List<VentaPrueba> Ventas { get; set; } = new List<VentaPrueba>();

    public Configuracion Configuracion { get; set; } = new Configuracion
    {
        NombreTienda = "Block du Booster",
        CorreoContacto = "contacto@blockdbooster.com",
        Telefono = "(591) 4-447-1234",
        Moneda = "Bs",
        Direccion = "Calle Heroínas #245, Cochabamba, Bolivia",
        Horario = "Lunes a viernes de 9:00 a 18:00"
    };

    // Últimos parámetros recibidos por la tienda (para asserts)
    public string? UltimoBuscar { get; private set; }
    public string? UltimoJuego { get; private set; }
    public string[]? UltimosJuegos { get; private set; }
    public string[]? UltimasCategorias { get; private set; }
    public string[]? UltimasRarezas { get; private set; }
    public decimal? UltimoPrecioMin { get; private set; }
    public decimal? UltimoPrecioMax { get; private set; }

    public bool ApiIndisponible { get; set; }

    private void VerificarDisponibilidad()
    {
        if (ApiIndisponible)
        {
            throw new HttpRequestException("API no disponible.");
        }
    }

    public Task<TiendaResultadoDto> ObtenerTiendaAsync(
        string? buscar,
        string? juego,
        string[]? juegos,
        string[]? categorias,
        string[]? rarezas,
        decimal? precioMin,
        decimal? precioMax)
    {
        VerificarDisponibilidad();

        UltimoBuscar = buscar;
        UltimoJuego = juego;
        UltimosJuegos = juegos;
        UltimasCategorias = categorias;
        UltimasRarezas = rarezas;
        UltimoPrecioMin = precioMin;
        UltimoPrecioMax = precioMax;

        var query = Items.AsQueryable();

        var termino = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        if (termino != null)
        {
            query = query.Where(i =>
                (i.Nombre != null && i.Nombre.Contains(termino)) ||
                (i.Juego != null && i.Juego.Contains(termino)) ||
                (i.Categoria != null && i.Categoria.Contains(termino)) ||
                (i.Rareza != null && i.Rareza.Contains(termino)));
        }

        var filtroJuegos = new List<string>();

        if (!string.IsNullOrWhiteSpace(juego) && juego.Trim() == "Otros")
        {
            query = query.Where(i =>
                i.Juego != "Yu-Gi-Oh!" &&
                i.Juego != "Pokémon" &&
                i.Juego != "Magic: The Gathering");
        }
        else if (!string.IsNullOrWhiteSpace(juego))
        {
            filtroJuegos.Add(juego.Trim());
        }
        else if (juegos != null)
        {
            filtroJuegos.AddRange(juegos
                .Where(j => !string.IsNullOrWhiteSpace(j))
                .Select(j => j.Trim())
                .Distinct());
        }

        if (filtroJuegos.Any())
        {
            query = query.Where(i => i.Juego != null && filtroJuegos.Contains(i.Juego));
        }

        var filtroCategorias = (categorias ?? Array.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct()
            .ToList();

        if (filtroCategorias.Any())
        {
            query = query.Where(i => i.Categoria != null && filtroCategorias.Contains(i.Categoria));
        }

        var filtroRarezas = (rarezas ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct()
            .ToList();

        if (filtroRarezas.Any())
        {
            query = query.Where(i => i.Rareza != null && filtroRarezas.Contains(i.Rareza));
        }

        if (precioMin.HasValue) query = query.Where(i => i.Precio >= precioMin.Value);
        if (precioMax.HasValue) query = query.Where(i => i.Precio <= precioMax.Value);

        var resultado = new TiendaResultadoDto
        {
            Items = query
                .OrderBy(i => i.Juego)
                .ThenBy(i => i.Nombre)
                .ToList(),
            TotalItems = Items.Count,
            CYgo = Items.Count(i => i.Juego == "Yu-Gi-Oh!"),
            CPokemon = Items.Count(i => i.Juego == "Pokémon"),
            CMagic = Items.Count(i => i.Juego == "Magic: The Gathering"),
            COtros = Items.Count(i =>
                i.Juego != "Yu-Gi-Oh!" &&
                i.Juego != "Pokémon" &&
                i.Juego != "Magic: The Gathering"),
            PrecioMaximoReal = Items.Any() ? Items.Max(i => i.Precio) : 0
        };

        return Task.FromResult(resultado);
    }

    public Task<List<Item>> ObtenerItemsAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult(Items
            .OrderBy(i => i.Juego)
            .ThenBy(i => i.Nombre)
            .ToList());
    }

    public Task<Item?> ObtenerItemAsync(int id)
    {
        VerificarDisponibilidad();

        return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
    }

    public Task<ApiItemResultado> CrearItemAsync(Item item, IFormFile? imagen)
    {
        VerificarDisponibilidad();

        if (Items.Any(i => i.Nombre == item.Nombre))
        {
            return Task.FromResult(new ApiItemResultado
            {
                Ok = false,
                Mensaje = "Ya existe una carta con ese nombre."
            });
        }

        item.Id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1;
        Items.Add(item);

        return Task.FromResult(new ApiItemResultado { Ok = true, Item = item });
    }

    public Task<ApiItemResultado> ActualizarItemAsync(int id, Item item, IFormFile? imagen)
    {
        VerificarDisponibilidad();

        var actual = Items.FirstOrDefault(i => i.Id == id);

        if (actual == null)
        {
            return Task.FromResult(new ApiItemResultado
            {
                Ok = false,
                Mensaje = "El item no existe."
            });
        }

        if (Items.Any(i => i.Nombre == item.Nombre && i.Id != id))
        {
            return Task.FromResult(new ApiItemResultado
            {
                Ok = false,
                Mensaje = "Ya existe otra carta con ese nombre."
            });
        }

        actual.Nombre = item.Nombre;
        actual.Descripcion = item.Descripcion;
        actual.Juego = item.Juego;
        actual.Categoria = item.Categoria;
        actual.Rareza = item.Rareza;
        actual.Precio = item.Precio;
        actual.Stock = item.Stock;

        return Task.FromResult(new ApiItemResultado { Ok = true, Item = actual });
    }

    public Task<ApiItemResultado> EliminarItemAsync(int id)
    {
        VerificarDisponibilidad();

        var item = Items.FirstOrDefault(i => i.Id == id);

        if (item == null)
        {
            return Task.FromResult(new ApiItemResultado
            {
                Ok = false,
                Mensaje = "El item no existe."
            });
        }

        Items.Remove(item);

        return Task.FromResult(new ApiItemResultado { Ok = true });
    }

    public Task<ApiLoginResultado> LoginAsync(string correo, string contrasenia)
    {
        VerificarDisponibilidad();

        // Igual que la API real: se busca por correo y la contraseña se
        // verifica contra el hash guardado, no se compara en texto plano.
        var usuario = Usuarios.FirstOrDefault(u => u.Correo == correo);

        if (usuario == null || !Contrasena.Verificar(contrasenia, usuario.Contrasenia))
        {
            return Task.FromResult(new ApiLoginResultado
            {
                Ok = false,
                Mensaje = "El correo o la contraseña son incorrectos."
            });
        }

        return Task.FromResult(new ApiLoginResultado
        {
            Ok = true,
            Usuario = new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol
            }
        });
    }

    public Task<(bool Ok, string? Mensaje)> RegistrarAsync(
        string nombre,
        string correo,
        string contrasenia,
        string telefono,
        string? rol = null)
    {
        VerificarDisponibilidad();

        if (Usuarios.Any(u => u.Correo == correo))
        {
            return Task.FromResult<(bool, string?)>((false, "El correo ya está registrado."));
        }

        Usuarios.Add(new UsuarioPrueba
        {
            Id = Usuarios.Count == 0 ? 1 : Usuarios.Max(u => u.Id) + 1,
            Nombre = nombre,
            Correo = correo,

            // Como en la API: en la base solo queda el hash.
            Contrasenia = Contrasena.Hashear(contrasenia),

            Telefono = telefono,
            Rol = rol == "Administrador" ? "Administrador" : "Usuario"
        });

        return Task.FromResult<(bool, string?)>((true, null));
    }

    // USUARIOS (gestión)

    public Task<List<UsuarioDto>> ObtenerUsuariosAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult(Usuarios.Select(ComoUsuarioDto).ToList());
    }

    public Task<UsuarioDto?> ObtenerUsuarioAsync(int id)
    {
        VerificarDisponibilidad();

        var usuario = Usuarios.FirstOrDefault(u => u.Id == id);

        return Task.FromResult(usuario == null ? null : ComoUsuarioDto(usuario));
    }

    public Task<(bool Ok, string? Mensaje)> ActualizarUsuarioAsync(int id, UsuarioDto usuario)
    {
        VerificarDisponibilidad();

        var actual = Usuarios.FirstOrDefault(u => u.Id == id);

        if (actual == null)
        {
            return Task.FromResult<(bool, string?)>((false, "El usuario no existe."));
        }

        if (string.IsNullOrWhiteSpace(usuario.Nombre))
        {
            return Task.FromResult<(bool, string?)>((false, "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(usuario.Correo))
        {
            return Task.FromResult<(bool, string?)>((false, "El correo es obligatorio."));
        }

        if (Usuarios.Any(u => u.Id != id && u.Correo == usuario.Correo.Trim()))
        {
            return Task.FromResult<(bool, string?)>((false, "El correo ya está registrado."));
        }

        actual.Nombre = usuario.Nombre;
        actual.Correo = usuario.Correo;
        actual.Rol = usuario.Rol;
        actual.Telefono = usuario.Telefono;
        actual.Estado = usuario.Estado ?? actual.Estado;

        return Task.FromResult<(bool, string?)>((true, null));
    }

    public Task<(bool Ok, string? Mensaje)> CambiarEstadoUsuarioAsync(int id, string estado)
    {
        VerificarDisponibilidad();

        var actual = Usuarios.FirstOrDefault(u => u.Id == id);

        if (actual == null)
        {
            return Task.FromResult<(bool, string?)>((false, "El usuario no existe."));
        }

        if (estado != "Activo" && estado != "Suspendido")
        {
            return Task.FromResult<(bool, string?)>(
                (false, "El estado debe ser 'Activo' o 'Suspendido'."));
        }

        actual.Estado = estado;

        return Task.FromResult<(bool, string?)>((true, null));
    }

    private static UsuarioDto ComoUsuarioDto(UsuarioPrueba usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Correo = usuario.Correo,
        Rol = usuario.Rol,
        Telefono = usuario.Telefono,
        Estado = usuario.Estado
    };

    // CONFIGURACIÓN

    public Task<Configuracion> ObtenerConfiguracionAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult(Configuracion);
    }

    public Task<(bool Ok, string? Mensaje)> GuardarConfiguracionAsync(Configuracion configuracion)
    {
        VerificarDisponibilidad();

        if (string.IsNullOrWhiteSpace(configuracion.NombreTienda))
        {
            return Task.FromResult<(bool, string?)>(
                (false, "El nombre de la tienda es obligatorio."));
        }

        Configuracion = configuracion;

        return Task.FromResult<(bool, string?)>((true, null));
    }

    // INVENTARIO

    public Task<ApiInventarioStats?> ObtenerEstadisticasInventarioAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult<ApiInventarioStats?>(new ApiInventarioStats
        {
            ProductosActivos = Items.Count(i => i.Stock > 0),
            StockBajo = Items.Count(i => i.Stock > 0 && i.Stock <= 5),
            Agotados = Items.Count(i => i.Stock == 0),
            ValorInventario = Items.Sum(i => i.Precio * i.Stock),
            FechaActualizacion = DateTime.Now
        });
    }

    public Task<List<Item>?> ObtenerItemsInventarioAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult<List<Item>?>(
            Items.OrderBy(i => i.Nombre).ToList());
    }

    // REPORTES Y BÚSQUEDA

    public Task<List<ResultadoBusquedaDto>?> BuscarGlobalAsync(string query)
    {
        VerificarDisponibilidad();

        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<List<ResultadoBusquedaDto>?>(
                new List<ResultadoBusquedaDto>());
        }

        var termino = query.Trim().ToLower();

        var productos = Items
            .Where(i => (i.Nombre?.ToLower().Contains(termino) ?? false) ||
                        (i.Juego?.ToLower().Contains(termino) ?? false))
            .Take(5)
            .Select(i => new ResultadoBusquedaDto
            {
                Id = i.Id,
                Nombre = i.Nombre,
                Tipo = "Producto",
                Url = $"/Items/Details/{i.Id}"
            })
            .ToList();

        var usuarios = Usuarios
            .Where(u => (u.Nombre?.ToLower().Contains(termino) ?? false) ||
                        (u.Correo?.ToLower().Contains(termino) ?? false))
            .Take(5)
            .Select(u => new ResultadoBusquedaDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Tipo = "Usuario",
                Url = "/Usuarios/Administrador"
            })
            .ToList();

        return Task.FromResult<List<ResultadoBusquedaDto>?>(
            productos.Concat(usuarios).ToList());
    }

    public Task<ReporteGeneralDto?> GetReporteGeneralAsync()
    {
        VerificarDisponibilidad();

        var productosTop = LineasDeVentas()
            .GroupBy(l => l.NombreItem ?? "Sin nombre")
            .Select(g => new ProductoMasVendidoDto
            {
                NombreCarta = g.Key,
                Juego = g.First().NombreJuego ?? "Otros",
                PrecioUnitario = g.Average(l => l.PrecioUnitario),
                UnidadesVendidas = g.Sum(l => l.Cantidad),
                IngresosTotales = g.Sum(l => l.Subtotal)
            })
            .OrderByDescending(p => p.UnidadesVendidas)
            .Take(5)
            .ToList();

        return Task.FromResult<ReporteGeneralDto?>(new ReporteGeneralDto
        {
            IngresosTotales = Ventas.Sum(v => v.Total),
            PedidosCompletados = Ventas.Count,
            ClientesNew = Usuarios.Count(u => u.Rol != "Administrador"),
            ProductoTop = productosTop.FirstOrDefault()?.NombreCarta ?? "Sin ventas",
            FechaGeneracion = DateTime.Now,
            ProductosTopList = productosTop
        });
    }

    public Task<List<VentaPorDiaDto>?> GetVentasPorDiaAsync()
    {
        VerificarDisponibilidad();

        var porDia = Ventas
            .GroupBy(v => v.FechaVenta.ToString("yyyy-MM-dd"))
            .Select(g => new VentaPorDiaDto
            {
                Fecha = g.Key,
                Total = g.Sum(v => v.Total)
            })
            .OrderBy(v => v.Fecha)
            .ToList();

        return Task.FromResult<List<VentaPorDiaDto>?>(porDia);
    }

    public Task<List<VentaPorJuegoDto>?> GetVentasPorJuegoAsync()
    {
        VerificarDisponibilidad();

        var porJuego = LineasDeVentas()
            .GroupBy(l => l.NombreJuego ?? "Desconocido")
            .Select(g => new VentaPorJuegoDto
            {
                Juego = g.Key,
                Total = g.Sum(l => l.Subtotal)
            })
            .ToList();

        return Task.FromResult<List<VentaPorJuegoDto>?>(porJuego);
    }

    public Task<List<ProductoDestacadoDto>> ObtenerDestacadosAsync()
    {
        VerificarDisponibilidad();

        var destacados = LineasDeVentas()
            .GroupBy(l => l.IdItem)
            .Select(g => new
            {
                Item = Items.FirstOrDefault(i => i.Id == g.Key),
                Unidades = g.Sum(l => l.Cantidad)
            })
            .Where(x => x.Item != null)
            .Select(x => new ProductoDestacadoDto
            {
                Id = x.Item!.Id,
                Nombre = x.Item.Nombre,
                Juego = x.Item.Juego,
                Categoria = x.Item.Categoria,
                Rareza = x.Item.Rareza,
                Precio = x.Item.Precio,
                Stock = x.Item.Stock,
                ImagenRuta = x.Item.ImagenRuta,
                UnidadesVendidas = x.Unidades
            })
            .OrderByDescending(p => p.UnidadesVendidas)
            .Take(5)
            .ToList();

        return Task.FromResult(destacados);
    }

    // VENTAS Y PEDIDOS

    public Task<List<VentaDto>?> ObtenerPedidosAdminAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult<List<VentaDto>?>(Ventas
            .OrderByDescending(v => v.FechaVenta)
            .Select(ComoVentaDto)
            .ToList());
    }

    public Task<VentaDetalleDto?> ObtenerDetallePedidoAsync(int id)
    {
        VerificarDisponibilidad();

        var venta = Ventas.FirstOrDefault(v => v.Id == id);

        if (venta == null)
        {
            return Task.FromResult<VentaDetalleDto?>(null);
        }

        return Task.FromResult<VentaDetalleDto?>(new VentaDetalleDto
        {
            Id = venta.Id,
            NombreUsuario = venta.NombreUsuario,
            FechaVenta = venta.FechaVenta,
            Total = venta.Total,
            Estado = venta.Estado,
            Lineas = venta.Lineas.Select(l => new LineaVentaDetalleDto
            {
                NombreItem = l.NombreItem,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                Subtotal = l.Subtotal
            }).ToList()
        });
    }

    public Task<bool> ActualizarEstadoPedidoAsync(int id, string estado)
    {
        VerificarDisponibilidad();

        var venta = Ventas.FirstOrDefault(v => v.Id == id);

        if (venta == null)
        {
            return Task.FromResult(false);
        }

        venta.Estado = estado;

        if (estado == "Entregado")
        {
            venta.FechaActualizacion = DateTime.Now;
        }

        return Task.FromResult(true);
    }

    public Task<List<VentaDto>> GetVentasRecientesAsync()
    {
        VerificarDisponibilidad();

        return Task.FromResult(Ventas
            .Where(v => v.Estado == "Entregado")
            .OrderByDescending(v => v.FechaVenta)
            .Take(30)
            .Select(ComoVentaDto)
            .ToList());
    }

    public Task<VentasStatsDto?> ObtenerEstadisticasVentasAsync()
    {
        VerificarDisponibilidad();

        var hoy = DateTime.Today;
        var inicioSemana = hoy.AddDays(-((int)hoy.DayOfWeek + 6) % 7);
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        var entregadas = Ventas
            .Where(v => v.Estado == "Entregado")
            .Select(v => (Fecha: v.FechaActualizacion ?? v.FechaVenta, v.Total))
            .ToList();

        var delMes = entregadas.Where(v => v.Fecha >= inicioMes).ToList();

        return Task.FromResult<VentasStatsDto?>(new VentasStatsDto
        {
            VentasDelDia = entregadas.Where(v => v.Fecha.Date == hoy).Sum(v => v.Total),
            VentasDeLaSemana = entregadas.Where(v => v.Fecha >= inicioSemana).Sum(v => v.Total),
            PedidosEnElMes = delMes.Count,
            TicketPromedio = delMes.Count > 0
                ? delMes.Sum(v => v.Total) / delMes.Count
                : 0m
        });
    }

    public Task<FinalizarVentaResultado?> FinalizarVentaAsync(FinalizarVentaRequest request)
    {
        VerificarDisponibilidad();

        var lineas = (request.Lineas ?? new List<LineaVentaRequest>())
            .Where(l => l.Cantidad > 0)
            .ToList();

        if (lineas.Count == 0)
        {
            return Task.FromResult<FinalizarVentaResultado?>(new FinalizarVentaResultado
            {
                Ok = false,
                Mensaje = "El carrito está vacío.",
                Total = 0
            });
        }

        // La API acepta "Pendiente" (por defecto) o "Entregado".
        var estadoPedido = string.IsNullOrWhiteSpace(request.Estado)
            ? "Pendiente"
            : request.Estado.Trim();

        if (estadoPedido != "Pendiente" && estadoPedido != "Entregado")
        {
            return Task.FromResult<FinalizarVentaResultado?>(new FinalizarVentaResultado
            {
                Ok = false,
                Mensaje = "El estado inicial debe ser 'Pendiente' o 'Entregado'.",
                Total = 0
            });
        }

        var problemas = new List<string>();

        foreach (var linea in lineas)
        {
            var item = Items.FirstOrDefault(i => i.Id == linea.IdItem);

            if (item == null)
            {
                problemas.Add($"El producto {linea.IdItem} ya no está disponible");
            }
            else if (item.Stock < linea.Cantidad)
            {
                problemas.Add($"{item.Nombre} (solo hay {item.Stock} en stock)");
            }
        }

        if (problemas.Count > 0)
        {
            return Task.FromResult<FinalizarVentaResultado?>(new FinalizarVentaResultado
            {
                Ok = false,
                Mensaje = "No se pudo finalizar la venta. Sin stock suficiente para: " +
                    string.Join(", ", problemas) + ".",
                Total = 0
            });
        }

        var detalles = new List<LineaVentaPrueba>();
        var total = 0m;

        foreach (var linea in lineas)
        {
            var item = Items.First(i => i.Id == linea.IdItem);
            var subtotal = item.Precio * linea.Cantidad;

            item.Stock -= linea.Cantidad;
            total += subtotal;

            detalles.Add(new LineaVentaPrueba
            {
                IdItem = item.Id,
                NombreItem = item.Nombre,
                NombreJuego = item.Juego,
                Cantidad = linea.Cantidad,
                PrecioUnitario = item.Precio,
                Subtotal = subtotal
            });
        }

        var comprador = Usuarios.FirstOrDefault(u => u.Id == request.IdUsuario);

        Ventas.Add(new VentaPrueba
        {
            Id = Ventas.Count == 0 ? 1 : Ventas.Max(v => v.Id) + 1,
            NombreUsuario = comprador?.Nombre ?? "Cliente",
            FechaVenta = DateTime.Now,
            Total = total,
            Estado = estadoPedido,
            FechaActualizacion = estadoPedido == "Entregado" ? DateTime.Now : null,
            Lineas = detalles
        });

        return Task.FromResult<FinalizarVentaResultado?>(new FinalizarVentaResultado
        {
            Ok = true,
            Mensaje = "Venta finalizada correctamente. " +
                $"Total: Bs {total.ToString("N2")}. El stock fue actualizado." +
                (estadoPedido == "Entregado" ? " El pedido quedo como Entregado." : ""),
            Total = total
        });
    }

    private static VentaDto ComoVentaDto(VentaPrueba venta) => new()
    {
        Id = venta.Id,
        NombreUsuario = venta.NombreUsuario,
        FechaVenta = venta.FechaVenta,
        Total = venta.Total,
        Estado = venta.Estado
    };

    private IEnumerable<LineaVentaPrueba> LineasDeVentas()
        => Ventas.SelectMany(v => v.Lineas);
}

public class UsuarioPrueba
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Correo { get; set; }
    public string? Contrasenia { get; set; }
    public string? Rol { get; set; }
    public string? Telefono { get; set; }
    public string? Estado { get; set; } = "Activo";
}

/// <summary>Venta en memoria para simular la tabla ventas de la API.</summary>
public class VentaPrueba
{
    public int Id { get; set; }
    public string? NombreUsuario { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }
    public string? Estado { get; set; } = "Pendiente";
    public DateTime? FechaActualizacion { get; set; }
    public List<LineaVentaPrueba> Lineas { get; set; } = new List<LineaVentaPrueba>();
}

/// <summary>Línea de detalle de una venta en memoria.</summary>
public class LineaVentaPrueba
{
    public int IdItem { get; set; }
    public string? NombreItem { get; set; }
    public string? NombreJuego { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}