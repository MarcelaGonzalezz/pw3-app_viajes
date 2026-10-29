using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Tests.Helpers;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Web.Controllers
{
    public class ViajesControllerTests
    {
        private const int UsuarioId = 5;

        private readonly Mock<IViajeServicio> _servicioMock;
        private readonly Mock<ISesionWeb> _sesionMock;
        private readonly IMapper _mapper;
        private readonly ViajesController _controller;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public ViajesControllerTests()
        {
            _servicioMock = new Mock<IViajeServicio>();
            _sesionMock = new Mock<ISesionWeb>();
            _mapper = MapperFactory.Crear();

            _sesionMock.SetupGet(s => s.UsuarioId).Returns(UsuarioId);
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            _controller = new ViajesController(_servicioMock.Object, _sesionMock.Object, _mapper, _httpClientFactoryMock.Object)
            {
                TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Index_CuandoHayViajes_DevuelveElListadoConLosTotales()
        {
            var activo = CrearListadoDto(1, EstadoActividadViaje.ACTIVO);
            var inactivo = CrearListadoDto(2, EstadoActividadViaje.INACTIVO);
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null))
                .ReturnsAsync([activo, inactivo]);

            var resultado = await _controller.Index(null);

            var vista = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsType<MisViajesViewModel>(vista.Model);
            Assert.Null(modelo.Filtro);
            Assert.Equal(new[] { 1, 2 }, modelo.Viajes.Select(viaje => viaje.Id));
            Assert.Equal(1, modelo.TotalActivos);
            Assert.Equal(1, modelo.TotalInactivos);
            Assert.Equal("Ramos Mejía", modelo.Viajes[0].DireccionOrigen);
            Assert.Equal("Microcentro", modelo.Viajes[0].DireccionDestino);
        }

        [Theory]
        [InlineData(EstadoActividadViaje.ACTIVO, 1)]
        [InlineData(EstadoActividadViaje.INACTIVO, 2)]
        public async Task Index_CuandoHayFiltro_DevuelveSoloLosViajesDelEstado(
            EstadoActividadViaje estado,
            int idEsperado)
        {
            var activo = CrearListadoDto(1, EstadoActividadViaje.ACTIVO);
            var inactivo = CrearListadoDto(2, EstadoActividadViaje.INACTIVO);
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null))
                .ReturnsAsync([activo, inactivo]);

            var resultado = await _controller.Index(estado);

            var vista = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsType<MisViajesViewModel>(vista.Model);
            Assert.Equal(estado, modelo.Filtro);
            var viaje = Assert.Single(modelo.Viajes);
            Assert.Equal(idEsperado, viaje.Id);
            Assert.Equal(estado, viaje.Estado);
            Assert.Equal(1, modelo.TotalActivos);
            Assert.Equal(1, modelo.TotalInactivos);
        }

        [Fact]
        public async Task Detalle_CuandoElViajeNoExiste_DevuelveNotFound()
        {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(10, UsuarioId))
                .ReturnsAsync((ViajeDetalleDTO?)null);

            var resultado = await _controller.Detalle(10);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Detalle_CuandoElViajeExiste_DevuelveLaVistaMapeada()
        {
            var dto = new ViajeDetalleDTO
            {
                Id = 10,
                RutaId = 3,
                Estado = EstadoActividadViaje.ACTIVO,
                DireccionOrigen = "Ramos Mejía",
                DireccionDestino = "Microcentro",
                FechaSalida = new DateTime(2026, 11, 2, 8, 0, 0),
                FechaLlegada = new DateTime(2026, 11, 2, 9, 30, 0),
                PesoMax = 25,
                CapacidadMaxPaquetes = 4,
                CantidadPaquetes = 1,
                LugaresDisponibles = 3,
                DiasSemana = [DayOfWeek.Monday],
                Paradas = [new ParadaDTO { Orden = 1, Direccion = new DireccionDTO { Texto = "Ciudadela" } }],
                Paquetes =
                [
                    new PaqueteAsignadoDTO
                    {
                        AsignacionId = 7,
                        EnvioId = 20,
                        Remitente = "Carla Méndez",
                        Estado = EstadoViaje.CONFIRMADO
                    }
                ]
            };
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(10, UsuarioId))
                .ReturnsAsync(dto);

            var resultado = await _controller.Detalle(10);

            var vista = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsType<ViajeDetalleViewModel>(vista.Model);
            Assert.Equal(10, modelo.Id);
            Assert.Equal(3, modelo.RutaId);
            Assert.Equal("Ramos Mejía", modelo.DireccionOrigen);
            Assert.Equal("Microcentro", modelo.DireccionDestino);
            Assert.Equal(25, modelo.PesoMax);
            Assert.Equal(4, modelo.CapacidadMaxPaquetes);
            Assert.Equal(3, modelo.LugaresDisponibles);
            Assert.Equal([DayOfWeek.Monday], modelo.DiasSemana);
            Assert.Equal("Ciudadela", Assert.Single(modelo.Paradas).Direccion.Texto);
            Assert.Equal(7, Assert.Single(modelo.Paquetes).AsignacionId);
            Assert.True(modelo.EstaActivo);
        }

        [Fact]
        public async Task Editar_CuandoElViajeNoExiste_DevuelveNotFound()
        {
            _servicioMock
                .Setup(s => s.ObtenerParaEditarAsync(10, UsuarioId))
                .ReturnsAsync((ViajeEdicionDTO?)null);

            var resultado = await _controller.Editar(10);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Editar_CuandoElViajeExiste_DevuelveLaVistaMapeada()
        {
            var dto = CrearEdicionDto();
            _servicioMock
                .Setup(s => s.ObtenerParaEditarAsync(10, UsuarioId))
                .ReturnsAsync(dto);

            var resultado = await _controller.Editar(10);

            var vista = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsType<ViajeEdicionViewModel>(vista.Model);
            Assert.Equal(dto.Id, modelo.Id);
            Assert.Equal(dto.FechaSalida, modelo.FechaSalida);
            Assert.Equal(dto.FechaLlegada, modelo.FechaLlegada);
            Assert.Equal(dto.PesoMax, modelo.PesoMax);
            Assert.Equal(dto.CapacidadMaxPaquetes, modelo.CapacidadMaxPaquetes);
            Assert.Equal(dto.DireccionOrigen, modelo.DireccionOrigen);
            Assert.Equal(dto.DireccionDestino, modelo.DireccionDestino);
            Assert.Equal(dto.CantidadPaquetes, modelo.CantidadPaquetes);
        }

        [Fact]
        public async Task GuardarEdicion_CuandoModelStateEsInvalido_VuelveAlFormularioConContexto()
        {
            _controller.ModelState.AddModelError(nameof(ViajeEdicionViewModel.PesoMax), "Peso inválido");
            var modelo = CrearEdicionViewModel();
            var contexto = CrearEdicionDto();
            _servicioMock
                .Setup(s => s.ObtenerParaEditarAsync(10, UsuarioId))
                .ReturnsAsync(contexto);

            var resultado = await _controller.EditarDesdeFormulario(10, modelo);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Equal("Editar", vista.ViewName);
            Assert.Same(modelo, vista.Model);
            Assert.Equal(10, modelo.Id);
            Assert.Equal(contexto.DireccionOrigen, modelo.DireccionOrigen);
            Assert.Equal(contexto.DireccionDestino, modelo.DireccionDestino);
            Assert.Equal(contexto.Estado, modelo.Estado);
            Assert.Equal(contexto.CantidadPaquetes, modelo.CantidadPaquetes);
            _servicioMock.Verify(
                s => s.ActualizarAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<ViajeEdicionDTO>()),
                Times.Never);
        }

        [Fact]
        public async Task GuardarEdicion_CuandoElViajeNoExiste_DevuelveNotFound()
        {
            _servicioMock
                .Setup(s => s.ActualizarAsync(10, UsuarioId, It.IsAny<ViajeEdicionDTO>()))
                .ReturnsAsync(ResultadoOperacionViaje.NoExiste());

            var resultado = await _controller.EditarDesdeFormulario(10, CrearEdicionViewModel());

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task GuardarEdicion_CuandoHayErroresDeValidacion_VuelveAlFormularioConLosErrores()
        {
            var modelo = CrearEdicionViewModel();
            var contexto = CrearEdicionDto();
            _servicioMock
                .Setup(s => s.ActualizarAsync(10, UsuarioId, It.IsAny<ViajeEdicionDTO>()))
                .ReturnsAsync(ResultadoOperacionViaje.Invalido(["Error uno", "Error dos"]));
            _servicioMock
                .Setup(s => s.ObtenerParaEditarAsync(10, UsuarioId))
                .ReturnsAsync(contexto);

            var resultado = await _controller.EditarDesdeFormulario(10, modelo);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Equal("Editar", vista.ViewName);
            Assert.Same(modelo, vista.Model);
            Assert.Contains(
                _controller.ModelState.Values.SelectMany(entrada => entrada.Errors),
                error => error.ErrorMessage == "Error uno");
            Assert.Contains(
                _controller.ModelState.Values.SelectMany(entrada => entrada.Errors),
                error => error.ErrorMessage == "Error dos");
            Assert.Equal(contexto.DireccionOrigen, modelo.DireccionOrigen);
            Assert.Equal(contexto.CantidadPaquetes, modelo.CantidadPaquetes);
        }

        [Fact]
        public async Task GuardarEdicion_CuandoEsExitosa_RedirigeAlDetalleYGuardaElMensaje()
        {
            ViajeEdicionDTO? dtoEnviado = null;
            var modelo = CrearEdicionViewModel();
            _servicioMock
                .Setup(s => s.ActualizarAsync(10, UsuarioId, It.IsAny<ViajeEdicionDTO>()))
                .Callback<int, int, ViajeEdicionDTO>((_, _, dto) => dtoEnviado = dto)
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso());

            var resultado = await _controller.EditarDesdeFormulario(10, modelo);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Detalle", redireccion.ActionName);
            Assert.Equal(10, redireccion.RouteValues!["id"]);
            Assert.Equal(10, modelo.Id);
            Assert.NotNull(dtoEnviado);
            Assert.Equal(modelo.FechaSalida, dtoEnviado!.FechaSalida);
            Assert.Equal(modelo.FechaLlegada, dtoEnviado.FechaLlegada);
            Assert.Equal(modelo.PesoMax, dtoEnviado.PesoMax);
            Assert.Equal(modelo.CapacidadMaxPaquetes, dtoEnviado.CapacidadMaxPaquetes);
            var mensaje = Assert.IsType<string>(_controller.TempData["Mensaje"]);
            Assert.Contains("Guardamos los cambios", mensaje);
        }

        [Fact]
        public async Task Actualizar_CuandoEsExitoso_RedirigeAlDetalle()
        {
            _servicioMock
                .Setup(s => s.ActualizarAsync(10, UsuarioId, It.IsAny<ViajeEdicionDTO>()))
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso());

            var resultado = await _controller.Actualizar(10, CrearEdicionViewModel());

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Detalle", redireccion.ActionName);
            Assert.Equal(10, redireccion.RouteValues!["id"]);
            _servicioMock.Verify(
                s => s.ActualizarAsync(10, UsuarioId, It.IsAny<ViajeEdicionDTO>()),
                Times.Once);
        }

        [Fact]
        public async Task Desactivar_CuandoElViajeNoExiste_DevuelveNotFound()
        {
            _servicioMock
                .Setup(s => s.DesactivarAsync(10, UsuarioId))
                .ReturnsAsync(ResultadoOperacionViaje.NoExiste());

            var resultado = await _controller.Desactivar(10);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Desactivar_CuandoTienePaquetes_InformaLaCantidadAfectada()
        {
            _servicioMock
                .Setup(s => s.DesactivarAsync(10, UsuarioId))
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso(paquetesAfectados: 3));

            var resultado = await _controller.Desactivar(10);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redireccion.ActionName);
            var mensaje = Assert.IsType<string>(_controller.TempData["Mensaje"]);
            Assert.Contains("3 paquete(s)", mensaje);
            Assert.Contains("siguen en pie", mensaje);
        }

        [Fact]
        public async Task Activar_CuandoElViajeNoExiste_DevuelveNotFound()
        {
            _servicioMock
                .Setup(s => s.ActivarAsync(10, UsuarioId))
                .ReturnsAsync(ResultadoOperacionViaje.NoExiste());

            var resultado = await _controller.Activar(10);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Activar_CuandoEsExitoso_InformaYRedirigeAlIndex()
        {
            _servicioMock
                .Setup(s => s.ActivarAsync(10, UsuarioId))
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso());

            var resultado = await _controller.Activar(10);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redireccion.ActionName);
            var mensaje = Assert.IsType<string>(_controller.TempData["Mensaje"]);
            Assert.Contains("volvió a estar activo", mensaje);
        }

        private static ViajeListadoDTO CrearListadoDto(int id, EstadoActividadViaje estado)
        {
            return new ViajeListadoDTO
            {
                Id = id,
                Estado = estado,
                DireccionOrigen = "Ramos Mejía",
                DireccionDestino = "Microcentro",
                FechaSalida = new DateTime(2026, 11, 2, 8, 0, 0),
                FechaLlegada = new DateTime(2026, 11, 2, 9, 30, 0),
                CantidadPaquetes = 1,
                CapacidadMaxPaquetes = 4
            };
        }

        private static ViajeEdicionDTO CrearEdicionDto()
        {
            return new ViajeEdicionDTO
            {
                Id = 10,
                FechaSalida = new DateTime(2026, 11, 2, 8, 0, 0),
                FechaLlegada = new DateTime(2026, 11, 2, 9, 30, 0),
                PesoMax = 25,
                CapacidadMaxPaquetes = 4,
                Estado = EstadoActividadViaje.INACTIVO,
                DireccionOrigen = "Ramos Mejía",
                DireccionDestino = "Microcentro",
                CantidadPaquetes = 2
            };
        }

        private static ViajeEdicionViewModel CrearEdicionViewModel()
        {
            return new ViajeEdicionViewModel
            {
                FechaSalida = new DateTime(2026, 12, 1, 9, 0, 0),
                FechaLlegada = new DateTime(2026, 12, 1, 10, 0, 0),
                PesoMax = 40,
                CapacidadMaxPaquetes = 6
            };
        }
    }
}
