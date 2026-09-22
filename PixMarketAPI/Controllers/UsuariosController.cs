using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;

namespace PixMarketAPI.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly PixContext _context;

        public UsuariosController(PixContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UsuarioDto>> Login([FromBody] LoginRequest login)
        {
            if (string.IsNullOrWhiteSpace(login.Correo) ||
                string.IsNullOrWhiteSpace(login.Contrasenia))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "Debes ingresar el correo y la contraseña."
                });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo == login.Correo &&
                    u.Contrasenia == login.Contrasenia);

            if (usuario == null)
            {
                return Unauthorized(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El correo o la contraseña son incorrectos."
                });
            }

            return Ok(new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol
            });
        }

        [HttpPost("registro")]
        public async Task<ActionResult<UsuarioDto>> Registro([FromBody] RegistroRequest registro)
        {
            if (string.IsNullOrWhiteSpace(registro.Nombre))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El nombre es obligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(registro.Correo))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El correo es obligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(registro.Contrasenia))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "La contraseña es obligatoria."
                });
            }

            if (registro.Contrasenia.Length < 6)
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "La contraseña debe tener mínimo 6 caracteres."
                });
            }

            if (!int.TryParse(registro.Telefono, out int telefonoNumero))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El teléfono debe contener solamente números."
                });
            }

            var usuarioExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == registro.Correo);

            if (usuarioExistente != null)
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El correo ya está registrado."
                });
            }

            var nuevoUsuario = new Usuario
            {
                Nombre = registro.Nombre.Trim(),
                Correo = registro.Correo.Trim(),
                Contrasenia = registro.Contrasenia,
                Telefono = telefonoNumero,
                Rol = "Usuario"
            };

            try
            {
                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new MensajeResultado
                    {
                        Ok = false,
                        Mensaje = "No se pudo guardar el usuario. " +
                            "Verifica que la base de datos esté disponible."
                    });
            }

            return Ok(new UsuarioDto
            {
                Id = nuevoUsuario.Id,
                Nombre = nuevoUsuario.Nombre,
                Correo = nuevoUsuario.Correo,
                Rol = nuevoUsuario.Rol
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioDto>> Details(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El usuario no existe."
                });
            }

            return Ok(new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol
            });
        }
    }
}