using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Controllers;
using PixMarket.Models;

namespace PixMarket.Tests;

public class CarritoControllerTests
{
    [Fact]
    public void Index_CarritoVacio_DevuelveListaVacia()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        var modelo = Controladores.ModeloCarrito(controller.Index());

        Assert.NotNull(modelo);
        Assert.Empty(modelo);
    }

    [Fact]
    public async Task Agregar_AgregaArticuloAlCarrito()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        var resultado = await controller.Agregar(1, 2);

        Assert.IsType<RedirectToActionResult>(resultado);

        var carrito = Controladores.ModeloCarrito(controller.Index());
        Assert.Single(carrito);
        Assert.Equal(1, carrito[0].Id);
        Assert.Equal(2, carrito[0].Cantidad);
    }

    [Fact]
    public async Task Agregar_LimitaLaCantidadAlStock()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(3, 10); // stock de "Bola de Fuego" = 3

        var carrito = Controladores.ModeloCarrito(controller.Index());
        Assert.Equal(3, carrito[0].Cantidad);
    }

    [Fact]
    public async Task Agregar_ArticuloAgotado_NoSeAgrega()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        var resultado = await controller.Agregar(2, 1); // stock 0

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Categorias", redireccion.ControllerName);

        Assert.Empty(Controladores.ModeloCarrito(controller.Index()));
    }

    [Fact]
    public async Task Agregar_ArticuloRepetido_SumaCantidad()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(1, 2);
        await controller.Agregar(1, 3);

        var carrito = Controladores.ModeloCarrito(controller.Index());
        Assert.Single(carrito);
        Assert.Equal(5, carrito[0].Cantidad);
    }

    [Fact]
    public async Task Agregar_ItemInexistente_DevuelveNotFound()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        var resultado = await controller.Agregar(999, 1);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Agregar_ApiIndisponible_RedirigeConMensaje()
    {
        var api = Controladores.CrearApi();
        api.ApiIndisponible = true;
        var controller = Controladores.CrearCarrito(api);

        var resultado = await controller.Agregar(1, 1);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Categorias", redireccion.ControllerName);
        Assert.NotNull(controller.TempData["MensajeCarrito"]);
    }

    [Fact]
    public async Task ActualizarCantidad_ModificaLaCantidad()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(1, 1);
        controller.ActualizarCantidad(1, 4);

        var carrito = Controladores.ModeloCarrito(controller.Index());
        Assert.Equal(4, carrito[0].Cantidad);
    }

    [Fact]
    public async Task ActualizarCantidad_Cero_EliminaElArticulo()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(1, 1);
        controller.ActualizarCantidad(1, 0);

        Assert.Empty(Controladores.ModeloCarrito(controller.Index()));
    }

    [Fact]
    public async Task Eliminar_QuitaElArticuloDelCarrito()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(1, 1);
        await controller.Agregar(3, 1);
        controller.Eliminar(1);

        var carrito = Controladores.ModeloCarrito(controller.Index());
        Assert.Single(carrito);
        Assert.Equal(3, carrito[0].Id);
    }

    [Fact]
    public async Task Vaciar_DejaElCarritoVacio()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(1, 1);
        controller.Vaciar();

        Assert.Empty(Controladores.ModeloCarrito(controller.Index()));
    }

    [Fact]
    public async Task Finalizar_DescuentaStockViaApiYVaciaElCarrito()
    {
        var api = Controladores.CrearApi();
        var item1 = api.Items.Single(i => i.Id == 1);
        var item3 = api.Items.Single(i => i.Id == 3);
        var stock1 = item1.Stock;
        var stock3 = item3.Stock;
        var controller = Controladores.CrearCarrito(api);

        await controller.Agregar(1, 2);
        await controller.Agregar(3, 1);

        var resultado = await controller.Finalizar();

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(stock1 - 2, api.Items.Single(i => i.Id == 1).Stock);
        Assert.Equal(stock3 - 1, api.Items.Single(i => i.Id == 3).Stock);
        Assert.Empty(Controladores.ModeloCarrito(controller.Index()));
        Assert.NotNull(controller.TempData["MensajeCarrito"]);
    }

    [Fact]
    public async Task Finalizar_CarritoVacio_NoLlamaALAAPI()
    {
        var api = Controladores.CrearApi();
        var stock1 = api.Items.Single(i => i.Id == 1).Stock;
        var controller = Controladores.CrearCarrito(api);

        var resultado = await controller.Finalizar();

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(stock1, api.Items.Single(i => i.Id == 1).Stock);
        Assert.NotNull(controller.TempData["MensajeCarrito"]);
    }

    [Fact]
    public async Task Finalizar_SinStock_NoVaciaElCarritoYMuestraMensaje()
    {
        var api = Controladores.CrearApi();
        var session = new FakeSession();
        var controller = Controladores.CrearCarrito(api, session);

        // línea con cantidad mayor al stock real (caso inconsistente)
        var carrito = new List<ItemCarrito>
        {
            new ItemCarrito { Id = 3, Nombre = "Bola de Fuego", Juego = "Magic: The Gathering", Categoria = "Hechizo", Rareza = "Rare", Precio = 45m, Stock = 3, Cantidad = 10 }
        };
        session.SetString("Carrito", JsonSerializer.Serialize(carrito));

        var resultado = await controller.Finalizar();

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(3, api.Items.Single(i => i.Id == 3).Stock);
        Assert.Contains("stock", controller.TempData["MensajeCarrito"]!.ToString()!.ToLowerInvariant());
        Assert.Single(Controladores.ModeloCarrito(controller.Index()));
    }

    [Fact]
    public async Task Finalizar_ApiIndisponible_NoVaciaElCarrito()
    {
        var api = Controladores.CrearApi();
        var session = new FakeSession();
        var carrito = new List<ItemCarrito>
        {
            new ItemCarrito { Id = 1, Nombre = "Dragón Blanco de Ojos Azules", Juego = "Yu-Gi-Oh!", Categoria = "Monstruo", Rareza = "Secret Rare", Precio = 100m, Stock = 5, Cantidad = 1 }
        };
        session.SetString("Carrito", JsonSerializer.Serialize(carrito));

        var controller = Controladores.CrearCarrito(api, session);

        api.ApiIndisponible = true;
        await controller.Finalizar();

        Assert.Single(Controladores.ModeloCarrito(controller.Index()));
        Assert.NotNull(controller.TempData["MensajeCarrito"]);
    }

    [Fact]
    public void Finalizar_RequiereInicioDeSesion()
    {
        var atributo = typeof(CarritoController)
            .GetMethod(nameof(CarritoController.Finalizar))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Null(atributo!.Roles); // cualquier rol autenticado
    }

    [Fact]
    public void Agregar_TieneAntiForgery()
    {
        var atributo = typeof(CarritoController)
            .GetMethod(nameof(CarritoController.Agregar))!
            .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false)
            .SingleOrDefault();

        Assert.NotNull(atributo);
    }
}