using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PixMarket.Controllers;
using PixMarket.Models;

namespace PixMarket.Tests;

public static class Controladores
{
    public static List<Item> ItemsDeEjemplo() => new()
    {
        new Item { Id = 1, Nombre = "Dragón Blanco de Ojos Azules", Juego = "Yu-Gi-Oh!", Categoria = "Monstruo", Rareza = "Secret Rare", Precio = 100m, Stock = 5 },
        new Item { Id = 2, Nombre = "Pikachu", Juego = "Pokémon", Categoria = "Monstruo", Rareza = "Secret Rare", Precio = 500m, Stock = 0 },
        new Item { Id = 3, Nombre = "Bola de Fuego", Juego = "Magic: The Gathering", Categoria = "Hechizo", Rareza = "Rare", Precio = 45m, Stock = 3 },
        new Item { Id = 4, Nombre = "Terraform", Juego = "Uno", Categoria = "Trampa", Rareza = "Common", Precio = 15m, Stock = 8 }
    };

    public static List<UsuarioPrueba> UsuariosDeEjemplo() => new()
    {
        new UsuarioPrueba { Id = 1, Nombre = "Admin", Correo = "admin@test.com", Contrasenia = "123456", Rol = "Administrador" },
        new UsuarioPrueba { Id = 2, Nombre = "Cliente", Correo = "cliente@test.com", Contrasenia = "abcdef", Rol = "Usuario" }
    };

    public static FakeApiService CrearApi() => new()
    {
        Items = ItemsDeEjemplo(),
        Usuarios = UsuariosDeEjemplo()
    };

    public static CategoriasController CrearTienda(FakeApiService api)
        => new(api);

    public static CarritoController CrearCarrito(FakeApiService api, FakeSession? session = null)
    {
        var http = new DefaultHttpContext { Session = session ?? new FakeSession() };

        var controller = new CarritoController(api);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = http,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };
        controller.TempData = new TempDataDictionary(http, new FakeTempDataProvider());

        return controller;
    }

    public static (UsuariosController controlador, FakeAuthenticationService auth) CrearUsuarios(
        FakeApiService api, FakeSession? session = null)
    {
        var auth = new FakeAuthenticationService();

        var services = new ServiceCollection();
        services.AddControllersWithViews();
        services.AddSingleton<IAuthenticationService>(auth);

        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var controller = new UsuariosController(api);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = http,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };

        return (controller, auth);
    }

    public static List<ItemCarrito> ModeloCarrito(IActionResult resultado)
        => ((ViewResult)resultado).Model as List<ItemCarrito> ?? new List<ItemCarrito>();

    public static List<Item> ModeloTienda(IActionResult resultado)
        => ((ViewResult)resultado).Model as List<Item> ?? new List<Item>();
}

public class FakeTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context)
        => new Dictionary<string, object>();

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
    }
}