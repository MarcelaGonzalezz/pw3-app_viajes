using Microsoft.AspNetCore.Mvc;
using Moq;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Tests.Helpers;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Web.Controllers
{
    public class GananciasControllerTests
    {
        private const int UsuarioId = 5;

        private static readonly DateTimeOffset MediadosDeNoviembre =
            new(2026, 11, 15, 12, 0, 0, TimeSpan.FromHours(-3));

        private readonly Mock<IGananciasServicio> _servicioMock = new();
        private readonly Mock<ISesionWeb> _sesionMock = new();
        private readonly Mock<IPreferenciasDeGanancias> _preferenciasMock = new();
        private readonly GananciasController _controller;

        public GananciasControllerTests()
        {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns(UsuarioId);
            _servicioMock
                .Setup(s => s.CalcularAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<CriterioOptimizacion>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, int anio, int mes, CriterioOptimizacion criterio, double? _, CancellationToken _) =>
                    Resultado(anio, mes, criterio));

            _controller = new GananciasController(
                _servicioMock.Object,
                _sesionMock.Object,
                MapperFactory.Crear(),
                new RelojFijo(MediadosDeNoviembre),
                _preferenciasMock.Object);
        }

        [Fact]
        public async Task Index_SinSesion_HaceChallengeYNoCalcula()
        {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns((int?)null);

            IActionResult resultado = await _controller.Index(null, null);

            Assert.IsType<ChallengeResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Index_SinParametros_CalculaElMesActualDelUsuarioLogueado()
        {
            await _controller.Index(null, null);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 11, CriterioOptimizacion.MayorGanancia, GananciasViewModel.TopeDesvioKm, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Index_ConParametrosValidos_LosPasaAlServicioYArmaElModelo()
        {
            IActionResult resultado = await _controller.Index(2026, 12, CriterioOptimizacion.MenorDesvio);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 12, CriterioOptimizacion.MenorDesvio, GananciasViewModel.TopeDesvioKm, It.IsAny<CancellationToken>()),
                Times.Once);

            GananciasViewModel modelo =
                Assert.IsType<GananciasViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Null(modelo.Error);
            Assert.Equal(12, modelo.Mes);
            Assert.Equal(CriterioOptimizacion.MenorDesvio, modelo.Criterio);
            Assert.Equal(12000m, modelo.GananciaPotencial);
            Assert.Equal(4000m, modelo.PromedioPorViaje);
        }

        [Theory]
        [InlineData(2026, 13, 0)]
        [InlineData(2026, 0, 0)]
        [InlineData(1999, 5, 0)]
        [InlineData(2026, 5, 7)]
        public async Task Index_ConDatosInvalidos_MuestraElMesActualConAviso(int anio, int mes, int criterio)
        {
            IActionResult resultado = await _controller.Index(anio, mes, (CriterioOptimizacion)criterio);

            GananciasViewModel modelo =
                Assert.IsType<GananciasViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.NotNull(modelo.Error);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 11, CriterioOptimizacion.MayorGanancia, GananciasViewModel.TopeDesvioKm, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Index_SinValorDeDesvio_UsaElTope()
        {
            await _controller.Index(null, null, CriterioOptimizacion.MayorGanancia, desvioMaximoKm: null);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 11, CriterioOptimizacion.MayorGanancia,
                GananciasViewModel.TopeDesvioKm, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Index_ConDesvioValido_LoPasaAlServicio()
        {
            IActionResult resultado = await _controller.Index(
                null, null, CriterioOptimizacion.MayorGanancia, desvioMaximoKm: 7);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 11, CriterioOptimizacion.MayorGanancia, 7.0, It.IsAny<CancellationToken>()),
                Times.Once);

            GananciasViewModel modelo =
                Assert.IsType<GananciasViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Null(modelo.Error);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(10.5)]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Index_ConDesvioFueraDeRango_MuestraAvisoYUsaElTope(double desvio)
        {
            IActionResult resultado = await _controller.Index(
                null, null, CriterioOptimizacion.MenorDesvio, desvioMaximoKm: desvio);

            GananciasViewModel modelo =
                Assert.IsType<GananciasViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.NotNull(modelo.Error);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 11, CriterioOptimizacion.MayorGanancia,
                GananciasViewModel.TopeDesvioKm, It.IsAny<CancellationToken>()),
        Times.Once);
            _preferenciasMock.Verify(p => p.Guardar(It.IsAny<int>(), It.IsAny<PreferenciaDeGanancias>()), Times.Never);
        }

        [Fact]
        public async Task Index_SinTocarElFormulario_UsaLoUltimoQueEligioElUsuario()
        {
            _preferenciasMock
                .Setup(p => p.Leer(UsuarioId))
                .Returns(new PreferenciaDeGanancias(CriterioOptimizacion.Equilibrada, GananciasViewModel.TopeDesvioKm));

            await _controller.Index(null, null);

            _servicioMock.Verify(s => s.CalcularAsync(
                UsuarioId, 2026, 11, CriterioOptimizacion.Equilibrada,
                GananciasViewModel.TopeDesvioKm, It.IsAny<CancellationToken>()),
                Times.Once);
            _preferenciasMock.Verify(p => p.Guardar(It.IsAny<int>(), It.IsAny<PreferenciaDeGanancias>()), Times.Never);
        }

        [Fact]
        public async Task Index_AlEnviarElFormulario_GuardaLaEleccion()
        {
            await _controller.Index(null, null, CriterioOptimizacion.MenorDesvio, desvioMaximoKm: 8);

            _preferenciasMock.Verify(p => p.Guardar(
                UsuarioId, new PreferenciaDeGanancias(CriterioOptimizacion.MenorDesvio, 8)),
                Times.Once);
        }

        [Fact]
        public async Task Index_ConDatosInvalidos_NoGuardaLaEleccion()
        {
            await _controller.Index(2026, 13, CriterioOptimizacion.MenorDesvio);

            _preferenciasMock.Verify(p => p.Guardar(It.IsAny<int>(), It.IsAny<PreferenciaDeGanancias>()), Times.Never);
        }

        private static GananciasMensualDTO Resultado(int anio, int mes, CriterioOptimizacion criterio)
        {
            DateTime inicio = new(anio, mes, 1);
            DateTime fin = inicio.AddMonths(1).AddTicks(-1);

            return new GananciasMensualDTO
            {
                Anio = anio,
                Mes = mes,
                Criterio = criterio,
                DesvioMaximoKm = 5,
                EfectivaDesde = inicio,
                EfectivaHasta = inicio,
                PotencialDesde = inicio,
                PotencialHasta = fin,
                GananciaEfectiva = 0m,
                GananciaComprometida = 0m,
                GananciaPotencial = 12000m,
                ViajesAnalizados = 3,
                PaquetesPosibles = 3,
                EnviosEntregados = 0,
                DesvioTotalKm = 3,
                PesoTotalKg = 6,
                Semanas = [],
                Salidas = [],
                SalidasDescartadas = 0,
                SalidasSinEnviosCompatibles = 0
            };
        }
    }
}
