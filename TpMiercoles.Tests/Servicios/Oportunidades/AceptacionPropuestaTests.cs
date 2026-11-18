using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Oportunidades;

public class AceptacionPropuestaTests {
    private static readonly DateTime Fecha = new(2026, 11, 2, 8, 0, 0);
    private readonly Mock<IRutaRepositorio> _rutas = new();
    private readonly Mock<IViajeRepositorio> _viajes = new();
    private readonly Mock<IEnvioRepositorio> _envios = new();
    private readonly Mock<IServicioRutas> _mapas = new(MockBehavior.Strict);
    private readonly Ruta _ruta = RutaTestData.CrearRuta(dias: [DayOfWeek.Monday]);
    private readonly Dictionary<int, Envio> _datos = new();
    private Viaje? _viaje;
    private readonly OportunidadServicio _servicio;

    public AceptacionPropuestaTests() {
        _rutas.Setup(r => r.ObtenerPorIdParaModificarAsync(1)).ReturnsAsync(_ruta);
        _viajes.Setup(v => v.ObtenerParaAsignarPorRutaYFechaAsync(1, Fecha)).ReturnsAsync(() => _viaje);
        _viajes.Setup(v => v.ObtenerParaAsignarAsync(50)).ReturnsAsync(() => _viaje);
        // Resumen de la confirmación: lee sin modificar.
        _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(_ruta);
        _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, It.IsAny<DateTime>())).ReturnsAsync(() => _viaje);
        _viajes.Setup(v => v.ObtenerDetalleAsync(50)).ReturnsAsync(() => _viaje);
        _viajes.Setup(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>())).Callback<Viaje>(v => {
            _viaje = v;
            v.Id = 50;
            // Simula la navegación que EF resuelve desde RutaId.
            v.Ruta = _ruta;
        }).Returns(Task.CompletedTask);
        _viajes.Setup(v => v.GuardarCambiosAsync()).Returns(Task.CompletedTask);
        _envios.Setup(e => e.ObtenerParaOportunidadAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => _datos.GetValueOrDefault(id));
        _servicio = new(_rutas.Object, _viajes.Object, _envios.Object, _mapas.Object,
            new RelojFijo(new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero)));
        for (int id = 10; id <= 30; id += 10)
            _datos[id] = new Envio {
                Id = id,
                UsuarioId = 2,
                Estado = EstadoEnvio.BuscandoConductor,
                FechaDesde = Fecha.Date,
                FechaHasta = Fecha.Date,
                Monto = 100,
                Paquete = new Paquete { PesoKg = 1, Descripcion = $"Paquete {id}" },
                DireccionRetiro = new Direccion { Texto = $"Retiro {id}" },
                DireccionEntrega = new Direccion { Texto = $"Entrega {id}" }
            };
    }

    private Viaje Existente() => _viaje = new Viaje {
        Id = 50,
        RutaId = 1,
        Ruta = _ruta,
        FechaSalida = Fecha,
        FechaLlegada = Fecha.AddHours(1),
        Estado = EstadoActividadViaje.ACTIVO,
        PesoMax = 25,
        CapacidadMaxPaquetes = 4
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TodosDisponibles_AsignaAlMismoViajeYGuardaTodoJunto(bool existente) {
        if (existente) Existente();
        var resultado = await _servicio.AceptarPropuestaAsync(1, existente ? 50 : null, Fecha, [10, 20, 30], 1);
        Assert.True(resultado.EsExitoso);
        Assert.Equal(50, resultado.ViajeId);
        Assert.Equal(new[] { 10, 20, 30 }, resultado.EnviosAceptadosIds);
        Assert.Equal(new[] { 10, 20, 30 }, _viaje!.Asignaciones.Select(a => a.EnvioId));
        Assert.All(_viaje.Asignaciones, a => Assert.Equal(EstadoViaje.CONFIRMADO, a.Estado));
        Assert.All(_datos.Values, e => Assert.Equal(EstadoEnvio.ConductorAsignado, e.Estado));
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Once);// Un solo guardado: o se guardan todos o ninguno.
        _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Exactly(existente ? 0 : 1));
        _mapas.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SegundoNoDisponible_NoAceptaNingunoNiCreaElViaje(bool inexistente) {
        if (inexistente) _datos.Remove(20);
        else _datos[20].Estado = EstadoEnvio.ConductorAsignado;
        var resultado = await _servicio.AceptarPropuestaAsync(1, null, Fecha, [10, 20, 30], 1);
        Assert.False(resultado.EsExitoso);
        Assert.Empty(resultado.EnviosAceptadosIds);
        Assert.Equal(20, resultado.EnvioFallidoId);
        Assert.Null(resultado.ViajeId);
        Assert.NotEmpty(resultado.Error!);
        Assert.Null(_viaje);
        Assert.Equal(EstadoEnvio.BuscandoConductor, _datos[10].Estado);
        Assert.Equal(EstadoEnvio.BuscandoConductor, _datos[30].Estado);
        _envios.Verify(e => e.ObtenerParaOportunidadAsync(30), Times.Never);
        _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Fact]
    public async Task SegundoNoDisponibleEnViajeExistente_NoModificaElViaje() {
        var viaje = Existente();
        _datos[20].Estado = EstadoEnvio.ConductorAsignado;
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10, 20], 1);
        Assert.False(resultado.EsExitoso);
        Assert.Contains("No se aceptó ningún envío", resultado.Error);
        Assert.Empty(viaje.Asignaciones);
        Assert.Equal(EstadoEnvio.BuscandoConductor, _datos[10].Estado);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Fact]
    public async Task PrimeroCancelado_NoCreaViajeNiGuarda() {
        _datos[10].Estado = EstadoEnvio.Cancelado;
        var resultado = await _servicio.AceptarPropuestaAsync(1, null, Fecha, [10, 20], 1);
        Assert.False(resultado.EsExitoso);
        Assert.Empty(resultado.EnviosAceptadosIds);
        Assert.Null(resultado.ViajeId);
        Assert.Equal(10, resultado.EnvioFallidoId);
        Assert.Null(_viaje);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
        _envios.Verify(e => e.ObtenerParaOportunidadAsync(20), Times.Never);
    }

    [Theory]
    [InlineData(0, 1, null, false)]
    [InlineData(1, 0, null, false)]
    [InlineData(1, 1, 0, false)]
    [InlineData(1, 1, -1, false)]
    [InlineData(1, 1, null, true)]
    public async Task SalidaInvalida_NoConsultaRepositorios(int ruta, int conductor, int? viaje, bool fechaVacia) {
        var resultado = await _servicio.AceptarPropuestaAsync(ruta, viaje, fechaVacia ? default : Fecha, [10], conductor);
        Assert.False(resultado.EsExitoso);
        Assert.NotNull(resultado.Error);
        _rutas.VerifyNoOtherCalls(); _viajes.VerifyNoOtherCalls(); _envios.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new[] { 10, 10 })]
    [InlineData(new[] { 10, 0 })]
    [InlineData(new[] { -1 })]
    public async Task ListaInvalida_NoAceptaNinguno(int[] ids) {
        var resultado = await _servicio.AceptarPropuestaAsync(1, null, Fecha, ids, 1);
        Assert.NotNull(resultado.Error);
        Assert.Empty(resultado.EnviosAceptadosIds);
        _rutas.VerifyNoOtherCalls(); _viajes.VerifyNoOtherCalls(); _envios.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListaNula_RechazadaAntesDeAccederARepositorios() {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _servicio.AceptarPropuestaAsync(1, null, Fecha, null!, 1));
        _viajes.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CapacidadAcumulada_RechazaLaPropuestaCompleta(bool porPeso) {
        var viaje = Existente();
        if (porPeso) viaje.PesoMax = 1.5;
        else viaje.CapacidadMaxPaquetes = 1;
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10, 20], 1);
        Assert.False(resultado.EsExitoso);
        Assert.Equal(20, resultado.EnvioFallidoId);
        Assert.Empty(viaje.Asignaciones);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Fact]
    public async Task PesosFraccionarios_AceptaElLimiteExacto() {
        Existente().PesoMax = 0.3;
        _datos[10].Paquete.PesoKg = 0.1;
        _datos[20].Paquete.PesoKg = 0.2;
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10, 20], 1);
        Assert.True(resultado.EsExitoso);
        Assert.Equal(new[] { 10, 20 }, resultado.EnviosAceptadosIds);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Once);
    }

    [Fact]
    public async Task PesosFraccionarios_SiLaSumaSuperaElMaximo_NoAceptaNinguno() {
        Existente().PesoMax = 0.3;
        _datos[10].Paquete.PesoKg = 0.1;
        _datos[20].Paquete.PesoKg = 0.2;
        _datos[30].Paquete.PesoKg = 0.001;
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10, 20, 30], 1);
        Assert.Empty(resultado.EnviosAceptadosIds);
        Assert.Equal(30, resultado.EnvioFallidoId);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Theory]
    [InlineData(EstadoViaje.CANCELADO, true)]
    [InlineData(EstadoViaje.CONFIRMADO, false)]
    public async Task CompromisosPrevios_SeConsideranSalvoCancelados(EstadoViaje estado, bool esperado) {
        var viaje = Existente();
        viaje.CapacidadMaxPaquetes = 1;
        viaje.PesoMax = 1;
        viaje.Asignaciones.Add(new Asignacion { Envio = _datos[20], EnvioId = 20, Estado = estado });
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10], 1);
        Assert.Equal(esperado, resultado.EsExitoso);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Exactly(esperado ? 1 : 0));
    }

    [Theory]
    [InlineData("inexistente")]
    [InlineData("otroConductor")]
    [InlineData("otraRuta")]
    [InlineData("viajeInactivo")]
    [InlineData("rutaInactiva")]
    public async Task ViajeNoHabilitado_NoAsigna(string caso) {
        var viaje = Existente();
        if (caso == "inexistente") _viaje = null;
        if (caso == "otroConductor") _ruta.UsuarioId = 99;
        if (caso == "otraRuta") viaje.RutaId = 99;
        if (caso == "viajeInactivo") viaje.Estado = EstadoActividadViaje.INACTIVO;
        if (caso == "rutaInactiva") _ruta.Estado = EstadoRuta.INACTIVA;
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10], 1);
        Assert.False(resultado.EsExitoso);
        Assert.Empty(viaje.Asignaciones);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task FechaEnvio_UsaHoySiIniciadoOFechaProgramadaSiFuturo(bool iniciado, bool disponible) {
        var viaje = Existente();
        viaje.FechaSalida = iniciado ? Fecha.AddDays(-1) : Fecha.AddDays(1);
        var diaEvaluado = iniciado ? Fecha.Date : viaje.FechaSalida.Date;
        _datos[10].FechaDesde = disponible ? diaEvaluado : diaEvaluado.AddDays(-1);
        _datos[10].FechaHasta = _datos[10].FechaDesde;
        var resultado = await _servicio.AceptarPropuestaAsync(1, 50, Fecha, [10], 1);
        Assert.Equal(disponible, resultado.EsExitoso);
        Assert.Equal(iniciado ? Fecha.AddDays(-1) : Fecha.AddDays(1), viaje.FechaSalida);
        _rutas.VerifyNoOtherCalls();
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Exactly(disponible ? 1 : 0));
    }

    [Fact]
    public async Task SalidaPasadaSinViaje_NoSeMaterializa() {
        var resultado = await _servicio.AceptarPropuestaAsync(1, null, Fecha.AddDays(-7), [10], 1);
        Assert.False(resultado.EsExitoso);
        Assert.Null(_viaje);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    #region Resumen para la confirmación (Pantalla 11)

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Resumen_TodosDisponibles_SumaGananciaPesoYCantidadSinModificarNada(bool existente) {
        if (existente) Existente();
        var resumen = await _servicio.ObtenerResumenPropuestaAsync(1, existente ? 50 : null, Fecha, [10, 20, 30], 1);
        Assert.NotNull(resumen);
        Assert.True(resumen!.TodosDisponibles);
        Assert.Equal(3, resumen.CantidadPaquetes);
        Assert.Equal(300m, resumen.GananciaTotal);
        Assert.Equal(3, resumen.PesoTotalKg);
        Assert.Equal("Paquete 10", resumen.Envios[0].Descripcion);
        Assert.Equal("Retiro 10", resumen.Envios[0].Retiro);
        Assert.Equal("Entrega 10", resumen.Envios[0].Entrega);
        Assert.All(_datos.Values, e => Assert.Equal(EstadoEnvio.BuscandoConductor, e.Estado));
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
        _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
    }

    [Fact]
    public async Task Resumen_EnvioTomadoPorOtroConductor_LoMarcaComoNoDisponible() {
        _datos[20].Estado = EstadoEnvio.ConductorAsignado;
        var resumen = await _servicio.ObtenerResumenPropuestaAsync(1, null, Fecha, [10, 20, 30], 1);
        Assert.False(resumen!.TodosDisponibles);
        var tomado = Assert.Single(resumen.Envios, e => !e.Disponible);
        Assert.Equal(20, tomado.Id);
        Assert.Contains("otro conductor", tomado.Motivo);
        _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Fact]
    public async Task Resumen_EnvioInexistente_LoMarcaComoNoDisponible() {
        _datos.Remove(20);
        var resumen = await _servicio.ObtenerResumenPropuestaAsync(1, null, Fecha, [10, 20], 1);
        Assert.False(resumen!.TodosDisponibles);
        Assert.False(resumen.Envios.Single(e => e.Id == 20).Disponible);
    }

    [Fact]
    public async Task Resumen_CapacidadAcumulada_MarcaElQueNoEntra() {
        Existente().CapacidadMaxPaquetes = 1;
        var resumen = await _servicio.ObtenerResumenPropuestaAsync(1, 50, Fecha, [10, 20], 1);
        Assert.False(resumen!.TodosDisponibles);
        Assert.True(resumen.Envios.Single(e => e.Id == 10).Disponible);
        Assert.False(resumen.Envios.Single(e => e.Id == 20).Disponible);
    }

    [Fact]
    public async Task Resumen_SalidaDeOtroConductor_DevuelveNull() {
        _ruta.UsuarioId = 99;
        Assert.Null(await _servicio.ObtenerResumenPropuestaAsync(1, null, Fecha, [10], 1));
    }

    #endregion
}
