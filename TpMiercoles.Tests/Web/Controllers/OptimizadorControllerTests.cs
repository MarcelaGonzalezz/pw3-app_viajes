using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Controllers;

public class OptimizadorControllerTests
{
    private readonly Mock<IOptimizadorServicio> _servicio = new(MockBehavior.Strict);
    private readonly Mock<ISesionWeb> _sesion = new();
    private readonly OptimizadorController _controller;
    private readonly List<SalidaDeRutaDTO> _salidas =
    [
        new() { RutaId = 1, ViajeId = 20, Origen = "Origen", Destino = "Destino",
            FechaSalida = new DateTime(2026, 11, 2, 8, 0, 0) }
    ];

    public OptimizadorControllerTests()
    {
        _sesion.Setup(s => s.UsuarioId).Returns(7);
        _servicio.Setup(s => s.ListarSalidasAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(_salidas);
        _controller = new OptimizadorController(_servicio.Object, _sesion.Object, Mock.Of<TpMiercoles.Servicios.IOportunidadServicio>());
    }

    [Fact]
    public void Acciones_EstanProtegidasPorAutorizacionYAntiforgery()
    {
        Assert.True(Attribute.IsDefined(typeof(OptimizadorController), typeof(AuthorizeAttribute)));
        var post = typeof(OptimizadorController).GetMethod(nameof(OptimizadorController.Index),
            [typeof(OptimizadorViewModel), typeof(CancellationToken)])!;
        Assert.True(Attribute.IsDefined(post, typeof(HttpPostAttribute)));
        Assert.True(Attribute.IsDefined(post, typeof(ValidateAntiForgeryTokenAttribute)));
    }

    [Fact]
    public async Task Get_SinSesion_DevuelveChallenge()
    {
        _sesion.Setup(s => s.UsuarioId).Returns((int?)null);
        Assert.IsType<ChallengeResult>(await _controller.Index(CancellationToken.None));
        _servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_CargaSelectorSinCalcularNiPrefijarDesvio()
    {
        var modelo = Modelo(await _controller.Index(CancellationToken.None));
        Assert.Single(modelo.Salidas);
        Assert.Equal(20, modelo.Salidas[0].ViajeId);
        Assert.Null(modelo.DesvioMaximoKm);
        Assert.Null(modelo.Resultado);
        _servicio.Verify(s => s.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_SinSesion_DevuelveChallenge()
    {
        _sesion.Setup(s => s.UsuarioId).Returns((int?)null);
        Assert.IsType<ChallengeResult>(await _controller.Index(Formulario(), CancellationToken.None));
        _servicio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Post_ModelStateInvalido_RecargaOpcionesYEliminaResultadoPrevio()
    {
        var formulario = Formulario();
        formulario.Salidas = [];
        formulario.Resultado = new ResultadoOptimizacionDTO { Estado = EstadoResultadoOptimizacion.Correcto };
        _controller.ModelState.AddModelError("DesvioMaximoKm", "Campo requerido");
        var modelo = Modelo(await _controller.Index(formulario, CancellationToken.None));
        Assert.Single(modelo.Salidas);
        Assert.Null(modelo.Resultado);
        Assert.Same(formulario, modelo);
        _servicio.Verify(s => s.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1:2:3")]
    [InlineData("0:20")]
    [InlineData("-1:20")]
    [InlineData("1:0")]
    [InlineData("1:-20")]
    [InlineData("1:abc")]
    [InlineData("999999999999:")]
    public async Task Post_SeleccionManipulada_MuestraErrorSinCalcular(string seleccion)
    {
        var formulario = Formulario();
        formulario.SalidaSeleccionada = seleccion;
        Modelo(await _controller.Index(formulario, CancellationToken.None));
        Assert.NotEmpty(_controller.ModelState[nameof(formulario.SalidaSeleccionada)]!.Errors);
        _servicio.Verify(s => s.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("1:20", 20)]
    [InlineData("1:", null)]
    public async Task Post_InterpretaSeleccionYUsaIdentidadDeSesion(string seleccion, int? viajeId)
    {
        using var cts = new CancellationTokenSource();
        var esperado = new ResultadoOptimizacionDTO
        {
            Estado = EstadoResultadoOptimizacion.Correcto, Mensajes = ["No hay candidatos."]
        };
        _servicio.Setup(s => s.CalcularAsync(
                It.Is<SolicitudOptimizacionDTO>(d => d.RutaId == 1 && d.ViajeId == viajeId && d.DesvioMaximoKm == 0),
                7, cts.Token))
            .ReturnsAsync(esperado);
        var formulario = Formulario();
        formulario.SalidaSeleccionada = seleccion;
        formulario.DesvioMaximoKm = 0;
        var modelo = Modelo(await _controller.Index(formulario, cts.Token));
        Assert.Same(esperado, modelo.Resultado);
        Assert.True(_controller.ModelState.IsValid);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public async Task Post_DesvioNoFinitoONegativo_NoLlegaAlServicio(double desvio)
    {
        var formulario = Formulario();
        formulario.DesvioMaximoKm = desvio;
        Modelo(await _controller.Index(formulario, CancellationToken.None));
        Assert.NotEmpty(_controller.ModelState[nameof(formulario.DesvioMaximoKm)]!.Errors);
    }

    [Theory]
    [InlineData(EstadoResultadoOptimizacion.SolicitudInvalida)]
    [InlineData(EstadoResultadoOptimizacion.SalidaNoEncontrada)]
    [InlineData(EstadoResultadoOptimizacion.SalidaNoHabilitada)]
    public async Task Post_ErrorDeNegocio_VuelveAlFormularioConMensajes(EstadoResultadoOptimizacion estado)
    {
        _servicio.Setup(s => s.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoOptimizacionDTO { Estado = estado, Mensajes = ["Salida no disponible"] });
        var modelo = Modelo(await _controller.Index(Formulario(), CancellationToken.None));
        Assert.Null(modelo.Resultado);
        Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "Salida no disponible");
        Assert.Single(modelo.Salidas);
    }

    [Fact]
    public async Task Post_ErrorDelProveedor_NoSePresentaComoBusquedaSinResultados()
    {
        _servicio.Setup(s => s.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(), 7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RutaNoEncontradaException("Detalle privado del proveedor"));
        var modelo = Modelo(await _controller.Index(Formulario(), CancellationToken.None));
        Assert.Null(modelo.Resultado);
        Assert.False(_controller.ModelState.IsValid);
        Assert.DoesNotContain(_controller.ModelState.Values.SelectMany(v => v.Errors),
            e => e.ErrorMessage.Contains("Detalle privado"));
    }

    [Fact]
    public async Task Post_Cancelacion_SePropaga()
    {
        _servicio.Setup(s => s.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(), 7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        await Assert.ThrowsAsync<OperationCanceledException>(() => _controller.Index(Formulario(), CancellationToken.None));
    }

    [Fact]
    public void FormularioVacio_ExigeSalidaYDesvioPeroNoResultados()
    {
        var formulario = new OptimizadorViewModel();
        var errores = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(formulario, new ValidationContext(formulario), errores, true));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(formulario.SalidaSeleccionada)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(formulario.DesvioMaximoKm)));
    }

    [Fact]
    public void Formulario_AdmiteDesvioCero()
    {
        var formulario = Formulario();
        formulario.DesvioMaximoKm = 0;
        Assert.True(Validator.TryValidateObject(formulario, new ValidationContext(formulario), [], true));
    }

    private static OptimizadorViewModel Formulario() => new() { SalidaSeleccionada = "1:20", DesvioMaximoKm = 5 };
    private static OptimizadorViewModel Modelo(IActionResult resultado)
        => Assert.IsType<OptimizadorViewModel>(Assert.IsType<ViewResult>(resultado).Model);
}
