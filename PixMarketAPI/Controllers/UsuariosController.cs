using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using PixMarketAPI.Models;
using PixMarketAPI.Seguridad;

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

        /// <summary>
        /// Inicia sesión con correo y contraseña.
        /// </summary>
        /// <param name="login">Cuerpo JSON con Correo y Contrasenia.</param>
        /// <returns>Datos del usuario (sin contraseña).</returns>
        [HttpPost("login")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

            // Se busca solo por correo. La contraseña se compara después con
            // Contrasena.Verificar, porque en la base está cifrada y no se
            // puede filtrar con un "WHERE Contrasenia = ...".
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == login.Correo);

            if (usuario == null ||
                !Contrasena.Verificar(login.Contrasenia, usuario.Contrasenia))
            {
                return Unauthorized(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El correo o la contraseña son incorrectos."
                });
            }

            // Las cuentas anteriores a esto tenían la contraseña en texto
            // plano. Aprovechamos este inicio de sesión para cifrarla, sin
            // obligar al usuario a cambiar nada.
            if (!Contrasena.EstaCifrada(usuario.Contrasenia))
            {
                usuario.Contrasenia = Contrasena.Hashear(login.Contrasenia);
                await _context.SaveChangesAsync();
            }

            return Ok(new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol
            });
        }

        /// <summary>
        /// Registra un usuario nuevo con rol "Usuario".
        /// </summary>
        /// <param name="registro">Cuerpo JSON con Nombre, Correo, Contrasenia (mínimo 6) y Telefono (solo números).</param>
        /// <returns>Datos del usuario creado (sin contraseña).</returns>
        [HttpPost("registro")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            /*cambiar a string telefono*/

           /* if (!int.TryParse(registro.Telefono, out int telefonoNumero))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El teléfono debe contener solamente números."
                });
            }*/

            if (string.IsNullOrWhiteSpace(registro.Telefono))
            
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El telefono es obligatorio"
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

                // En la base solo se guarda el hash, nunca la contraseña.
                Contrasenia = Contrasena.Hashear(registro.Contrasenia),

                Telefono = registro.Telefono.Trim(),
                Rol = registro.Rol == "Administrador" ? "Administrador" : "Usuario"
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
                Rol = nuevoUsuario.Rol,
                Telefono = nuevoUsuario.Telefono,
                Estado = nuevoUsuario.Estado
            });
        }

        /// <summary>
        /// Obtiene un usuario por su identificador.
        /// </summary>
        /// <param name="id">Identificador del usuario.</param>
        /// <returns>Datos del usuario (sin contraseña).</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                Rol = usuario.Rol,
                Telefono = usuario.Telefono,
                Estado = usuario.Estado
            });
        }

        //metodos

        // GET: api/usuarios
        [HttpGet]
        [ProducesResponseType(typeof(List<UsuarioDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UsuarioDto>>> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.Nombre)
                .Select(u => new UsuarioDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Rol = u.Rol,
                    Telefono = u.Telefono,
                    Estado = u.Estado
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // PUT: api/usuarios/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Edit(int id, [FromBody] UsuarioDto usuarioDto)
        {
            if (id != usuarioDto.Id)
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El ID no coincide."
                });
            }

            if (string.IsNullOrWhiteSpace(usuarioDto.Nombre))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El nombre es obligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(usuarioDto.Correo))
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El correo es obligatorio."
                });
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El usuario no existe."
                });
            }

            // El login busca por correo, así que no puede haber dos cuentas iguales.
            var correoRepetido = await _context.Usuarios
                .AnyAsync(u => u.Correo == usuarioDto.Correo.Trim() && u.Id != id);

            if (correoRepetido)
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El correo ya está registrado."
                });
            }

            usuario.Nombre = usuarioDto.Nombre.Trim();
            usuario.Correo = usuarioDto.Correo.Trim();
            usuario.Telefono = usuarioDto.Telefono;
            usuario.Rol = usuarioDto.Rol ?? usuario.Rol;
            usuario.Estado = usuarioDto.Estado ?? usuario.Estado;

            await _context.SaveChangesAsync();

            return Ok(new MensajeResultado
            {
                Ok = true,
                Mensaje = "Usuario actualizado correctamente."
            });
        }

        // PATCH: api/usuarios/{id}/estado
        [HttpPatch("{id:int}/estado")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
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

            // Solo se aceptan los estados definidos por el panel, para no
            // guardar valores arbitrarios en la base de datos.
            var estado = request.Estado?.Trim();

            if (estado != "Activo" && estado != "Suspendido")
            {
                return BadRequest(new MensajeResultado
                {
                    Ok = false,
                    Mensaje = "El estado debe ser 'Activo' o 'Suspendido'."
                });
            }

            usuario.Estado = estado;
            await _context.SaveChangesAsync();

            return Ok(new MensajeResultado
            {
                Ok = true,
                Mensaje = "Estado actualizado correctamente."
            });
        }




    }
}