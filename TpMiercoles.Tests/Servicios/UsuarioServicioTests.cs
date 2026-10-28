using Microsoft.AspNetCore.Identity;
using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class UsuarioServicioTests
    {
        private readonly Mock<IUsuarioRepositorio> _repositorioMock;
        private readonly PasswordHasher<Usuario> _hasher;
        private readonly UsuarioServicio _servicio;

        public UsuarioServicioTests()
        {
            _repositorioMock = new Mock<IUsuarioRepositorio>();
            _hasher = new PasswordHasher<Usuario>();
            _servicio = new UsuarioServicio(MapperFactory.Crear(), _repositorioMock.Object, _hasher);
        }

        [Fact]
        public async Task RegistrarAsync_CuandoElEmailYaExiste_LanzaEmailExistenteException()
        {
            var dto = CrearUsuarioDto();
            _repositorioMock
                .Setup(r => r.ExisteEmailAsync(dto.Email))
                .ReturnsAsync(true);

            var excepcion = await Assert.ThrowsAsync<EmailExistenteException>(
                () => _servicio.RegistrarAsync(dto));

            Assert.Equal("El email ya está registrado", excepcion.Message);
            _repositorioMock.Verify(
                r => r.RegistrarAsync(It.IsAny<Usuario>()),
                Times.Never);
        }

        [Fact]
        public async Task RegistrarAsync_CuandoElEmailNoExiste_RegistraElUsuarioConLaContraseniaHasheada()
        {
            var dto = CrearUsuarioDto();
            Usuario? usuarioGuardado = null;

            _repositorioMock
                .Setup(r => r.ExisteEmailAsync(dto.Email))
                .ReturnsAsync(false);
            _repositorioMock
                .Setup(r => r.RegistrarAsync(It.IsAny<Usuario>()))
                .Callback<Usuario>(usuario => usuarioGuardado = usuario)
                .Returns(Task.CompletedTask);

            await _servicio.RegistrarAsync(dto);

            Assert.NotNull(usuarioGuardado);
            Assert.Equal(dto.Nombre, usuarioGuardado!.Nombre);
            Assert.Equal(dto.Apellido, usuarioGuardado.Apellido);
            Assert.Equal(dto.Email, usuarioGuardado.Email);
            Assert.NotEqual(dto.ContraseniaPlana, usuarioGuardado.ContraseniaHash);
            Assert.NotEqual(
                PasswordVerificationResult.Failed,
                _hasher.VerifyHashedPassword(usuarioGuardado, usuarioGuardado.ContraseniaHash, dto.ContraseniaPlana));
            _repositorioMock.Verify(
                r => r.RegistrarAsync(usuarioGuardado),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_CuandoElEmailNoExiste_DevuelveNull()
        {
            var dto = CrearLoginDto();
            _repositorioMock
                .Setup(r => r.BuscarPorEmailAsync(dto.Email))
                .ReturnsAsync((Usuario?)null);

            var resultado = await _servicio.LoginAsync(dto);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task LoginAsync_CuandoLaContraseniaEsIncorrecta_DevuelveNull()
        {
            var dto = CrearLoginDto("incorrecta");
            var usuario = CrearUsuarioConContrasenia("Contrasenia123");
            _repositorioMock
                .Setup(r => r.BuscarPorEmailAsync(dto.Email))
                .ReturnsAsync(usuario);

            var resultado = await _servicio.LoginAsync(dto);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task LoginAsync_CuandoLasCredencialesSonCorrectas_DevuelveLaSesion()
        {
            var dto = CrearLoginDto("Contrasenia123");
            var usuario = CrearUsuarioConContrasenia("Contrasenia123");
            _repositorioMock
                .Setup(r => r.BuscarPorEmailAsync(dto.Email))
                .ReturnsAsync(usuario);

            var resultado = await _servicio.LoginAsync(dto);

            Assert.NotNull(resultado);
            Assert.Equal(usuario.Id, resultado!.Id);
            Assert.Equal(usuario.Nombre, resultado.Nombre);
            Assert.Equal(usuario.Email, resultado.Email);
        }

        private Usuario CrearUsuarioConContrasenia(string contraseniaPlana)
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "test",
                Apellido = "unitario",
                Email = "test@test.com"
            };
            usuario.ContraseniaHash = _hasher.HashPassword(usuario, contraseniaPlana);
            return usuario;
        }

        private static UsuarioDTO CrearUsuarioDto()
        {
            return new UsuarioDTO
            {
                Nombre = "test",
                Apellido = "unitario",
                Email = "test@test.com",
                ContraseniaPlana = "Contrasenia123"
            };
        }

        private static UsuarioLoginDTO CrearLoginDto(string contrasenia = "Contrasenia123")
        {
            return new UsuarioLoginDTO
            {
                Email = "test@test.com",
                ContraseniaPlana = contrasenia
            };
        }
    }
}
