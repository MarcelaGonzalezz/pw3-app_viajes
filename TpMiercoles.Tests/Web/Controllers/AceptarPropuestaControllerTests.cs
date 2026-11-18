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
    public async Task Parcial_InformaAceptadosYErrorEnDetalle()
    {
        Preparar(new ResultadoAceptacionPropuestaDTO { ViajeId = 50, EnviosAceptadosIds = [10],
            EnvioFallidoId = 20, Error = "Envío no disponible" });
        var resultado = Assert.IsType<RedirectToActionResult>(await _controller.AceptarPropuesta(_modelo));
        Assert.Equal("Detalle", resultado.ActionName);
        Assert.Equal("Viajes", resultado.ControllerName);
        Assert.Equal(50, resultado.RouteValues!["id"]);
        string mensaje = Assert.IsType<string>(_controller.TempData["Error"]);
        Assert.Contains("10", mensaje);
        Assert.Contains("Envío no disponible", mensaje);
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
        Assert.Equal(error ?? "No se pudo aceptar la propuesta.", _controller.TempData["Error"]);
        Assert.Null(_controller.TempData["Mensaje"]);
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
}
