using AutoMapper;
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

namespace TpMiercoles.Tests.Controllers;

public class EnvioControllerTests
{
    private const int UsuarioId = 7;

    private readonly IMapper _mapper;
    private readonly Mock<IEnvioServicio> _servicioMock;
    private readonly Mock<ISesionWeb> _sesionMock;
    private readonly EnvioController _controller;

    public EnvioControllerTests()
    {
        _mapper = MapperFactory.Crear();
        _servicioMock = new Mock<IEnvioServicio>();
        _sesionMock = new Mock<ISesionWeb>();

        _sesionMock
            .Setup(s => s.UsuarioId)
            .Returns(UsuarioId);
        _servicioMock
            .Setup(s => s.ListarAsync(It.IsAny<int>(), It.IsAny<EstadoEnvio?>()))
            .ReturnsAsync([]);
        _servicioMock
            .Setup(s => s.CrearAsync(It.IsAny<EnvioDTO>(), It.IsAny<int>()))
            .ReturnsAsync(ResultadoOperacionEnvio.Exitoso());

        var httpContext = new DefaultHttpContext();
        _controller = new EnvioController(_servicioMock.Object, _mapper, _sesionMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private void SinSesion()
        => _sesionMock.Setup(s => s.UsuarioId).Returns((int?)null);

    [Fact]
    public void Controller_TieneAtributoAuthorize()
    {
        Assert.True(Attribute.IsDefined(typeof(EnvioController), typeof(AuthorizeAttribute)));
    }

    #region Index

    [Fact]
    public async Task Index_CuandoNoHaySesion_DevuelveChallengeYNoConsultaAlServicio()
    {
        SinSesion();

        var resultado = await _controller.Index(null);

        Assert.IsType<ChallengeResult>(resultado);
        _servicioMock.Verify(
            s => s.ListarAsync(It.IsAny<int>(), It.IsAny<EstadoEnvio?>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_SiempreListaTodosLosEnviosDelUsuarioSinFiltrarEnElServicio()
    {
        await _controller.Index(EstadoEnvio.Cancelado);

        _servicioMock.Verify(s => s.ListarAsync(UsuarioId, null), Times.Once);
    }

    [Fact]
    public async Task Index_SinFiltro_DevuelveTodosLosEnvios()
    {
        _servicioMock
            .Setup(s => s.ListarAsync(UsuarioId, null))
            .ReturnsAsync(
            [
                EnvioTestData.CrearListadoDTO(1, EstadoEnvio.BuscandoConductor),
                EnvioTestData.CrearListadoDTO(2, EstadoEnvio.Cancelado)
            ]);

        var resultado = await _controller.Index(null);

        var modelo = ObtenerModelo<MisEnviosViewModel>(resultado);
        Assert.Null(modelo.Filtro);
        Assert.Equal([1, 2], modelo.Envios.Select(e => e.Id));
    }

    [Fact]
    public async Task Index_ConFiltro_DevuelveSoloLosEnviosEnEseEstado()
    {
        _servicioMock
            .Setup(s => s.ListarAsync(UsuarioId, null))
            .ReturnsAsync(
            [
                EnvioTestData.CrearListadoDTO(1, EstadoEnvio.BuscandoConductor),
                EnvioTestData.CrearListadoDTO(2, EstadoEnvio.Cancelado),
                EnvioTestData.CrearListadoDTO(3, EstadoEnvio.Cancelado)
            ]);

        var resultado = await _controller.Index(EstadoEnvio.Cancelado);

        var modelo = ObtenerModelo<MisEnviosViewModel>(resultado);
        Assert.Equal(EstadoEnvio.Cancelado, modelo.Filtro);
        Assert.Equal([2, 3], modelo.Envios.Select(e => e.Id));
    }

    [Fact]
    public async Task Index_LosContadoresSeCalculanSobreTodosLosEnviosAunqueHayaFiltro()
    {
        _servicioMock
            .Setup(s => s.ListarAsync(UsuarioId, null))
            .ReturnsAsync(
            [
                EnvioTestData.CrearListadoDTO(1, EstadoEnvio.BuscandoConductor),
                EnvioTestData.CrearListadoDTO(2, EstadoEnvio.BuscandoConductor),
                EnvioTestData.CrearListadoDTO(3, EstadoEnvio.ConductorAsignado),
                EnvioTestData.CrearListadoDTO(4, EstadoEnvio.Retirado),
                EnvioTestData.CrearListadoDTO(5, EstadoEnvio.EnCamino),
                EnvioTestData.CrearListadoDTO(6, EstadoEnvio.Entregado),
                EnvioTestData.CrearListadoDTO(7, EstadoEnvio.Cancelado)
            ]);

        var resultado = await _controller.Index(EstadoEnvio.Entregado);

        var modelo = ObtenerModelo<MisEnviosViewModel>(resultado);
        Assert.Equal(7, modelo.TotalTodos);
        Assert.Equal(2, modelo.TotalBuscando);
        Assert.Equal(1, modelo.TotalAsignados);
        Assert.Equal(2, modelo.TotalEnCamino);
        Assert.Equal(1, modelo.TotalEntregados);
        Assert.Equal(1, modelo.TotalCancelados);
        Assert.Single(modelo.Envios);
    }

    [Fact]
    public async Task Index_CuandoNoHayEnvios_DevuelveModeloVacioConContadoresEnCero()
    {
        var resultado = await _controller.Index(null);

        var modelo = ObtenerModelo<MisEnviosViewModel>(resultado);
        Assert.Empty(modelo.Envios);
        Assert.Equal(0, modelo.TotalTodos);
        Assert.Equal(0, modelo.TotalBuscando);
        Assert.Equal(0, modelo.TotalAsignados);
        Assert.Equal(0, modelo.TotalEnCamino);
        Assert.Equal(0, modelo.TotalEntregados);
        Assert.Equal(0, modelo.TotalCancelados);
    }

    #endregion

    #region Detalle

    [Fact]
    public async Task Detalle_CuandoNoHaySesion_DevuelveChallengeYNoConsultaAlServicio()
    {
        SinSesion();

        var resultado = await _controller.Detalle(10);

        Assert.IsType<ChallengeResult>(resultado);
        _servicioMock.Verify(
            s => s.ObtenerDetalleAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Detalle_CuandoElEnvioNoExiste_DevuelveNotFound()
    {
        _servicioMock
            .Setup(s => s.ObtenerDetalleAsync(99, UsuarioId))
            .ReturnsAsync((EnvioDetalleDTO?)null);

        var resultado = await _controller.Detalle(99);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Detalle_CuandoElEnvioExiste_DevuelveLaVistaConElModeloMapeado()
    {
        _servicioMock
            .Setup(s => s.ObtenerDetalleAsync(10, UsuarioId))
            .ReturnsAsync(new EnvioDetalleDTO
            {
                Id = 10,
                Origen = "Ramos Mejía",
                Destino = "Ciudadela",
                Estado = EstadoEnvio.ConductorAsignado,
                TieneConductorAsignado = true,
                ConductorNombre = "Carlos Gomez"
            });

        var resultado = await _controller.Detalle(10);

        var modelo = ObtenerModelo<EnvioDetalleViewModel>(resultado);
        Assert.Equal(10, modelo.Id);
        Assert.Equal("Ramos Mejía", modelo.Origen);
        Assert.Equal("Ciudadela", modelo.Destino);
        Assert.Equal(EstadoEnvio.ConductorAsignado, modelo.Estado);
        Assert.True(modelo.TieneConductorAsignado);
        Assert.Equal("Carlos Gomez", modelo.ConductorNombre);
    }

    [Fact]
    public async Task Detalle_ConsultaAlServicioConElIdYElUsuarioDeLaSesion()
    {
        await _controller.Detalle(10);

        _servicioMock.Verify(s => s.ObtenerDetalleAsync(10, UsuarioId), Times.Once);
    }

    #endregion

    #region Cancelar

    [Fact]
    public async Task Cancelar_CuandoNoHaySesion_DevuelveChallengeYNoCancela()
    {
        SinSesion();

        var resultado = await _controller.Cancelar(10);

        Assert.IsType<ChallengeResult>(resultado);
        _servicioMock.Verify(
            s => s.CancelarAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Cancelar_CuandoElEnvioNoExiste_DevuelveNotFound()
    {
        _servicioMock
            .Setup(s => s.CancelarAsync(99, UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.NoExiste());

        var resultado = await _controller.Cancelar(99);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Cancelar_CuandoElServicioDevuelveError_GuardaElErrorEnTempData()
    {
        _servicioMock
            .Setup(s => s.CancelarAsync(10, UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.Invalido("No se puede cancelar."));

        await _controller.Cancelar(10);

        Assert.Equal("No se puede cancelar.", _controller.TempData["Error"]);
        Assert.False(_controller.TempData.ContainsKey("Mensaje"));
    }

    [Fact]
    public async Task Cancelar_CuandoSeCancelaCorrectamente_GuardaElMensajeEnTempData()
    {
        _servicioMock
            .Setup(s => s.CancelarAsync(10, UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.Exitoso());

        await _controller.Cancelar(10);

        Assert.Equal("El envío #10 fue cancelado correctamente.", _controller.TempData["Mensaje"]);
        Assert.False(_controller.TempData.ContainsKey("Error"));
    }

    [Fact]
    public async Task Cancelar_SinVolverADetalle_RedirigeAlIndex()
    {
        _servicioMock
            .Setup(s => s.CancelarAsync(10, UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.Exitoso());

        var resultado = await _controller.Cancelar(10, volverADetalle: false);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnvioController.Index), redireccion.ActionName);
    }

    [Fact]
    public async Task Cancelar_VolviendoADetalle_RedirigeAlDetalleDelEnvio()
    {
        _servicioMock
            .Setup(s => s.CancelarAsync(10, UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.Exitoso());

        var resultado = await _controller.Cancelar(10, volverADetalle: true);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnvioController.Detalle), redireccion.ActionName);
        Assert.Equal(10, redireccion.RouteValues!["id"]);
    }

    [Fact]
    public async Task Cancelar_CuandoHayErrorYVolverADetalle_TambienRedirigeAlDetalle()
    {
        _servicioMock
            .Setup(s => s.CancelarAsync(10, UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.Invalido("Error"));

        var resultado = await _controller.Cancelar(10, volverADetalle: true);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnvioController.Detalle), redireccion.ActionName);
        Assert.Equal(10, redireccion.RouteValues!["id"]);
    }

    #endregion

    #region Nuevo (GET)

    [Fact]
    public void Nuevo_Get_DevuelveLaVistaConUnModeloVacio()
    {
        var resultado = _controller.Nuevo();

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.IsType<NuevoEnvioViewModel>(vista.Model);
    }

    #endregion

    #region Nuevo (POST)

    [Fact]
    public async Task Nuevo_Post_CuandoElModeloEsInvalido_DevuelveLaMismaVistaYNoLlamaAlServicio()
    {
        var modelo = EnvioTestData.CrearNuevoEnvioViewModel();
        _controller.ModelState.AddModelError("Monto", "El monto debe ser mayor que cero");

        var resultado = await _controller.Nuevo(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        _servicioMock.Verify(
            s => s.CrearAsync(It.IsAny<EnvioDTO>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Nuevo_Post_CuandoNoHaySesion_DevuelveChallengeYNoCrea()
    {
        SinSesion();

        var resultado = await _controller.Nuevo(EnvioTestData.CrearNuevoEnvioViewModel());

        Assert.IsType<ChallengeResult>(resultado);
        _servicioMock.Verify(
            s => s.CrearAsync(It.IsAny<EnvioDTO>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Nuevo_Post_CuandoElServicioDevuelveError_AgregaElErrorAlModelStateYDevuelveLaVista()
    {
        var modelo = EnvioTestData.CrearNuevoEnvioViewModel();
        _servicioMock
            .Setup(s => s.CrearAsync(It.IsAny<EnvioDTO>(), UsuarioId))
            .ReturnsAsync(ResultadoOperacionEnvio.Invalido("La fecha no puede ser anterior a hoy."));

        var resultado = await _controller.Nuevo(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        var errores = _controller.ModelState[string.Empty]!.Errors;
        Assert.Contains(errores, e => e.ErrorMessage == "La fecha no puede ser anterior a hoy.");
        Assert.False(_controller.TempData.ContainsKey("Mensaje"));
    }

    [Fact]
    public async Task Nuevo_Post_CuandoEsValido_LlamaAlServicioConElUsuarioYElDtoMapeado()
    {
        var modelo = EnvioTestData.CrearNuevoEnvioViewModel();
        EnvioDTO? dtoRecibido = null;
        _servicioMock
            .Setup(s => s.CrearAsync(It.IsAny<EnvioDTO>(), UsuarioId))
            .Callback<EnvioDTO, int>((dto, _) => dtoRecibido = dto)
            .ReturnsAsync(ResultadoOperacionEnvio.Exitoso());

        await _controller.Nuevo(modelo);

        Assert.NotNull(dtoRecibido);
        Assert.Equal("Ramos Mejía", dtoRecibido!.Origen.Texto);
        Assert.Equal("Ciudadela", dtoRecibido.Destino.Texto);
        Assert.Equal(new DateTime(2026, 3, 20), dtoRecibido.FechaHasta);
        Assert.Equal(1500m, dtoRecibido.Monto);
        Assert.Equal(2.5, dtoRecibido.Paquete.PesoKg);
        Assert.Equal(TamanioEnvio.Grande, dtoRecibido.Paquete.Tamanio);
        Assert.Equal("Zapatillas", dtoRecibido.Paquete.Descripcion);
        _servicioMock.Verify(s => s.CrearAsync(It.IsAny<EnvioDTO>(), UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Nuevo_Post_CuandoEsValido_GuardaElMensajeYRedirigeAlIndex()
    {
        var resultado = await _controller.Nuevo(EnvioTestData.CrearNuevoEnvioViewModel());

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnvioController.Index), redireccion.ActionName);
        Assert.Equal("Envío publicado correctamente", _controller.TempData["Mensaje"]);
    }

    #endregion

    private static T ObtenerModelo<T>(IActionResult resultado)
    {
        var vista = Assert.IsType<ViewResult>(resultado);
        return Assert.IsType<T>(vista.Model);
    }
}
