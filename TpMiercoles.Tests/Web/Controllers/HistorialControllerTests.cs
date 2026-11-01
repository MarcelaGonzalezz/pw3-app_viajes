using Microsoft.AspNetCore.Mvc;
using Moq;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Historial;
using TpMiercoles.Tests.Helpers;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Web.Controllers
{
    public class HistorialControllerTests
    {
        private const int UsuarioId = 5;

        private readonly Mock<IHistorialServicio> _servicioMock = new();
        private readonly Mock<ISesionWeb> _sesionMock = new();
        private readonly HistorialController _controller;

        public HistorialControllerTests()
        {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns(UsuarioId);
            _servicioMock
                .Setup(s => s.ObtenerAsync(It.IsAny<int>(), It.IsAny<TipoOperacionHistorial?>(),
                                           It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new HistorialDTO());

            _controller = new HistorialController(_servicioMock.Object, _sesionMock.Object, MapperFactory.Crear());
        }

        [Fact]
        public async Task Index_SinSesion_HaceChallengeYNoConsultaElServicio()
        {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns((int?)null);

            var resultado = await _controller.Index(null, null, null);

            Assert.IsType<ChallengeResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Index_ConsultaElHistorialDelUsuarioLogueado()
        {
            await _controller.Index(TipoOperacionHistorial.ViajeRealizado, null, null);

            _servicioMock.Verify(s => s.ObtenerAsync(
                UsuarioId, TipoOperacionHistorial.ViajeRealizado, null, null), Times.Once);
        }

        [Fact]
        public async Task Index_ArmaElModeloConResumenYFilas()
        {
            _servicioMock
                .Setup(s => s.ObtenerAsync(UsuarioId, null, null, null))
                .ReturnsAsync(new HistorialDTO
                {
                    ViajesRealizados = 2,
                    EnviosTransportados = 5,
                    GananciaEfectiva = 22500m,
                    TotalOperaciones = 1,
                    Items =
                    [
                        new HistorialItemDTO
                        {
                            Tipo = TipoOperacionHistorial.ViajeRealizado,
                            Id = 7,
                            Origen = "Belgrano",
                            Destino = "Lomas de Zamora",
                            Fecha = new DateTime(2026, 9, 10),
                            CantidadPaquetes = 3,
                            Monto = 18000m
                        }
                    ]
                });

            var resultado = await _controller.Index(null, null, null);

            var vista = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsType<HistorialViewModel>(vista.Model);
            Assert.Equal(2, modelo.ViajesRealizados);
            Assert.Equal(5, modelo.EnviosTransportados);
            Assert.Equal(22500m, modelo.GananciaEfectiva);
            HistorialItemViewModel fila = Assert.Single(modelo.Items);
            Assert.True(fila.EsViaje);
            Assert.Contains("3 paquetes", fila.Subtitulo);
        }

        [Fact]
        public async Task Index_ConFechasInvertidas_MuestraErrorYNoFiltraPorFechas()
        {
            var resultado = await _controller.Index(null, new DateTime(2026, 9, 30), new DateTime(2026, 9, 1));

            var modelo = Assert.IsType<HistorialViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.NotNull(modelo.Error);
            Assert.Null(modelo.Desde);
            Assert.Null(modelo.Hasta);
            _servicioMock.Verify(s => s.ObtenerAsync(UsuarioId, null, null, null), Times.Once);
        }
    }
}
