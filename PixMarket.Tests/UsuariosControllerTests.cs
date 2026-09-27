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
    public async Task Details_ConIdValido_DevuelveElUsuarioConTelefonoYEstado()
    {
        var api = Controladores.CrearApi();
        api.Usuarios[0].Telefono = "5551234";
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Details(1);

        var vista = Assert.IsType<ViewResult>(resultado);
        var usuario = Assert.IsType<PixMarket.Servicios.UsuarioDto>(vista.Model);
        Assert.Equal("Admin", usuario.Nombre);
        Assert.Equal("5551234", usuario.Telefono);
        Assert.Equal("Activo", usuario.Estado);
    }

    [Fact]
    public async Task Details_ConIdInexistente_MuestraErrorYVuelveAlListado()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Details(99);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Gestion", redireccion.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Details_ConApiIndisponible_MuestraErrorYVuelveAlListado()
    {
        var api = Controladores.CrearApi();
        api.ApiIndisponible = true;
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Details(1);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Gestion", redireccion.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_Get_DevuelveElUsuarioParaEditar()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Edit(2);

        var vista = Assert.IsType<ViewResult>(resultado);
        var usuario = Assert.IsType<PixMarket.Servicios.UsuarioDto>(vista.Model);
        Assert.Equal("cliente@test.com", usuario.Correo);
    }

    [Fact]
    public async Task Edit_Post_ActualizaLosDatosYRedirige()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Edit(2, new PixMarket.Servicios.UsuarioDto
        {
            Id = 2,
            Nombre = "Cliente Actualizado",
            Correo = "cliente2@test.com",
            Telefono = "5559999",
            Rol = "Administrador"
        });

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Gestion", redireccion.ActionName);

        var guardado = api.Usuarios.Single(u => u.Id == 2);
        Assert.Equal("Cliente Actualizado", guardado.Nombre);
        Assert.Equal("cliente2@test.com", guardado.Correo);
        Assert.Equal("5559999", guardado.Telefono);
        Assert.Equal("Administrador", guardado.Rol);
    }

    [Fact]
    public async Task Edit_Post_ConCorreoRepetido_MuestraErrorYNoGuarda()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Edit(2, new PixMarket.Servicios.UsuarioDto
        {
            Id = 2,
            Nombre = "Cliente",
            Correo = "admin@test.com",
            Rol = "Usuario"
        });

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.NotNull(vista.ViewData["Error"]);
        Assert.Equal("cliente@test.com", api.Usuarios.Single(u => u.Id == 2).Correo);
    }

    [Fact]
    public async Task Edit_Post_SinNombre_MuestraError()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Edit(2, new PixMarket.Servicios.UsuarioDto
        {
            Id = 2,
            Nombre = "",
            Correo = "cliente@test.com",
            Rol = "Usuario"
        });

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Contains("nombre", vista.ViewData["Error"]!.ToString()!.ToLowerInvariant());
    }

    [Fact]
    public async Task CambiarEstado_SuspenderDejaElUsuarioSuspendido()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.CambiarEstado(2, "Suspendido");

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Gestion", redireccion.ActionName);
        Assert.Equal("Suspendido", api.Usuarios.Single(u => u.Id == 2).Estado);
        Assert.NotNull(controller.TempData["Mensaje"]);
    }

    [Fact]
    public async Task CambiarEstado_ActivarVuelveADejarElUsuarioActivo()
    {
        var api = Controladores.CrearApi();
        api.Usuarios[1].Estado = "Suspendido";
        var (controller, _) = Controladores.CrearUsuarios(api);

        await controller.CambiarEstado(2, "Activo");

        Assert.Equal("Activo", api.Usuarios.Single(u => u.Id == 2).Estado);
    }

    [Fact]
    public async Task CambiarEstado_ConEstadoInvalido_MuestraErrorYNoCambia()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.CambiarEstado(2, "Banear");

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Gestion", redireccion.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal("Activo", api.Usuarios.Single(u => u.Id == 2).Estado);
    }

    [Fact]
    public async Task Crear_CreaElUsuarioConElRolElegido()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        var resultado = await controller.Crear(
            "Nuevo Admin", "nuevo.admin@test.com", "123456", "5550000", "Administrador");

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Gestion", redireccion.ActionName);

        var creado = api.Usuarios.Single(u => u.Correo == "nuevo.admin@test.com");
        Assert.Equal("Administrador", creado.Rol);
        Assert.Equal("5550000", creado.Telefono);
    }

    [Fact]
    public async Task Crear_ConRolDesconocido_CreaElUsuarioComoUsuario()
    {
        var api = Controladores.CrearApi();
        var (controller, _) = Controladores.CrearUsuarios(api);

        await controller.Crear("Nuevo", "nuevo@test.com", "123456", "5551234", "SuperAdmin");

        Assert.Equal("Usuario", api.Usuarios.Single(u => u.Correo == "nuevo@test.com").Rol);
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