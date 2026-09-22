using Microsoft.AspNetCore.Http;
using PixMarket.Models;
using PixMarket.Servicios;

namespace PixMarket.Tests;

public class FakeApiService : IPixMarketApiService
{
    public List<Item> Items { get; set; } = new List<Item>();

    public List<UsuarioPrueba> Usuarios { get; set; } = new List<UsuarioPrueba>();

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

        var usuario = Usuarios.FirstOrDefault(u =>
            u.Correo == correo && u.Contrasenia == contrasenia);

        if (usuario == null)
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
        string nombre, string correo, string contrasenia, string telefono)
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
            Contrasenia = contrasenia,
            Rol = "Usuario"
        });

        return Task.FromResult<(bool, string?)>((true, null));
    }

    public Task<FinalizarVentaDto> FinalizarVentaAsync(List<ItemCarrito> lineas)
    {
        VerificarDisponibilidad();

        var problemas = new List<string>();

        foreach (var linea in lineas)
        {
            var item = Items.FirstOrDefault(i => i.Id == linea.Id);

            if (item == null)
            {
                problemas.Add($"{linea.Nombre} (ya no está disponible)");
            }
            else if (item.Stock < linea.Cantidad)
            {
                problemas.Add($"{item.Nombre} (solo hay {item.Stock} en stock)");
            }
        }

        if (problemas.Any())
        {
            return Task.FromResult(new FinalizarVentaDto
            {
                Ok = false,
                Mensaje = "No se pudo finalizar la venta. Sin stock suficiente para: " +
                    string.Join(", ", problemas) + ".",
                Total = 0
            });
        }

        var total = 0m;

        foreach (var linea in lineas)
        {
            var item = Items.First(i => i.Id == linea.Id);
            item.Stock -= linea.Cantidad;
            total += item.Precio * linea.Cantidad;
        }

        return Task.FromResult(new FinalizarVentaDto
        {
            Ok = true,
            Mensaje = $"Venta finalizada correctamente. Total: Bs {total.ToString("N2")}. El stock fue actualizado.",
            Total = total
        });
    }
}

public class UsuarioPrueba
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Correo { get; set; }
    public string? Contrasenia { get; set; }
    public string? Rol { get; set; }
}