using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class OptimizadorServicioTests
{
    [Fact]
    public async Task CalculaTresAlternativasConUmbralInclusivoDelOchentaPorCiento()
    {
        var caso = new Escenario(cantidad: 1);
        caso.Agregar(1, 2, 10000, 8000);
        caso.Agregar(2, 2, 4000, 1000);
        caso.Agregar(3, 2, 8000, 3000);
        var resultado = await caso.Calcular(10);
        Assert.Equal(EstadoResultadoOptimizacion.Correcto, resultado.Estado);
        Assert.Equal(3, resultado.Propuestas.Count);
        Assert.Equal(new[] { 1 }, PorEtiqueta(resultado, "Mayor ganancia").EnviosNuevosIds);
        Assert.Equal(new[] { 2 }, PorEtiqueta(resultado, "Menor desvío").EnviosNuevosIds);
        Assert.Equal(new[] { 3 }, PorEtiqueta(resultado, "Equilibrada").EnviosNuevosIds);
        Assert.Equal(OptimizacionTestData.Fecha, resultado.FechaSalidaProgramada);
        Assert.Equal(20, resultado.ViajeId);
    }

    [Fact]
    public async Task MejorCombinacion_NoEsNecesariamenteElPaqueteDeMayorPago()
    {
        var caso = new Escenario(peso: 10, cantidad: 2);
        caso.Agregar(1, 10, 7000, 3000);
        caso.Agregar(2, 5, 4000, 2000);
        caso.Agregar(3, 5, 4000, 2000);
        caso.Proveedor.Desvios["2,3"] = 3000;
        var resultado = await caso.Calcular(5);
        Assert.Equal(new[] { 2, 3 }, PorEtiqueta(resultado, "Mayor ganancia").EnviosNuevosIds);
        Assert.Equal(8000m, PorEtiqueta(resultado, "Mayor ganancia").IngresoAdicional);
        caso.Viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
        caso.Envios.Verify(v => v.GuardarCambiosAsync(), Times.Never);
        Assert.Empty(caso.Viaje.Asignaciones);
        Assert.All(caso.Candidatos, e => Assert.Equal(EstadoEnvio.BuscandoConductor, e.Estado));
    }

    [Fact]
    public async Task DesvioCompartido_SeMideConjuntoYNoSeSumanLosIndividuales()
    {
        var caso = new Escenario(cantidad: 2);
        caso.Agregar(1, 2, 1000, 4000);
        caso.Agregar(2, 2, 1000, 4000);
        caso.Proveedor.Desvios["1,2"] = 5000;
        var resultado = await caso.Calcular(5);
        var propuesta = PorEtiqueta(resultado, "Mayor ganancia");
        Assert.Equal(new[] { 1, 2 }, propuesta.EnviosNuevosIds);
        Assert.Equal(5, propuesta.DesvioTotalKm);
    }

    [Fact]
    public async Task DesvioIndividualAlto_NoEliminaUnaCombinacionConjuntaFactible()
    {
        var caso = new Escenario(cantidad: 2);
        caso.Agregar(1, 2, 1000, 6000);
        caso.Agregar(2, 2, 1000, 7000);
        caso.Proveedor.Desvios["1,2"] = 5000;
        var resultado = await caso.Calcular(5);
        Assert.Equal(new[] { 1, 2 }, Assert.Single(resultado.Propuestas).EnviosNuevosIds);
    }

    [Theory]
    [InlineData(10000, 10000, 4000, 2000)]
    [InlineData(4000, 6000, 2000, 2000)]
    public async Task Desempates_PrefierenMenorDesvioOMayorIngreso(
        int pagoA, int pagoB, int desvioA, int desvioB)
    {
        var caso = new Escenario(cantidad: 1);
        caso.Agregar(1, 2, pagoA, desvioA);
        caso.Agregar(2, 2, pagoB, desvioB);
        var resultado = await caso.Calcular(5);
        Assert.Equal(new[] { 2 }, PorEtiqueta(resultado, "Mayor ganancia").EnviosNuevosIds);
        Assert.Equal(new[] { 2 }, PorEtiqueta(resultado, "Menor desvío").EnviosNuevosIds);
    }

    [Fact]
    public async Task AlternativasIguales_SeUnificanConSusTresEtiquetas()
    {
        var caso = new Escenario();
        caso.Agregar(1, 2, 1000, 0);
        var propuesta = Assert.Single((await caso.Calcular(0)).Propuestas);
        Assert.Equal(new[] { "Mayor ganancia", "Menor desvío", "Equilibrada" }, propuesta.Etiquetas);
        Assert.Equal(0, propuesta.DesvioTotalKm);
    }

    [Fact]
    public async Task Compromisos_SeIncluyenEnRecorridoTotalesYCapacidadPeroNoEnUmbralEquilibrado()
    {
        var caso = new Escenario(peso: 10, cantidad: 2);
        var comprometido = OptimizacionTestData.Envio(90, 2, 100000);
        OptimizacionTestData.Asignar(caso.Viaje, comprometido);
        caso.Proveedor.Envios.Add(comprometido);
        caso.Proveedor.Desvios["90"] = 1000;
        caso.Agregar(1, 8, 10000, 0);
        caso.Agregar(2, 8, 4000, 0);
        caso.Agregar(3, 8, 8000, 0);
        caso.Proveedor.Desvios["1,90"] = 8000;
        caso.Proveedor.Desvios["2,90"] = 2000;
        caso.Proveedor.Desvios["3,90"] = 3000;
        var resultado = await caso.Calcular(10);
        var equilibrada = PorEtiqueta(resultado, "Equilibrada");
        Assert.Equal(new[] { 3 }, equilibrada.EnviosNuevosIds);
        Assert.Equal(108000m, equilibrada.IngresoTotal);
        Assert.Equal(8000m, equilibrada.IngresoAdicional);
        Assert.Equal(10, equilibrada.PesoTotalKg);
        Assert.Equal(8, equilibrada.PesoAdicionalKg);
        Assert.Equal(2, equilibrada.CantidadTotalPaquetes);
        Assert.Equal(3, equilibrada.DesvioTotalKm);
        Assert.Equal(2, equilibrada.DesvioAdicionalKm, 3); // 13 km con todo - 11 km con el ya aceptado
        Assert.Equal(13, equilibrada.DistanciaTotalKm);
        Assert.Equal(15, equilibrada.DuracionTotalMinutos);
        Assert.All(caso.Proveedor.Consultas.Where(c => c.Length > 1), ids => Assert.Contains(90, ids));
        Assert.Single(caso.Proveedor.Consultas, c => c.Length == 0);
    }

    [Theory]
    [InlineData(0.8, 3)]
    [InlineData(0.9, 1)]
    public async Task PorcentajeConfigurado_ModificaAlternativaEquilibrada(double proporcion, int elegido)
    {
        var caso = new Escenario(cantidad: 1, proporcion: (decimal)proporcion);
        caso.Agregar(1, 2, 10000, 8000);
        caso.Agregar(3, 2, 8000, 3000);
        Assert.Equal(new[] { elegido }, PorEtiqueta(await caso.Calcular(10), "Equilibrada").EnviosNuevosIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void PorcentajeInvalido_SeRechaza(double proporcion)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Escenario(proporcion: (decimal)proporcion));

    [Fact]
    public async Task SolicitudInvalida_NoConsultaMapas()
    {
        var caso = new Escenario();
        var resultado = await caso.Servicio.CalcularAsync(new SolicitudOptimizacionDTO { RutaId = 0, DesvioMaximoKm = -1 }, 7);
        Assert.Equal(EstadoResultadoOptimizacion.SolicitudInvalida, resultado.Estado);
        Assert.NotEmpty(resultado.Mensajes);
        Assert.Empty(caso.Proveedor.Consultas);
    }

    [Fact]
    public async Task ViajeYFechaJuntos_SeRechazaSinConsultarMapas()
    {
        var caso = new Escenario();
        var resultado = await caso.Servicio.CalcularAsync( new SolicitudOptimizacionDTO
            {
                RutaId = 1,
                ViajeId = 20,
                FechaSalida = new DateTime(2026, 11, 9, 8, 0, 0),
                DesvioMaximoKm = 5
            }, 7);

        Assert.Equal(EstadoResultadoOptimizacion.SolicitudInvalida, resultado.Estado);
        Assert.NotEmpty(resultado.Mensajes);
        Assert.Empty(caso.Proveedor.Consultas);
    }

    [Fact]
    public async Task EnviosExcluidos_NoAparecenEnNingunaPropuesta()
    {
        static Escenario Armar()
        {
            var caso = new Escenario();
            caso.Agregar(1, 2, 10000, 0);
            caso.Agregar(2, 2, 4000, 0);
            caso.Proveedor.Desvios["1,2"] = 0;
            return caso;
        }

        var sinExcluir = await Armar().Calcular(5);
        Assert.Contains(sinExcluir.Propuestas, p => p.EnviosNuevosIds.Contains(1));

        var resultado = await Armar().Servicio.CalcularAsync(SolicitudExcluyendo(5, 1), 7);

        Assert.Equal(EstadoResultadoOptimizacion.Correcto, resultado.Estado);
        Assert.NotEmpty(resultado.Propuestas);
        Assert.All(resultado.Propuestas, p => Assert.DoesNotContain(1, p.EnviosNuevosIds));
    }

    [Fact]
    public async Task TodosLosCandidatosExcluidos_DevuelveCorrectoSinPropuestas()
    {
        var caso = new Escenario();
        caso.Agregar(1, 2, 10000, 0);

        var resultado = await caso.Servicio.CalcularAsync(SolicitudExcluyendo(5, 1), 7);

        Assert.Equal(EstadoResultadoOptimizacion.Correcto, resultado.Estado);
        Assert.Empty(resultado.Propuestas);
        Assert.Contains("No hay envíos candidatos para esta salida.", resultado.Mensajes);
    }

    [Fact]
    public async Task SalidaLlena_InformaLoComprometidoAunqueNoHayaPropuestas()
    {
        var caso = new Escenario(peso: 10, cantidad: 1);
        var comprometido = OptimizacionTestData.Envio(90, 2, 100000);
        OptimizacionTestData.Asignar(caso.Viaje, comprometido);
        caso.Proveedor.Envios.Add(comprometido);
        caso.Proveedor.Desvios["90"] = 0;
        caso.Agregar(1, 2, 5000, 0);

        var resultado = await caso.Calcular(5);

        Assert.Equal(EstadoResultadoOptimizacion.Correcto, resultado.Estado);
        Assert.Empty(resultado.Propuestas);
        Assert.Equal(100000m, resultado.IngresoComprometido);
    }

    [Fact]
    public async Task OtroConductor_NoObtienePropuestasNiConsultaMapas()
    {
        var caso = new Escenario();
        var resultado = await caso.Servicio.CalcularAsync(Solicitud(5), 999);
        Assert.Equal(EstadoResultadoOptimizacion.SalidaNoEncontrada, resultado.Estado);
        Assert.Empty(caso.Proveedor.Consultas);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Inactivo_NoOptimiza(bool inactivarRuta)
    {
        var caso = new Escenario();
        if (inactivarRuta) caso.Ruta.Estado = EstadoRuta.INACTIVA;
        else caso.Viaje.Estado = EstadoActividadViaje.INACTIVO;
        Assert.Equal(EstadoResultadoOptimizacion.SalidaNoHabilitada, (await caso.Calcular(5)).Estado);
        Assert.Empty(caso.Proveedor.Consultas);
    }

    [Fact]
    public async Task CompromisosExcedidos_NoIniciaBusqueda()
    {
        var caso = new Escenario(peso: 1);
        OptimizacionTestData.Asignar(caso.Viaje, OptimizacionTestData.Envio(90, 2));
        Assert.Equal(EstadoResultadoOptimizacion.SalidaNoHabilitada, (await caso.Calcular(5)).Estado);
        Assert.Empty(caso.Proveedor.Consultas);
    }

    [Theory]
    [InlineData(0)] // Sin candidatos
    [InlineData(1)] // Sin capacidad
    [InlineData(2)] // Desvío de compromisos excedido
    [InlineData(3)] // Todos los candidatos exceden desvío
    public async Task SinPropuesta_DevuelveResultadoCorrectoConExplicacion(int escenario)
    {
        var caso = new Escenario(peso: 2);
        if (escenario == 1 || escenario == 2)
        {
            var envio = OptimizacionTestData.Envio(90, escenario == 1 ? 2 : 1);
            OptimizacionTestData.Asignar(caso.Viaje, envio);
            caso.Proveedor.Envios.Add(envio);
            caso.Proveedor.Desvios["90"] = escenario == 2 ? 6000 : 0;
        }
        if (escenario == 3) caso.Agregar(1, 1, 1000, 6000);
        var resultado = await caso.Calcular(5);
        Assert.Equal(EstadoResultadoOptimizacion.Correcto, resultado.Estado);
        Assert.Empty(resultado.Propuestas);
        Assert.NotEmpty(resultado.Mensajes);
    }

    [Fact]
    public async Task SalidaSinMaterializar_CalculaSinCrearViaje()
    {
        var caso = new Escenario();
        caso.Agregar(1, 2, 1000, 0);
        var resultado = await caso.Servicio.CalcularAsync(
            new SolicitudOptimizacionDTO { RutaId = 1, DesvioMaximoKm = 5 }, 7);
        Assert.Null(resultado.ViajeId);
        Assert.Single(resultado.Propuestas);
        caso.Viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
        caso.Viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-11000)]
    public async Task DistanciaInvalidaEnCombinacion_NoProducePropuesta(double desvio)
    {
        var caso = new Escenario();
        caso.Agregar(1, 2, 1000, desvio);
        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.Calcular(5));
    }

    [Fact]
    public async Task FallaDeMapas_SePropagaEnLugarDeDevolverListaVacia()
    {
        var caso = new Escenario();
        caso.Proveedor.Error = new RutaNoEncontradaException("Proveedor no disponible");
        await Assert.ThrowsAsync<RutaNoEncontradaException>(() => caso.Calcular(5));
    }

    [Fact]
    public async Task Cancelacion_SePropagaSinConsultaDeRutas()
    {
        var caso = new Escenario();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            caso.Servicio.CalcularAsync(Solicitud(5), 7, new CancellationToken(true)));
        Assert.Empty(caso.Proveedor.Consultas);
    }

    // ---------- Busqueda en dos pasos (estimador + rutas reales) ----------

    [Fact]
    public async Task DosPasos_SoloLasFinalistasSeConsultanConRutasReales()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        for (int id = 1; id <= 8; id++) caso.Agregar(id, 1, 1000 * id, 500 * id);

        var resultado = await caso.Calcular(10);

        Assert.False(resultado.DistanciasEstimadas);
        Assert.Equal(9, caso.Estimador!.Consultas.Count); // recorrido habitual + las 8 combinaciones
        Assert.Equal(6, caso.Proveedor.Consultas.Count);  // recorrido habitual + 5 finalistas
        Assert.Equal(new[] { 8 }, PorEtiqueta(resultado, "Mayor ganancia").EnviosNuevosIds);
        Assert.Equal(new[] { 1 }, PorEtiqueta(resultado, "Menor desvío").EnviosNuevosIds);
    }

    [Fact]
    public async Task DosPasos_ElMargenNoPierdeUnaCombinacionQueConRutasRealesCumple()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 5000, 4800);
        caso.Estimador!.Desvios["1"] = 5500; // estimado 5,5 km: se pasa de 5, pero entra en el margen (6 km)

        var resultado = await caso.Calcular(5);

        Assert.False(resultado.DistanciasEstimadas);
        Assert.Equal(4.8, Assert.Single(resultado.Propuestas).DesvioTotalKm, 3);
    }

    [Fact]
    public async Task DosPasos_LaFinalistaQueSePasaConRutasRealesSeDescartaYQuedaLaSiguiente()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 10000, 7000); // real: 7 km
        caso.Agregar(2, 2, 8000, 2000);
        caso.Estimador!.Desvios["1"] = 1000; // estimado: 1 km, parecia la mejor

        var resultado = await caso.Calcular(5);

        Assert.False(resultado.DistanciasEstimadas);
        Assert.All(resultado.Propuestas, p => Assert.DoesNotContain(1, p.EnviosNuevosIds));
        Assert.Equal(new[] { 2 }, PorEtiqueta(resultado, "Mayor ganancia").EnviosNuevosIds);
    }

    [Fact]
    public async Task DosPasos_SiUnaConfirmacionVieneSinTrazado_TodaLaSalidaQuedaEstimada()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 5000, 3000);
        caso.Estimador!.Desvios["1"] = 1000;
        caso.Proveedor.ConTrazado = false; // el plan B respondio con el mock

        var resultado = await caso.Calcular(5);

        Assert.True(resultado.DistanciasEstimadas);
        Assert.Equal(1, Assert.Single(resultado.Propuestas).DesvioTotalKm, 3); // valor del paso 1
    }

    [Fact]
    public async Task DosPasos_SinConfirmar_ElLimiteExactoSeAplicaSobreLoEstimado()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 5000, 4800);
        caso.Estimador!.Desvios["1"] = 5500;
        caso.Proveedor.ConTrazado = false;

        var resultado = await caso.Calcular(5);

        Assert.True(resultado.DistanciasEstimadas);
        Assert.Empty(resultado.Propuestas);
    }

    [Fact]
    public async Task DosPasos_UnaFinalistaSinCaminoPosible_SeDescartaYQuedaLaSiguiente()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 10000, 1000);
        caso.Agregar(2, 2, 8000, 2000);
        caso.Proveedor.ErroresHttp["1"] = 404;

        var resultado = await caso.Calcular(5);

        Assert.False(resultado.DistanciasEstimadas);
        Assert.All(resultado.Propuestas, p => Assert.DoesNotContain(1, p.EnviosNuevosIds));
    }

    [Theory]
    [InlineData("", 500)]  // falla el recorrido habitual
    [InlineData("1", 500)] // falla una finalista
    [InlineData("1", 400)]
    public async Task DosPasos_SiOrsFallaPorOtroMotivo_UsaElPaso1Completo(string combinacion, int codigo)
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 10000, 3000);
        caso.Estimador!.Desvios["1"] = 1000;
        caso.Proveedor.ErroresHttp[combinacion] = codigo;

        var resultado = await caso.Calcular(5);

        Assert.True(resultado.DistanciasEstimadas);
        Assert.Equal(1, Assert.Single(resultado.Propuestas).DesvioTotalKm, 3); // valor del paso 1
    }

    [Fact]
    public async Task SoloEstimado_NoConsultaRutasReales()
    {
        var caso = new Escenario(cantidad: 1, conEstimador: true);
        caso.Agregar(1, 2, 5000, 1000);

        var resultado = await caso.Servicio.CalcularAsync(
            new SolicitudOptimizacionDTO { RutaId = 1, ViajeId = 20, DesvioMaximoKm = 5, SoloEstimado = true }, 7);

        Assert.True(resultado.DistanciasEstimadas);
        Assert.Single(resultado.Propuestas);
        Assert.Empty(caso.Proveedor.Consultas);
    }

    private static SolicitudOptimizacionDTO Solicitud(double desvio)
        => new() { RutaId = 1, ViajeId = 20, DesvioMaximoKm = desvio };

    private static SolicitudOptimizacionDTO SolicitudExcluyendo(double desvio, params int[] excluidos)
    => new() { RutaId = 1, ViajeId = 20, DesvioMaximoKm = desvio, EnviosExcluidos = excluidos };

    private static PropuestaOptimizacionDTO PorEtiqueta(ResultadoOptimizacionDTO resultado, string etiqueta)
        => Assert.Single(resultado.Propuestas, p => p.Etiquetas.Contains(etiqueta));

    // Se ejecutan los componentes reales; solo SQL y mapas se sustituyen.
    private sealed class Escenario
    {
        internal Ruta Ruta { get; }
        internal Viaje Viaje { get; }
        internal List<Envio> Candidatos { get; } = [];
        internal Mock<IViajeRepositorio> Viajes { get; } = new();
        internal Mock<IEnvioRepositorio> Envios { get; } = new();
        internal ProveedorControlado Proveedor { get; } = new();
        internal EstimadorControlado? Estimador { get; }
        internal OptimizadorServicio Servicio { get; }

        internal Escenario(double peso = 10, int cantidad = 3, decimal proporcion = 0.8m, bool conEstimador = false)
        {
            Estimador = conEstimador ? new EstimadorControlado() : null;
            Ruta = OptimizacionTestData.Ruta(peso, cantidad);
            Viaje = OptimizacionTestData.Viaje(Ruta);
            var rutas = new Mock<IRutaRepositorio>();
            rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(Ruta);
            Viajes.Setup(v => v.ObtenerDetalleAsync(20)).ReturnsAsync(Viaje);
            Envios.Setup(e => e.ListarDisponiblesAsync(OptimizacionTestData.Fecha, 7)).ReturnsAsync(Candidatos);
            var reloj = new RelojFijo(new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero));
            var constructor = new ConstructorRecorridoOptimizacion();
            Servicio = new OptimizadorServicio(Proveedor, constructor, new MotorDeCombinaciones(),
                new ResolvedorSalidaOptimizacion(Viajes.Object, reloj, rutas.Object),
                new ValidadorSolicitudOptimizacion(), new ValidadorSalidaOptimizacion(),
                new CalculadorCompromisosSalida(), new BuscadorCandidatosOptimizacion(Envios.Object),
                new EvaluadorDesvioCompromisos(Proveedor, constructor), proporcion, Estimador);
        }

        internal void Agregar(int id, double peso, decimal pago, double desvioMetros)
        {
            var envio = OptimizacionTestData.Envio(id, peso, pago);
            Candidatos.Add(envio);
            Proveedor.Envios.Add(envio);
            Proveedor.Desvios[id.ToString()] = desvioMetros;
            if (Estimador is not null)
            {
                Estimador.Envios.Add(envio);
                Estimador.Desvios[id.ToString()] = desvioMetros;
            }
        }

        internal Task<ResultadoOptimizacionDTO> Calcular(double desvio)
            => Servicio.CalcularAsync(Solicitud(desvio), 7);
    }

    private class ProveedorControlado : IServicioRutas
    {
        // true: responde con trazado, como ORS. El mock y el plan B responden sin trazado.
        internal bool ConTrazado { get; set; } = true;
        internal List<Envio> Envios { get; } = [];
        internal Dictionary<string, double> Desvios { get; } = new();
        internal List<int[]> Consultas { get; } = [];
        internal Exception? Error { get; set; }
        // error por combinacion ("" = recorrido habitual), con el codigo HTTP que responderia ORS
        internal Dictionary<string, int> ErroresHttp { get; } = new();

        public Task<ResultadoRuta> CalcularRutaAsync(Coordenada origen, Coordenada destino,
            IReadOnlyList<Coordenada>? paradas = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
            var puntos = paradas?.ToList() ?? [];
            int[] ids = Envios.Where(e => puntos.Contains(new Coordenada(e.DireccionRetiro.Latitud, e.DireccionRetiro.Longitud)))
                .Select(e => e.Id).OrderBy(id => id).ToArray();
            foreach (var envio in Envios.Where(e => ids.Contains(e.Id)))
            {
                int retiro = puntos.IndexOf(new(envio.DireccionRetiro.Latitud, envio.DireccionRetiro.Longitud));
                int entrega = puntos.IndexOf(new(envio.DireccionEntrega.Latitud, envio.DireccionEntrega.Longitud));
                Assert.True(entrega > retiro, "El recorrido enviado al proveedor debe retirar antes de entregar.");
            }
            Assert.Equal(new Coordenada(0, 0), origen);
            Assert.Equal(new Coordenada(0, 10), destino);
            Consultas.Add(ids);
            if (ErroresHttp.TryGetValue(string.Join(",", ids), out int codigo))
                throw new RutaNoEncontradaException($"ORS respondió {codigo}.") { CodigoHttp = codigo };
            double desvio = ids.Length == 0 ? 0 : Desvios[string.Join(",", ids)];
            return Task.FromResult(new ResultadoRuta(10000 + desvio, ids.Length == 0 ? 600 : 900, ConTrazado ? "{}" : null));
        }
    }

    // Hace de MockServicioRutas: mismas reglas, sin trazado.
    private sealed class EstimadorControlado : ProveedorControlado, IEstimadorDeRutas
    {
        internal EstimadorControlado() => ConTrazado = false;
    }

}
