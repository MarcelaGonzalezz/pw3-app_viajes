using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Controllers;

public class AceptarPropuestaControllerTests
{
    private readonly Mock<IOportunidadServicio> _servicio = new(MockBehavior.Strict);
    private readonly Mock<ISesionWeb> _sesion = new();
    private readonly OptimizadorController _controller;
    private readonly AceptarPropuestaViewModel _modelo = new() { RutaId = 1, ViajeId = 50,
        FechaSalidaProgramada = new DateTime(2026, 11, 2, 8, 0, 0), EnvioIds = [10, 20] };

    public AceptarPropuestaControllerTests()
    {
        _sesion.Setup(s => s.UsuarioId).Returns(7);
        _controller = new OptimizadorController(Mock.Of<IOptimizadorServicio>(), _sesion.Object, _servicio.Object);
        _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public void Aceptar_ExigePostYAntiforgery()
    {
        var accion = typeof(OptimizadorController).GetMethod(nameof(OptimizadorController.AceptarPropuesta))!;
        Assert.True(Attribute.IsDefined(accion, typeof(HttpPostAttribute)));
        Assert.True(Attribute.IsDefined(accion, typeof(ValidateAntiForgeryTokenAttribute)));
    }

    [Fact]
    public async Task SinSesion_NoInvocaServicio()
    {
        _sesion.Setup(s => s.UsuarioId).Returns((int?)null);
        Assert.IsType<ChallengeResult>(await _controller.AceptarPropuesta(_modelo));
        _servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ModeloInvalido_RedirigeAlOptimizadorSinAceptar()
    {
        _controller.ModelState.AddModelError("EnvioIds", "Requerido");
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.AceptarPropuesta(_modelo));
        Assert.Equal("Index", resultado.ActionName);
        Assert.NotNull(_controller.TempData["Error"]);
        _servicio.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(50)]
    public async Task Exito_UsaSesionYDatosDelFormularioYRedirigeAlViaje(int? viajeId)
    {
        _modelo.ViajeId = viajeId;
        Preparar(new ResultadoAceptacionPropuestaDTO { ViajeId = 50, EnviosAceptadosIds = [10, 20] });
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.AceptarPropuesta(_modelo));
        Assert.Equal("Detalle", resultado.ActionName);
        Assert.Equal("Viajes", resultado.ControllerName);
        Assert.Equal(50, resultado.RouteValues!["id"]);
        Assert.Contains("2 envíos", Assert.IsType<string>(_controller.TempData["Mensaje"]));
        _servicio.Verify(s => s.AceptarPropuestaAsync(1, viajeId, _modelo.FechaSalidaProgramada!.Value,
            It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 10, 20 })), 7), Times.Once);
        _servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Conflicto_VuelveAlOptimizadorConLaMismaBusquedaParaRecalcular() {
        _modelo.SalidaSeleccionada = "1:50";
        _modelo.DesvioMaximoKm = 5;
        Preparar(new ResultadoAceptacionPropuestaDTO { EnvioFallidoId = 20, Error = "Envío no disponible." });
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.AceptarPropuesta(_modelo));
        Assert.Equal("Index", resultado.ActionName);
        Assert.Equal("1:50", resultado.RouteValues!["salida"]);
        Assert.Equal(5.0, resultado.RouteValues!["desvio"]);
        string mensaje = Assert.IsType<string>(_controller.TempData["Error"]);
        Assert.Contains("Envío no disponible.", mensaje);
        Assert.Contains("recalcular", mensaje);
        Assert.Null(_controller.TempData["Mensaje"]);
    }

    [Theory]
    [InlineData("Salida inexistente")]
    [InlineData(null)]
    public async Task NingunoAceptado_VuelveAlOptimizadorConError(string? error)
    {
        Preparar(new ResultadoAceptacionPropuestaDTO { Error = error });
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.AceptarPropuesta(_modelo));
        Assert.Equal("Index", resultado.ActionName);
        Assert.StartsWith(error ?? "No se pudo aceptar la propuesta.", Assert.IsType<string>(_controller.TempData["Error"])); Assert.Null(_controller.TempData["Mensaje"]);
    }

    private void Preparar(ResultadoAceptacionPropuestaDTO resultado) =>
        _servicio.Setup(s => s.AceptarPropuestaAsync(1, _modelo.ViajeId, _modelo.FechaSalidaProgramada!.Value,
            It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 10, 20 })), 7)).ReturnsAsync(resultado);

    [Theory]
    [InlineData("ruta")]
    [InlineData("viaje")]
    [InlineData("fecha")]
    [InlineData("listaVacia")]
    [InlineData("listaNula")]
    public void Formulario_ValidaDatosRequeridos(string caso)
    {
        if (caso == "ruta") _modelo.RutaId = 0;
        if (caso == "viaje") _modelo.ViajeId = 0;
        if (caso == "fecha") _modelo.FechaSalidaProgramada = null;
        if (caso == "listaVacia") _modelo.EnvioIds = [];
        if (caso == "listaNula") _modelo.EnvioIds = null!;
        Assert.False(Validator.TryValidateObject(_modelo, new ValidationContext(_modelo), [], true));
    }

    [Fact]
    public void Formulario_PermiteSalidaSinViajeMaterializado()
    {
        _modelo.ViajeId = null;
        Assert.True(Validator.TryValidateObject(_modelo, new ValidationContext(_modelo), [], true));
    }

    #region Confirmar (Pantalla 11)

    private readonly ConfirmarPropuestaViewModel _confirmacion = new() {
        RutaId = 1,
        ViajeId = 50,
        FechaSalidaProgramada = new DateTime(2026, 11, 2, 8, 0, 0),
        EnvioIds = [10, 20],
        DesvioTotalKm = 3.5,
        CantidadTotalPaquetes = 4,
        SalidaSeleccionada = "1:50",
        DesvioMaximoKm = 5
    };

    [Fact]
    public void Confirmar_ExigePostYAntiforgery() {
        var accion = typeof(OptimizadorController).GetMethod(nameof(OptimizadorController.Confirmar))!;
        Assert.True(Attribute.IsDefined(accion, typeof(HttpPostAttribute)));
        Assert.True(Attribute.IsDefined(accion, typeof(ValidateAntiForgeryTokenAttribute)));
    }

    [Fact]
    public async Task Confirmar_SinSesion_NoInvocaServicio() {
        _sesion.Setup(s => s.UsuarioId).Returns((int?)null);
        Assert.IsType<ChallengeResult>(await _controller.Confirmar(_confirmacion));
        _servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Confirmar_ModeloInvalido_VuelveAlOptimizadorSinConsultar() {
        _controller.ModelState.AddModelError("EnvioIds", "Requerido");
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.Confirmar(_confirmacion));
        Assert.Equal("Index", resultado.ActionName);
        Assert.Equal("1:50", resultado.RouteValues!["salida"]);
        Assert.NotNull(_controller.TempData["Error"]);
        _servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Confirmar_SalidaInexistente_VuelveAlOptimizadorConError() {
        PrepararResumen(null);
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.Confirmar(_confirmacion));
        Assert.Equal("Index", resultado.ActionName);
        Assert.NotNull(_controller.TempData["Error"]);
    }

    [Fact]
    public async Task Confirmar_MuestraElResumenSinAceptarNada() {
        var resumen = new ResumenPropuestaDTO {
            RutaId = 1,
            ViajeId = 50,
            Envios = [new EnvioDePropuestaDTO { Id = 10, Disponible = true }, new EnvioDePropuestaDTO { Id = 20, Disponible = true }]
        };
        PrepararResumen(resumen);

        var vista = Assert.IsType<ViewResult>(await _controller.Confirmar(_confirmacion));

        var modelo = Assert.IsType<ConfirmarPropuestaViewModel>(vista.Model);
        Assert.Same(resumen, modelo.Resumen);
        Assert.Equal(3.5, modelo.DesvioTotalKm);
        _servicio.Verify(s => s.AceptarPropuestaAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime>(),
            It.IsAny<IReadOnlyList<int>>(), It.IsAny<int>()), Times.Never);
    }

    private void PrepararResumen(ResumenPropuestaDTO? resumen) =>
        _servicio.Setup(s => s.ObtenerResumenPropuestaAsync(1, 50, _confirmacion.FechaSalidaProgramada!.Value,
            It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 10, 20 })), 7)).ReturnsAsync(resumen);

    #endregion
}
