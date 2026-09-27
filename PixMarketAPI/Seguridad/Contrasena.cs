using System.Security.Cryptography;
using System.Text;

namespace PixMarketAPI.Seguridad
{
    /// <summary>
    /// Convierte las contraseñas en un hash para no guardarlas en texto plano.
    ///
    /// Usa PBKDF2 con SHA-256, una sal aleatoria de 16 bytes por contraseña y
    /// 100 000 iteraciones. Todo eso viene incluido en .NET, así que no hace
    /// falta instalar ningún paquete.
    ///
    /// Lo que se guarda en la columna Contrasenia tiene esta forma:
    ///
    ///     PBKDF2$100000&lt;sal en base64&gt;$&lt;hash en base64&gt;
    ///
    /// Dos cuentas con la misma contraseña ("123456") guardan textos distintos,
    /// porque cada una tiene su propia sal. Por eso la base de datos ya no
    /// sirve para saber qué contraseñas se usan.
    ///
    /// Aviso: un hash no se puede "des-hashear". Si se pierde una contraseña
    /// hay que asignarle una nueva, no recuperarla.
    /// </summary>
    public static class Contrasena
    {
        // Si el formato guardado no empieza con esto, es una contraseña en
        // texto plano de las que ya había en la base de datos.
        private const string Prefijo = "PBKDF2$";

        private const int Iteraciones = 100_000;
        private const int BytesSal = 16;
        private const int BytesHash = 32;

        private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

        /// <summary>
        /// Devuelve el hash de una contraseña, listo para guardarse.
        /// Nunca devuelve la misma cadena dos veces: la sal es aleatoria.
        /// </summary>
        public static string Hashear(string clave)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clave);

            var sal = RandomNumberGenerator.GetBytes(BytesSal);

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(clave),
                sal,
                Iteraciones,
                Algoritmo,
                BytesHash);

            return string.Join('$',
                Prefijo.TrimEnd('$'),
                Iteraciones.ToString(),
                Convert.ToBase64String(sal),
                Convert.ToBase64String(hash));
        }

        /// <summary>
        /// Indica si el valor guardado ya es un hash nuestro.
        /// Si devuelve false es una contraseña en texto plano.
        /// </summary>
        public static bool EstaCifrada(string? valorGuardada)
            => valorGuardada is not null &&
               valorGuardada.StartsWith(Prefijo, StringComparison.Ordinal);

        /// <summary>
        /// Comprueba si una contraseña corresponde al valor guardado.
        ///
        /// Acepta las dos formas: si el valor es un hash lo verifica con
        /// PBKDF2, y si es texto plano (las cuentas viejas) lo compara
        /// directamente para que esos usuarios puedan entrar y se les
        /// recifre la contraseña en el siguiente inicio de sesión.
        /// </summary>
        public static bool Verificar(string clave, string? valorGuardada)
        {
            if (string.IsNullOrEmpty(valorGuardada) || string.IsNullOrEmpty(clave))
            {
                return false;
            }

            if (!EstaCifrada(valorGuardada))
            {
                return string.Equals(clave, valorGuardada, StringComparison.Ordinal);
            }

            // Formato: PBKDF2$iteraciones$sal$hash
            var partes = valorGuardada.Split('$');

            if (partes.Length != 4)
            {
                return false;
            }

            if (!int.TryParse(partes[1], out var iteraciones) || iteraciones <= 0)
            {
                return false;
            }

            byte[] sal;
            byte[] hashEsperado;

            try
            {
                sal = Convert.FromBase64String(partes[2]);
                hashEsperado = Convert.FromBase64String(partes[3]);
            }
            catch (FormatException)
            {
                // El valor guardado está dañado: se trata como no coincidente.
                return false;
            }

            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(clave),
                sal,
                iteraciones,
                Algoritmo,
                hashEsperado.Length);

            // Comparación en tiempo constante: no se puede adivinar el hash
            // probando caracteres uno por uno.
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
    }
}
