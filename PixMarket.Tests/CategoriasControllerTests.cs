using Microsoft.AspNetCore.Mvc;
using PixMarket.Models;

namespace PixMarket.Tests;

public class CategoriasControllerTests
{
    [Fact]
    public async Task Index_SinFiltros_DevuelveTodasLasCartasOrdenadas()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, null, null, null, null, null, null);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Equal(4, modelo.Count);
        // Orden: por Juego y luego Nombre (Magic < Pokemón < Uno < Yu-Gi-Oh!)
        Assert.Equal(new[] { 3, 2, 4, 1 }, modelo.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task Index_BuscarPorTexto_Filtra()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index("Pikachu", null, null, null, null, null, null);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(2, modelo[0].Id);
        Assert.Equal("Pikachu", ((ViewResult)resultado).ViewData["Buscar"]);
        Assert.Equal("Pikachu", api.UltimoBuscar);
    }

    [Fact]
    public async Task Index_BuscarVacio_DevuelveTodo()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index("", null, null, null, null, null, null);

        Assert.Equal(4, Controladores.ModeloTienda(resultado).Count);
        Assert.Equal("", api.UltimoBuscar);
    }

    [Fact]
    public async Task Index_PestanaJuego_SePasaComoParametro()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, "Pokémon", null, null, null, null, null);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(2, modelo[0].Id);
        Assert.Equal("Pokémon", api.UltimoJuego);
    }

    [Fact]
    public async Task Index_PestanaOtros_ExcluyeLosTresJuegosPrincipales()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, "Otros", null, null, null, null, null);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(4, modelo[0].Id); // "Uno" no está en los 3 principales
        Assert.Equal("Otros", api.UltimoJuego);
    }

    [Fact]
    public async Task Index_CheckboxesJuegos_Filtran()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, null, new[] { "Pokémon", "Yu-Gi-Oh!" }, null, null, null, null);

        Assert.Equal(2, Controladores.ModeloTienda(resultado).Count);
        Assert.Equal(new[] { "Pokémon", "Yu-Gi-Oh!" }, api.UltimosJuegos);
    }

    [Fact]
    public async Task Index_Categoria_FiltraPorCategoria()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, null, null, new[] { "Hechizo" }, null, null, null);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(3, modelo[0].Id);
        Assert.Equal(new[] { "Hechizo" }, api.UltimasCategorias);
    }

    [Fact]
    public async Task Index_Rareza_FiltraPorRareza()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, null, null, null, new[] { "Rare" }, null, null);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(3, modelo[0].Id);
        Assert.Equal(new[] { "Rare" }, api.UltimasRarezas);
    }

    [Fact]
    public async Task Index_RangoDePrecio_Filtra()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index(null, null, null, null, null, 50m, 200m);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(1, modelo[0].Id); // precio 100
        Assert.Equal(50m, api.UltimoPrecioMin);
        Assert.Equal(200m, api.UltimoPrecioMax);
    }

    [Fact]
    public async Task Index_FiltrosCombinados_SeAplicanTodos()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index("Dragón", "Yu-Gi-Oh!", null, new[] { "Monstruo" }, null, 50m, 200m);

        var modelo = Controladores.ModeloTienda(resultado);
        Assert.Single(modelo);
        Assert.Equal(1, modelo[0].Id);
    }

    [Fact]
    public async Task Index_SinResultados_DevuelveListaVacia()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = await controller.Index("noexiste", null, null, null, null, null, null);

        Assert.Empty(Controladores.ModeloTienda(resultado));
    }

    [Fact]
    public async Task Index_ExponeContadoresDePestanas()
    {
        var api = Controladores.CrearApi();
        var controller = Controladores.CrearTienda(api);

        var resultado = (ViewResult)await controller.Index(null, null, null, null, null, null, null);

        Assert.Equal(4, resultado.ViewData["TotalItems"]);
        Assert.Equal(1, resultado.ViewData["CYgo"]);
        Assert.Equal(1, resultado.ViewData["CPokemon"]);
        Assert.Equal(1, resultado.ViewData["CMagic"]);
        Assert.Equal(1, resultado.ViewData["COtros"]);
        Assert.Equal(500m, resultado.ViewData["PrecioMaximoReal"]);
    }

    [Fact]
    public async Task Index_ApiIndisponible_MuestraErrorYPermiteVistaVacia()
    {
        var api = Controladores.CrearApi();
        api.ApiIndisponible = true;
        var controller = Controladores.CrearTienda(api);

        var resultado = (ViewResult)await controller.Index(null, null, null, null, null, null, null);

        Assert.Empty(Controladores.ModeloTienda(resultado));
        Assert.NotNull(resultado.ViewData["ApiError"]);
    }
}