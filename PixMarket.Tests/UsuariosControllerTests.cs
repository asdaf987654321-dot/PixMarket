using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixMarket.Controllers;

namespace PixMarket.Tests;

public class UsuariosControllerTests
{
    [Fact]
    public async Task Login_ConCredencialesValidas_RedirigeAlAdministradorYSesiona()
    {
        var api = Controladores.CrearApi();
        var (controller, auth) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Index("admin@test.com", "123456", false);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Administrador", redireccion.ActionName);

        Assert.NotNull(auth.SignedInPrincipal);
        Assert.Equal("Admin", auth.SignedInPrincipal!.Identity?.Name);
        Assert.Equal("admin@test.com", auth.SignedInPrincipal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("Administrador", auth.SignedInPrincipal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("1", auth.SignedInPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.False(auth.SignInProperties?.IsPersistent);
    }

    [Fact]
    public async Task Login_ConRolUsuario_RedirigeACliente()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Index("cliente@test.com", "abcdef", false);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Cliente", redireccion.ActionName);
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_MuestraErrorYNoSesiona()
    {
        var api = Controladores.CrearApi();
        var (controller, auth) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Index("admin@test.com", "incorrecta", false);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.NotNull(vista.ViewData["Error"]);
        Assert.Null(auth.SignedInPrincipal);
    }

    [Fact]
    public async Task Login_ConCamposVacios_MuestraError()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Index("", "", false);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Contains("correo", vista.ViewData["Error"]!.ToString()!.ToLowerInvariant());
    }

    [Fact]
    public async Task Login_ConRecordarme_FirmaCookiePersistente()
    {
        var api = Controladores.CrearApi();
        var (controller, auth) = Controladores.CrearUsuarios(api);

        await controller.Index("admin@test.com", "123456", true);

        Assert.NotNull(auth.SignInProperties);
        Assert.True(auth.SignInProperties!.IsPersistent);
        Assert.NotNull(auth.SignInProperties.ExpiresUtc);
    }

    [Fact]
    public async Task Login_ApiIndisponible_MuestraError()
    {
        var api = Controladores.CrearApi();
        api.ApiIndisponible = true;
        var (controller, auth) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Index("admin@test.com", "123456", false);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.NotNull(vista.ViewData["Error"]);
        Assert.Null(auth.SignedInPrincipal);
    }

    [Fact]
    public async Task Logout_FirmaLaSesionYRedirigeAlHome()
    {
        var api = Controladores.CrearApi();
        var (controller, auth) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Logout();

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Home", redireccion.ControllerName);
        Assert.True(auth.SignedOut);
    }

    [Fact]
    public async Task Registro_ConDatosValidos_CreaElUsuarioYRedirige()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Registro("Nuevo", "nuevo@test.com", "123456", "5551234");

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redireccion.ActionName);
        Assert.Contains(api.Usuarios, u => u.Correo == "nuevo@test.com");
        Assert.Equal("Usuario", api.Usuarios.Single(u => u.Correo == "nuevo@test.com").Rol);
        Assert.NotNull(controller.TempData["RegistroExitoso"]);
    }

    [Fact]
    public async Task Registro_ConCorreoExistente_MuestraError()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Registro("Admin", "admin@test.com", "123456", "5551234");

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.NotNull(vista.ViewData["Error"]);
    }

    [Fact]
    public async Task Registro_ConContrasenaCorta_MuestraError()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Registro("Nuevo", "nuevo@test.com", "123", "5551234");

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Contains("6 caracteres", vista.ViewData["Error"]!.ToString());
    }

    [Fact]
    public async Task Registro_ApiIndisponible_MuestraError()
    {
        var api = Controladores.CrearApi();
        api.ApiIndisponible = true;
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Registro("Nuevo", "nuevo@test.com", "123456", "5551234");

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.NotNull(vista.ViewData["Error"]);
        Assert.DoesNotContain(api.Usuarios, u => u.Correo == "nuevo@test.com");
    }

    [Fact]
    public void Administrador_RequiereRolAdministrador()
    {
        var atributo = typeof(UsuariosController)
            .GetMethod(nameof(UsuariosController.Administrador))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains("Administrador", atributo!.Roles?.Split(',').Select(r => r.Trim()) ?? Array.Empty<string>());
    }

    [Fact]
    public void Cliente_RequiereRolUsuario()
    {
        var atributo = typeof(UsuariosController)
            .GetMethod(nameof(UsuariosController.Cliente))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains("Usuario", atributo!.Roles?.Split(',').Select(r => r.Trim()) ?? Array.Empty<string>());
    }

    [Fact]
    public void ItemsController_RequiereRolAdministrador()
    {
        var atributo = typeof(ItemsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains("Administrador", atributo!.Roles?.Split(',').Select(r => r.Trim()) ?? Array.Empty<string>());
    }
}