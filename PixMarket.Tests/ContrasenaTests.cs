using PixMarketAPI.Seguridad;

namespace PixMarket.Tests;

/// <summary>
/// Pruebas del cifrado de contraseñas (PBKDF2-SHA256).
///
/// Comprueban lo importante: que la contraseña no se pueda recuperar desde la
/// base de datos, que dos usuarios con la misma contraseña tengan hashes
/// distintos, y que las cuentas viejas (que estaban en texto plano) todavía
/// puedan entrar.
/// </summary>
public class ContrasenaTests
{
    [Fact]
    public void Hashear_no_devuelve_la_contrasena_en_claro()
    {
        var hash = Contrasena.Hashear("Admin123#");

        Assert.DoesNotContain("Admin123#", hash, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hashear_devuelve_el_formato_esperado()
    {
        // PBKDF2$iteraciones$sal$hash
        var partes = Contrasena.Hashear("123456").Split('$');

        Assert.Equal(4, partes.Length);
        Assert.Equal("PBKDF2", partes[0]);
        Assert.True(int.TryParse(partes[1], out var iteraciones));
        Assert.True(iteraciones >= 100_000);
    }

    [Fact]
    public void La_misma_contrasena_produce_hashes_distintos()
    {
        // La sal es aleatoria: si no, dos personas con "123456" podrían
        // compararse solo mirando la base de datos.
        var primero = Contrasena.Hashear("123456");
        var segundo = Contrasena.Hashear("123456");

        Assert.NotEqual(primero, segundo);
    }

    [Fact]
    public void Verificar_acepta_la_contrasena_correcta()
    {
        var hash = Contrasena.Hashear("claveSegura99");

        Assert.True(Contrasena.Verificar("claveSegura99", hash));
    }

    [Theory]
    [InlineData("claveErronea")]
    [InlineData("")]
    [InlineData("CLASESEGURA99")] // distinguir mayúsculas y minúsculas
    [InlineData("claveSegura99 ")] // un espacio de más
    public void Verificar_rechaza_una_contrasena_incorrecta(string intento)
    {
        var hash = Contrasena.Hashear("claveSegura99");

        Assert.False(Contrasena.Verificar(intento, hash));
    }

    [Fact]
    public void Verificar_rechaza_un_hash_manipulado()
    {
        var hash = Contrasena.Hashear("123456");
        var partes = hash.Split('$');

        // Alguien intenta cambiar el hash guardado en la base de datos.
        var partesAlteradas = new[]
        {
            partes[0], partes[1], partes[2],
            Convert.ToBase64String(new byte[32])
        };

        Assert.False(Contrasena.Verificar("123456", string.Join('$', partesAlteradas)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("PBKDF2$")]
    [InlineData("PBKDF2$100000$salInvalida!!!$hash")]
    [InlineData("PBKDF2$noEsNumero$sal$hash")]
    [InlineData("PBKDF2$100000$soloTresPartes")]
    public void Verificar_rechaza_valores_guardados_invalidos(string? guardado)
    {
        Assert.False(Contrasena.Verificar("123456", guardado));
    }

    [Fact]
    public void EstaCifrada_reconoce_los_dos_formatos()
    {
        Assert.True(Contrasena.EstaCifrada(Contrasena.Hashear("123456")));

        // Lo que había en la base de datos antes de este cambio.
        Assert.False(Contrasena.EstaCifrada("123"));
        Assert.False(Contrasena.EstaCifrada(null));
    }

    [Fact]
    public void Verificar_acepta_una_contrasena_vieja_en_texto_plano()
    {
        // Necesario para que las cuentas existentes puedan entrar y se les
        // recifre la contraseña en su primer inicio de sesión.
        Assert.True(Contrasena.Verificar("Admin123", "Admin123"));
        Assert.False(Contrasena.Verificar("otra", "Admin123"));
    }

    [Fact]
    public void El_hash_cabe_en_la_columna_de_la_base_de_datos()
    {
        // La columna Contrasenia es varchar(200) en el modelo.
        Assert.True(Contrasena.Hashear(new string('a', 100)).Length <= 200);
    }
}
