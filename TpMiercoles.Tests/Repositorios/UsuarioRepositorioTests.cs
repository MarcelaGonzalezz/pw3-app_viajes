using Microsoft.EntityFrameworkCore;
using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Repositorios
{
    public class UsuarioRepositorioTests
    {
        [Fact]
        public async Task ExisteEmailAsync_CuandoElEmailExiste_DevuelveTrue()
        {
            var usuario = CrearUsuario("existente@test.com");
            var db = new DbContextMockFactory([usuario]);
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);

            var existe = await repositorio.ExisteEmailAsync("existente@test.com");

            Assert.True(existe);
        }

        [Fact]
        public async Task ExisteEmailAsync_CuandoElEmailNoExiste_DevuelveFalse()
        {
            var usuario = CrearUsuario("existente@test.com");
            var db = new DbContextMockFactory([usuario]);
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);

            var existe = await repositorio.ExisteEmailAsync("otro@test.com");

            Assert.False(existe);
        }

        [Fact]
        public async Task BuscarPorEmailAsync_CuandoElUsuarioExiste_DevuelveElUsuario()
        {
            var usuario = CrearUsuario("existente@test.com");
            var db = new DbContextMockFactory([usuario]);
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);

            var resultado = await repositorio.BuscarPorEmailAsync("existente@test.com");

            Assert.Same(usuario, resultado);
        }

        [Fact]
        public async Task BuscarPorEmailAsync_CuandoElUsuarioNoExiste_DevuelveNull()
        {
            var db = new DbContextMockFactory([CrearUsuario("existente@test.com")]);
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);

            var resultado = await repositorio.BuscarPorEmailAsync("otro@test.com");

            Assert.Null(resultado);
        }

        [Fact]
        public async Task RegistrarAsync_CuandoElEmailNoEstaDuplicado_AgregaYGuardaElUsuario()
        {
            var db = new DbContextMockFactory();
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);
            var usuario = CrearUsuario("nuevo@test.com");

            await repositorio.RegistrarAsync(usuario);

            db.Contexto.Verify(c => c.Add(usuario), Times.Once);
            db.Contexto.Verify(
                c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegistrarAsync_CuandoElEmailEstaDuplicado_LanzaEmailExistenteException()
        {
            var existente = CrearUsuario("duplicado@test.com");
            var db = new DbContextMockFactory([existente]);
            db.Contexto
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Violación de índice único", new Exception("UNIQUE constraint failed")));
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);

            var excepcion = await Assert.ThrowsAsync<EmailExistenteException>(
                () => repositorio.RegistrarAsync(CrearUsuario("duplicado@test.com")));

            Assert.Equal("El email ya está registrado", excepcion.Message);
            db.Contexto.Verify(
                c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegistrarAsync_CuandoDbUpdateExceptionNoEsPorEmail_RelanzaLaExcepcionOriginal()
        {
            var db = new DbContextMockFactory();
            var excepcionOriginal = new DbUpdateException("Otro error", new Exception("Fallo de base"));
            db.Contexto
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(excepcionOriginal);
            var repositorio = new UsuarioRepositorio(db.Contexto.Object);

            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
                () => repositorio.RegistrarAsync(CrearUsuario("nuevo@test.com")));

            Assert.Same(excepcionOriginal, excepcion);
        }

        private static Usuario CrearUsuario(string email)
        {
            return new Usuario
            {
                Id = 1,
                Nombre = "test",
                Apellido = "test",
                Email = email,
                ContraseniaHash = "hash"
            };
        }
    }
}
