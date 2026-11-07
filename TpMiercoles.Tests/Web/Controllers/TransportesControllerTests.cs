using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Tests.Helpers;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Web.Controllers
{
    public class TransportesControllerTests
    {
        private const int UsuarioId = 5;

        private readonly Mock<ITransporteServicio> _servicioMock = new();
        private readonly Mock<ISesionWeb> _sesionMock = new();
        private readonly TransportesController _controller;

        public TransportesControllerTests()
        {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns(UsuarioId);
            _servicioMock
                .Setup(s => s.ListarAsync(It.IsAny<int>(), It.IsAny<EstadoEnvio?>()))
                .ReturnsAsync([]);

            var httpContext = new DefaultHttpContext();
            _controller = new TransportesController(_servicioMock.Object, _sesionMock.Object, MapperFactory.Crear())
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        private void SinSesion() => _sesionMock.SetupGet(s => s.UsuarioId).Returns((int?)null);

        private static TransporteListadoDTO Transporte(
            int asignacionId, EstadoEnvio estado, EstadoEnvio? proximo, string descripcion = "Zapatillas")
            => new()
            {
                AsignacionId = asignacionId,
                ViajeId = 10,
                EnvioId = 100 + asignacionId,
                Descripcion = descripcion,
                Origen = "Plaza Italia",
                Destino = "Obelisco",
                Ganancia = 4000m,
                Estado = estado,
                ProximoEstado = proximo
            };

        [Fact]
        public void Controller_TieneAtributoAuthorize()
        {
            Assert.True(Attribute.IsDefined(typeof(TransportesController), typeof(AuthorizeAttribute)));
        }

        // ---------- Index ----------

        [Fact]
        public async Task Index_SinSesion_DevuelveChallenge()
        {
            SinSesion();

            var resultado = await _controller.Index();

            Assert.IsType<ChallengeResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Index_ConFiltroTodos_MuestraEnCursoYEntregados()
        {
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null))
                .ReturnsAsync(
                [
                    Transporte(1, EstadoEnvio.ConductorAsignado, EstadoEnvio.Retirado),
                    Transporte(2, EstadoEnvio.Entregado, null)
                ]);

            var resultado = await _controller.Index("todos");

            var modelo = Assert.IsType<MisTransportesViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Equal(2, modelo.Transportes.Count);
            Assert.True(modelo.Transportes[0].TieneAccion);
            Assert.False(modelo.Transportes[1].TieneAccion);
            Assert.Equal("Entregado", modelo.Transportes[1].EstadoTexto);
        }

        [Fact]
        public async Task Index_PorDefecto_MuestraSoloLosQueNoEstanEntregados()
        {
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null))
                .ReturnsAsync(
                [
                    Transporte(1, EstadoEnvio.Entregado, null),
                    Transporte(2, EstadoEnvio.Retirado, EstadoEnvio.EnCamino),
                    Transporte(3, EstadoEnvio.ConductorAsignado, EstadoEnvio.Retirado)
                ]);

            var resultado = await _controller.Index();

            var modelo = Assert.IsType<MisTransportesViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Equal("encurso", modelo.Filtro);
            Assert.Equal(2, modelo.Transportes.Count);
            Assert.DoesNotContain(modelo.Transportes, t => t.Estado == EstadoEnvio.Entregado);
            Assert.Equal(2, modelo.TotalEnCurso);
            Assert.Equal(1, modelo.TotalEntregados);
        }

        [Fact]
        public async Task Index_ConFiltroEntregados_MuestraSoloLosEntregados()
        {
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null))
                .ReturnsAsync(
                [
                    Transporte(1, EstadoEnvio.Entregado, null),
                    Transporte(2, EstadoEnvio.Retirado, EstadoEnvio.EnCamino)
                ]);

            var resultado = await _controller.Index("entregados");

            var modelo = Assert.IsType<MisTransportesViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Single(modelo.Transportes);
            Assert.Equal(EstadoEnvio.Entregado, modelo.Transportes[0].Estado);
        }

        [Fact]
        public async Task Index_CadaTransporteTraeSuBotonDeLaAccionSiguiente()
        {
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null))
                .ReturnsAsync(
                [
                    Transporte(1, EstadoEnvio.ConductorAsignado, EstadoEnvio.Retirado),
                    Transporte(2, EstadoEnvio.Retirado, EstadoEnvio.EnCamino),
                    Transporte(3, EstadoEnvio.EnCamino, EstadoEnvio.Entregado)
                ]);

            var resultado = await _controller.Index();

            var modelo = Assert.IsType<MisTransportesViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Equal(
                new[] { "Marcar retirado", "Marcar en camino", "Marcar entregado" },
                modelo.Transportes.Select(t => t.AccionTexto).ToArray());
            Assert.Equal(
                new[] { nameof(TransportesController.Retirar), nameof(TransportesController.MarcarEnCamino), nameof(TransportesController.Entregar) },
                modelo.Transportes.Select(t => t.AccionNombre).ToArray());
        }

        // ---------- Detalle ("Ver envio") ----------

        [Fact]
        public async Task Detalle_SinSesion_DevuelveChallenge()
        {
            SinSesion();

            var resultado = await _controller.Detalle(1);

            Assert.IsType<ChallengeResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Detalle_CuandoNoExisteOEsDeOtroConductor_DevuelveNotFound()
        {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(99, UsuarioId))
                .ReturnsAsync((TransporteDetalleDTO?)null);

            var resultado = await _controller.Detalle(99);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Detalle_CuandoExiste_DevuelveLaVistaConLosDatosDelPaquete()
        {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(1, UsuarioId))
                .ReturnsAsync(new TransporteDetalleDTO
                {
                    AsignacionId = 1,
                    ViajeId = 10,
                    EnvioId = 101,
                    Descripcion = "Zapatillas",
                    Origen = "Plaza Italia",
                    Destino = "Obelisco",
                    Ganancia = 4000m,
                    Estado = EstadoEnvio.Retirado,
                    ProximoEstado = EstadoEnvio.EnCamino,
                    RemitenteNombre = "Carla Méndez",
                    RemitenteEmail = "carla@test.com",
                    ViajeOrigen = "Belgrano",
                    ViajeDestino = "Lomas de Zamora",
                    FechaAceptacion = new DateTime(2026, 11, 1, 8, 0, 0),
                    FechaRetiro = new DateTime(2026, 11, 2, 8, 0, 0)
                });

            var resultado = await _controller.Detalle(1);

            var modelo = Assert.IsType<TransporteDetalleViewModel>(Assert.IsType<ViewResult>(resultado).Model);
            Assert.Equal(10, modelo.ViajeId);
            Assert.Equal("Zapatillas", modelo.Titulo);
            Assert.Equal("Carla Méndez", modelo.RemitenteNombre);
            Assert.Equal("carla@test.com", modelo.RemitenteEmail);
            Assert.Equal("Belgrano", modelo.ViajeOrigen);
            Assert.Equal("Retirado", modelo.EstadoTexto);
            Assert.True(modelo.EnCurso);
        }

        [Fact]
        public void Detalle_LaLineaDeTiempoMarcaLosPasosHechosYElActual()
        {
            var modelo = new TransporteDetalleViewModel { Estado = EstadoEnvio.Retirado };

            List<PasoTransporteViewModel> pasos = modelo.ObtenerPasos();

            Assert.Equal(["Aceptado", "Retirado", "En camino", "Entregado"], pasos.Select(p => p.Texto).ToArray());
            Assert.Equal(new[] { true, true, false, false }, pasos.Select(p => p.Hecho).ToArray());
            Assert.Equal(new[] { false, true, false, false }, pasos.Select(p => p.Actual).ToArray());
        }

        [Fact]
        public void Detalle_UnEnvioEntregadoTieneTodosLosPasosHechos()
        {
            var modelo = new TransporteDetalleViewModel { Estado = EstadoEnvio.Entregado };

            Assert.All(modelo.ObtenerPasos(), p => Assert.True(p.Hecho));
            Assert.False(modelo.EnCurso);
        }

        // ---------- Acciones: retirar / en camino / entregar ----------

        [Theory]
        [InlineData("retirar", EstadoEnvio.Retirado)]
        [InlineData("en-camino", EstadoEnvio.EnCamino)]
        [InlineData("entregar", EstadoEnvio.Entregado)]
        public async Task Accion_PideAlServicioElPasoCorrespondiente(string paso, EstadoEnvio destino)
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, UsuarioId, destino))
                .ReturnsAsync(ResultadoOperacionTransporte.Exitoso(Transporte(1, destino, null)));

            await Ejecutar(paso, 1, volverADetalle: false);

            _servicioMock.Verify(s => s.AvanzarAsync(1, UsuarioId, destino), Times.Once);
        }

        [Theory]
        [InlineData("retirar")]
        [InlineData("en-camino")]
        [InlineData("entregar")]
        public async Task Accion_SinSesion_DevuelveChallengeYNoAvanza(string paso)
        {
            SinSesion();

            var resultado = await Ejecutar(paso, 1, volverADetalle: false);

            Assert.IsType<ChallengeResult>(resultado);
            _servicioMock.Verify(
                s => s.AvanzarAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EstadoEnvio>()), Times.Never);
        }

        [Theory]
        [InlineData("retirar")]
        [InlineData("en-camino")]
        [InlineData("entregar")]
        public async Task Accion_CuandoNoExisteOEsDeOtroConductor_DevuelveNotFound(string paso)
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, UsuarioId, It.IsAny<EstadoEnvio>()))
                .ReturnsAsync(ResultadoOperacionTransporte.NoExiste());

            var resultado = await Ejecutar(paso, 1, volverADetalle: false);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Accion_CuandoLaTransicionNoCorresponde_GuardaElErrorEnTempData()
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, UsuarioId, EstadoEnvio.Entregado))
                .ReturnsAsync(ResultadoOperacionTransporte.EnConflicto(
                    "No se puede marcar como entregado un paquete que todavía no fue retirado."));

            var resultado = await Ejecutar("entregar", 1, volverADetalle: false);

            Assert.Equal(
                "No se puede marcar como entregado un paquete que todavía no fue retirado.",
                _controller.TempData["Error"]);
            Assert.False(_controller.TempData.ContainsKey("Mensaje"));
            Assert.IsType<RedirectToActionResult>(resultado);
        }

        [Fact]
        public async Task Retirar_CuandoSaleBien_GuardaElMensajeYVuelveALaLista()
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, UsuarioId, EstadoEnvio.Retirado))
                .ReturnsAsync(ResultadoOperacionTransporte.Exitoso(Transporte(1, EstadoEnvio.Retirado, EstadoEnvio.EnCamino)));

            var resultado = await _controller.Retirar(1);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal(nameof(TransportesController.Index), redireccion.ActionName);
            Assert.Contains("retirado", (string)_controller.TempData["Mensaje"]!);
        }

        [Fact]
        public async Task Entregar_CuandoSaleBien_GuardaElMensaje()
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, UsuarioId, EstadoEnvio.Entregado))
                .ReturnsAsync(ResultadoOperacionTransporte.Exitoso(Transporte(1, EstadoEnvio.Entregado, null)));

            var resultado = await _controller.Entregar(1);

            Assert.IsType<RedirectToActionResult>(resultado);
            string mensaje = (string)_controller.TempData["Mensaje"]!;
            Assert.Contains("Zapatillas", mensaje);
            Assert.Contains("entregado", mensaje);
        }

        [Fact]
        public async Task Accion_VolviendoADetalle_RedirigeAlDetalleDelTransporte()
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, UsuarioId, EstadoEnvio.EnCamino))
                .ReturnsAsync(ResultadoOperacionTransporte.Exitoso(Transporte(1, EstadoEnvio.EnCamino, EstadoEnvio.Entregado)));

            var resultado = await _controller.MarcarEnCamino(1, volverADetalle: true);

            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal(nameof(TransportesController.Detalle), redireccion.ActionName);
            Assert.Equal(1, redireccion.RouteValues!["id"]);
        }

        private Task<IActionResult> Ejecutar(string paso, int id, bool volverADetalle)
            => paso switch
            {
                "retirar" => _controller.Retirar(id, volverADetalle),
                "en-camino" => _controller.MarcarEnCamino(id, volverADetalle),
                "entregar" => _controller.Entregar(id, volverADetalle),
                _ => throw new ArgumentOutOfRangeException(nameof(paso))
            };
    }
}
