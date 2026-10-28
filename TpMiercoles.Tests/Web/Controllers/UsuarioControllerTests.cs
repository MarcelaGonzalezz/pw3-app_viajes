using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Tests.Helpers;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Web.Controllers
{
    public class UsuarioControllerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUsuarioServicio> _servicioMock;
        private readonly Mock<ISesionWeb> _sesionMock;
        private readonly UsuarioController _controller;

        public UsuarioControllerTests()
        {
            _mapper = MapperFactory.Crear();
            _servicioMock = new Mock<IUsuarioServicio>();
            _sesionMock = new Mock<ISesionWeb>();

            _servicioMock
                .Setup(s => s.RegistrarAsync(It.IsAny<UsuarioDTO>()))
                .Returns(Task.CompletedTask);
            _sesionMock
                .Setup(s => s.IniciarAsync(It.IsAny<UsuarioSesionDTO>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _controller = new UsuarioController(_servicioMock.Object, _mapper, _sesionMock.Object);
        }

        [Fact]
        public void Login_Get_DevuelveVista()
        {
            var resultado = _controller.Login();

            Assert.IsType<ViewResult>(resultado);
        }

        [Fact]
        public void Registrar_Get_DevuelveVista()
        {
            var resultado = _controller.Registrar();

            Assert.IsType<ViewResult>(resultado);
        }

        [Fact]
        public async Task Registrar_CuandoModelStateEsInvalido_DevuelveVistaSinRegistrar()
        {
            _controller.ModelState.AddModelError(nameof(RegistroUsuarioViewModel.Email), "El Email es obligatorio");
            var modelo = CrearRegistroValido();

            var resultado = await _controller.Registrar(modelo);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Same(modelo, vista.Model);
            _servicioMock.Verify(
                s => s.RegistrarAsync(It.IsAny<UsuarioDTO>()),
                Times.Never);
        }

        [Fact]
        public async Task Registrar_CuandoDatosSonValidos_RegistraYRedirigeAlLogin()
        {
            var modelo = CrearRegistroValido();
            UsuarioDTO? dtoEnviado = null;

            _servicioMock
                .Setup(s => s.RegistrarAsync(It.IsAny<UsuarioDTO>()))
                .Callback<UsuarioDTO>(dto => dtoEnviado = dto)
                .Returns(Task.CompletedTask);

            var resultado = await _controller.Registrar(modelo);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Login", redireccion.ActionName);
            Assert.NotNull(dtoEnviado);
            Assert.Equal(modelo.Nombre, dtoEnviado!.Nombre);
            Assert.Equal(modelo.Apellido, dtoEnviado.Apellido);
            Assert.Equal(modelo.Email, dtoEnviado.Email);
            Assert.Equal(modelo.Contrasenia, dtoEnviado.ContraseniaPlana);
            _servicioMock.Verify(
                s => s.RegistrarAsync(It.IsAny<UsuarioDTO>()),
                Times.Once);
        }

        [Fact]
        public async Task Registrar_CuandoElEmailYaExiste_MuestraErrorEnElCampoEmail()
        {
            var modelo = CrearRegistroValido();
            const string mensaje = "El email ya está registrado";

            _servicioMock
                .Setup(s => s.RegistrarAsync(It.IsAny<UsuarioDTO>()))
                .ThrowsAsync(new EmailExistenteException(mensaje));

            var resultado = await _controller.Registrar(modelo);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Same(modelo, vista.Model);
            Assert.True(_controller.ModelState.TryGetValue(nameof(modelo.Email), out var entrada));
            Assert.Contains(entrada!.Errors, error => error.ErrorMessage == mensaje);
        }

        [Fact]
        public async Task Login_CuandoModelStateEsInvalido_DevuelveVistaSinIniciarSesion()
        {
            _controller.ModelState.AddModelError(nameof(LoginUsuarioViewModel.Email), "El Email es obligatorio");
            var modelo = CrearLoginValido();

            var resultado = await _controller.Login(modelo);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Same(modelo, vista.Model);
            _servicioMock.Verify(
                s => s.LoginAsync(It.IsAny<UsuarioLoginDTO>()),
                Times.Never);
            _sesionMock.Verify(
                s => s.IniciarAsync(It.IsAny<UsuarioSesionDTO>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_CuandoLasCredencialesSonIncorrectas_MuestraErrorGeneral()
        {
            var modelo = CrearLoginValido();
            _servicioMock
                .Setup(s => s.LoginAsync(It.IsAny<UsuarioLoginDTO>()))
                .ReturnsAsync((UsuarioSesionDTO?)null);

            var resultado = await _controller.Login(modelo);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Same(modelo, vista.Model);
            Assert.Contains(
                _controller.ModelState.Values.SelectMany(entrada => entrada.Errors),
                error => error.ErrorMessage == "Email o contraseña incorrectos");
            _sesionMock.Verify(
                s => s.IniciarAsync(It.IsAny<UsuarioSesionDTO>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Login_CuandoLasCredencialesSonValidas_IniciaSesionYRedirigeAlHome(bool recordarme)
        {
            var modelo = CrearLoginValido();
            modelo.Recordarme = recordarme;
            var sesion = new UsuarioSesionDTO
            {
                Id = 7,
                Nombre = "test",
                Email = modelo.Email
            };
            UsuarioLoginDTO? dtoEnviado = null;

            _servicioMock
                .Setup(s => s.LoginAsync(It.IsAny<UsuarioLoginDTO>()))
                .Callback<UsuarioLoginDTO>(dto => dtoEnviado = dto)
                .ReturnsAsync(sesion);

            var resultado = await _controller.Login(modelo);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redireccion.ActionName);
            Assert.Equal("Home", redireccion.ControllerName);
            Assert.NotNull(dtoEnviado);
            Assert.Equal(modelo.Email, dtoEnviado!.Email);
            Assert.Equal(modelo.Contrasenia, dtoEnviado.ContraseniaPlana);
            _sesionMock.Verify(
                s => s.IniciarAsync(sesion, recordarme),
                Times.Once);
        }

        private static RegistroUsuarioViewModel CrearRegistroValido()
        {
            return new RegistroUsuarioViewModel
            {
                Nombre = "test",
                Apellido = "unitario",
                Email = "test@test.com",
                Contrasenia = "Contrasenia123",
                RepetirContrasenia = "Contrasenia123"
            };
        }

        private static LoginUsuarioViewModel CrearLoginValido()
        {
            return new LoginUsuarioViewModel
            {
                Email = "test@test.com",
                Contrasenia = "Contrasenia123",
                Recordarme = false
            };
        }
    }
}
