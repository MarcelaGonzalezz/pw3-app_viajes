using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios.Optimizacion
{
    public interface IOptimizadorServicio
    {
        Task<ResultadoOptimizacionDTO> CalcularAsync(
            SolicitudOptimizacionDTO solicitud,
            int conductorId,
            CancellationToken ct = default);
        Task<List<SalidaDeRutaDTO>> ListarSalidasAsync(
    int conductorId,
    CancellationToken ct = default);
    }
    public sealed class OptimizadorServicio : IOptimizadorServicio
    {
        private readonly IServicioRutas _servicioRutas;
        private readonly ConstructorRecorridoOptimizacion _constructorRecorrido;
        private readonly MotorDeCombinaciones _motor;
        private readonly IResolvedorSalidaOptimizacion _resolvedor;
        private readonly ValidadorSolicitudOptimizacion _validadorSolicitud;
        private readonly ValidadorSalidaOptimizacion _validadorSalida;
        private readonly CalculadorCompromisosSalida _calculadorCompromisos;
        private readonly IBuscadorCandidatosOptimizacion _buscadorCandidatos;
        private readonly EvaluadorDesvioCompromisos _evaluadorDesvio;
        private readonly decimal _proporcionMinimaIngreso;

        public OptimizadorServicio(
            IServicioRutas servicioRutas,
            ConstructorRecorridoOptimizacion constructorRecorrido,
            MotorDeCombinaciones motor,
            IResolvedorSalidaOptimizacion resolvedor,
            ValidadorSolicitudOptimizacion validadorSolicitud,
            ValidadorSalidaOptimizacion validadorSalida,
            CalculadorCompromisosSalida calculadorCompromisos,
            IBuscadorCandidatosOptimizacion buscadorCandidatos,
            EvaluadorDesvioCompromisos evaluadorDesvio,
            decimal proporcionMinimaIngreso = 0.80m)
        {
            if (proporcionMinimaIngreso <= 0m
                || proporcionMinimaIngreso > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(proporcionMinimaIngreso));
            }

            _servicioRutas = servicioRutas;
            _constructorRecorrido = constructorRecorrido;
            _motor = motor;
            _resolvedor = resolvedor;
            _validadorSolicitud = validadorSolicitud;
            _validadorSalida = validadorSalida;
            _calculadorCompromisos = calculadorCompromisos;
            _buscadorCandidatos = buscadorCandidatos;
            _evaluadorDesvio = evaluadorDesvio;
            _proporcionMinimaIngreso = proporcionMinimaIngreso;
        }
        private static List<(int EnvioId, double PesoKg, decimal Pago)> PrepararCandidatos(
                IReadOnlyList<Envio> candidatos)
        {
            ArgumentNullException.ThrowIfNull(candidatos);

            return candidatos
                .Select(envio => (
                    EnvioId: envio.Id,
                    PesoKg: envio.Paquete.PesoKg,
                    Pago: envio.Monto))
                .OrderByDescending(candidato => candidato.Pago)
                .ThenBy(candidato => candidato.EnvioId)
                .ToList();
        }

        private async Task<ResultadoRuta> CalcularRutaCombinacionAsync(
    IReadOnlyList<Coordenada> recorridoBase,
    CompromisosSalida compromisos,
    IReadOnlyDictionary<int, Envio> candidatosPorId,
    IReadOnlyList<int> idsSeleccionados,
    CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var enviosDelRecorrido = new List<Envio>(compromisos.Envios);

            var idsIncluidos = compromisos.Envios
                .Select(envio => envio.Id)
                .ToHashSet();

            foreach (int envioId in idsSeleccionados)
            {
                if (!candidatosPorId.TryGetValue(envioId, out var envio))
                {
                    throw new InvalidOperationException(
                        $"El envío {envioId} no pertenece a los candidatos.");
                }

                if (!idsIncluidos.Add(envioId))
                {
                    throw new InvalidOperationException(
                        $"El envío {envioId} está repetido en el recorrido.");
                }

                enviosDelRecorrido.Add(envio);
            }

            var recorrido = _constructorRecorrido.ConstruirConEnvios(
                recorridoBase,
                enviosDelRecorrido,
                ct);

            var paradas = recorrido
                .Skip(1)
                .Take(recorrido.Count - 2)
                .ToList();

            return await _servicioRutas.CalcularRutaAsync(
                recorrido[0],
                recorrido[^1],
                paradas,
                ct);
        }

        private async Task<List<CombinacionEvaluada>> EvaluarCombinacionesAsync(
    IReadOnlyList<Coordenada> recorridoBase,
    ResultadoRuta rutaBase,
    CompromisosSalida compromisos,
    IReadOnlyList<Envio> candidatos,
    double desvioMaximoKm,
    CancellationToken ct)
        {
            if (!double.IsFinite(desvioMaximoKm) || desvioMaximoKm < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desvioMaximoKm));
            }

            if (!double.IsFinite(rutaBase.DistanciaMetros)
                || rutaBase.DistanciaMetros < 0)
            {
                throw new InvalidOperationException(
                    "La distancia del recorrido base no es válida.");
            }

            var datosCandidatos = PrepararCandidatos(candidatos);

            var candidatosPorId = candidatos.ToDictionary(
                envio => envio.Id);

            var combinaciones = _motor.GenerarCombinaciones(
                datosCandidatos,
                compromisos.PesoDisponibleKg,
                compromisos.CantidadDisponiblePaquetes,
                ct);

            var resultados = new List<CombinacionEvaluada>();

            foreach (var idsSeleccionados in combinaciones)
            {
                ct.ThrowIfCancellationRequested();

                ResultadoRuta recorrido =
                    await CalcularRutaCombinacionAsync(
                        recorridoBase,
                        compromisos,
                        candidatosPorId,
                        idsSeleccionados,
                        ct);

                if (!double.IsFinite(recorrido.DistanciaMetros)
                    || recorrido.DistanciaMetros < 0)
                {
                    throw new InvalidOperationException(
                        "El servicio de rutas devolvió una distancia inválida.");
                }

                double desvioKm = Math.Max(
                    0,
                    recorrido.DistanciaKm - rutaBase.DistanciaKm);

                if (desvioKm > desvioMaximoKm)
                {
                    continue;
                }

                decimal ingresoAdicional = idsSeleccionados.Sum(
                    envioId => candidatosPorId[envioId].Monto);

                double pesoAdicionalKg = idsSeleccionados.Sum(
                    envioId => candidatosPorId[envioId].Paquete.PesoKg);

                resultados.Add(new CombinacionEvaluada
                {
                    EnviosSeleccionados = idsSeleccionados,
                    IngresoAdicional = ingresoAdicional,
                    PesoAdicionalKg = pesoAdicionalKg,
                    DesvioKm = desvioKm,
                    Recorrido = recorrido
                });
            }

            return resultados;
        }

        private static (
    CombinacionEvaluada? MayorGanancia,
    CombinacionEvaluada? MenorDesvio,
    CombinacionEvaluada? Equilibrada)
    SeleccionarAlternativas(
        IReadOnlyList<CombinacionEvaluada> combinaciones,
        decimal proporcionMinimaIngreso)
        {
            ArgumentNullException.ThrowIfNull(combinaciones);

            if (proporcionMinimaIngreso <= 0m
                || proporcionMinimaIngreso > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(proporcionMinimaIngreso),
                    "La proporción debe ser mayor a cero y menor o igual a uno.");
            }

            if (combinaciones.Count == 0)
            {
                return (null, null, null);
            }

            CombinacionEvaluada mayorGanancia = combinaciones
                .OrderByDescending(combinacion => combinacion.IngresoAdicional)
                .ThenBy(combinacion => combinacion.DesvioKm)
                .First();

            CombinacionEvaluada menorDesvio = combinaciones
                .OrderBy(combinacion => combinacion.DesvioKm)
                .ThenByDescending(combinacion => combinacion.IngresoAdicional)
                .First();

            decimal ingresoMinimoEquilibrado =
                mayorGanancia.IngresoAdicional * proporcionMinimaIngreso;

            CombinacionEvaluada equilibrada = combinaciones
                .Where(combinacion =>
                    combinacion.IngresoAdicional >= ingresoMinimoEquilibrado)
                .OrderBy(combinacion => combinacion.DesvioKm)
                .ThenByDescending(combinacion => combinacion.IngresoAdicional)
                .First();

            return (mayorGanancia, menorDesvio, equilibrada);
        }

        private static List<PropuestaOptimizacionDTO> ArmarPropuestas(
    IReadOnlyList<CombinacionEvaluada> combinaciones,
    CompromisosSalida compromisos,
    decimal proporcionMinimaIngreso)
        {
            ArgumentNullException.ThrowIfNull(compromisos);

            var alternativas = SeleccionarAlternativas(
                combinaciones,
                proporcionMinimaIngreso);

            var opciones = new (
                string Etiqueta,
                CombinacionEvaluada? Combinacion)[]
            {
        ("Mayor ganancia", alternativas.MayorGanancia),
        ("Menor desvío", alternativas.MenorDesvio),
        ("Equilibrada", alternativas.Equilibrada)
            };

            var propuestas = new List<PropuestaOptimizacionDTO>();

            var propuestasPorCombinacion =
                new Dictionary<string, PropuestaOptimizacionDTO>();

            foreach (var opcion in opciones)
            {
                var combinacion = opcion.Combinacion;

                if (combinacion is null)
                {
                    continue;
                }

                int[] idsOrdenados = combinacion.EnviosSeleccionados
                    .OrderBy(id => id)
                    .ToArray();

                string clave = string.Join(",", idsOrdenados);

                if (propuestasPorCombinacion.TryGetValue(
                    clave,
                    out var existente))
                {
                    existente.Etiquetas.Add(opcion.Etiqueta);
                    continue;
                }

                var propuesta = new PropuestaOptimizacionDTO
                {
                    Etiquetas = [opcion.Etiqueta],

                    EnviosNuevosIds = Array.AsReadOnly(idsOrdenados),

                    IngresoAdicional = combinacion.IngresoAdicional,

                    IngresoTotal =
                        compromisos.IngresoComprometido
                        + combinacion.IngresoAdicional,

                    PesoAdicionalKg = combinacion.PesoAdicionalKg,

                    PesoTotalKg =
                        compromisos.PesoComprometidoKg
                        + combinacion.PesoAdicionalKg,

                    CantidadTotalPaquetes =
                        compromisos.CantidadComprometidaPaquetes
                        + idsOrdenados.Length,

                    DesvioTotalKm = combinacion.DesvioKm,

                    DistanciaTotalKm =
                        combinacion.Recorrido.DistanciaKm,

                    DuracionTotalMinutos =
                        combinacion.Recorrido.DuracionMinutos
                };

                propuestasPorCombinacion.Add(clave, propuesta);
                propuestas.Add(propuesta);
            }

            return propuestas;
        }

        public async Task<ResultadoOptimizacionDTO> CalcularAsync(
    SolicitudOptimizacionDTO solicitud,
    int conductorId,
    CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(solicitud);

            if (conductorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conductorId));
            }

            ct.ThrowIfCancellationRequested();

            var erroresSolicitud = _validadorSolicitud.Validar(solicitud);

            if (erroresSolicitud.Count > 0)
            {
                return new ResultadoOptimizacionDTO
                {
                    Estado = EstadoResultadoOptimizacion.SolicitudInvalida,
                    Mensajes = erroresSolicitud
                };
            }

            SalidaOptimizacion? salida;

            if (solicitud.ViajeId is int viajeId)
            {
                salida = await _resolvedor.ResolverViajeAsync(
                    solicitud.RutaId,
                    viajeId,
                    conductorId);
            }
            else
            {
                salida = await _resolvedor.ResolverProximaSalidaAsync(
                    solicitud.RutaId,
                    conductorId);
            }

            ct.ThrowIfCancellationRequested();

            if (salida is null)
            {
                return new ResultadoOptimizacionDTO
                {
                    Estado = EstadoResultadoOptimizacion.SalidaNoEncontrada,
                    Mensajes =
                    [
                        "No se encontró una salida válida para la selección."
                    ]
                };
            }

            var erroresSalida = _validadorSalida.Validar(salida);

            if (erroresSalida.Count > 0)
            {
                return new ResultadoOptimizacionDTO
                {
                    Estado = EstadoResultadoOptimizacion.SalidaNoHabilitada,
                    Mensajes = erroresSalida
                };
            }

            var compromisos = _calculadorCompromisos.Calcular(salida);

            var erroresCapacidad = _validadorSalida.ValidarCapacidad(
                salida,
                compromisos);

            if (erroresCapacidad.Count > 0)
            {
                return new ResultadoOptimizacionDTO
                {
                    Estado = EstadoResultadoOptimizacion.SalidaNoHabilitada,
                    Mensajes = erroresCapacidad
                };
            }

            var evaluacionDesvio = await _evaluadorDesvio.EvaluarAsync(
                salida,
                compromisos,
                solicitud.DesvioMaximoKm,
                ct);

            var mensajes = new List<string>();
            var propuestas = new List<PropuestaOptimizacionDTO>();

            if (evaluacionDesvio.SuperaLimite)
            {
                mensajes.Add(
                    "El recorrido calculado para los envíos ya comprometidos "
                    + "supera el desvío máximo ingresado.");
            }
            else if (compromisos.PesoDisponibleKg == 0
                || compromisos.CantidadDisponiblePaquetes == 0)
            {
                mensajes.Add(
                    "La salida no tiene capacidad disponible para nuevos envíos.");
            }
            else
            {
                var candidatos = await _buscadorCandidatos.BuscarAsync(
                    salida,
                    compromisos);

                ct.ThrowIfCancellationRequested();

                if (candidatos.Count == 0)
                {
                    mensajes.Add(
                        "No hay envíos candidatos para esta salida.");
                }
                else
                {
                    var recorridoBase =
                        _constructorRecorrido.ConstruirBase(salida);

                    var combinaciones = await EvaluarCombinacionesAsync(
                        recorridoBase,
                        evaluacionDesvio.RutaBase,
                        compromisos,
                        candidatos,
                        solicitud.DesvioMaximoKm,
                        ct);

                    propuestas = ArmarPropuestas(
                        combinaciones,
                        compromisos,
                        _proporcionMinimaIngreso);

                    if (propuestas.Count == 0)
                    {
                        mensajes.Add(
                            "Ninguna combinación evaluada cumple "
                            + "todas las restricciones.");
                    }
                }
            }

            return new ResultadoOptimizacionDTO
            {
                Estado = EstadoResultadoOptimizacion.Correcto,
                Mensajes = mensajes.AsReadOnly(),
                Propuestas = propuestas.AsReadOnly(),
                RutaId = salida.RutaId,
                ViajeId = salida.ViajeId,
                FechaSalidaProgramada = salida.FechaSalidaProgramada,
                MomentoEvaluacion = salida.MomentoEvaluacion
            };


        }
        public Task<List<SalidaDeRutaDTO>> ListarSalidasAsync(
    int conductorId,
    CancellationToken ct = default)
        {
            return _resolvedor.ListarSalidasAsync(conductorId, ct);
        }

        private sealed class CombinacionEvaluada
        {
            public required IReadOnlyList<int> EnviosSeleccionados { get; init; }

            public required decimal IngresoAdicional { get; init; }

            public required double PesoAdicionalKg { get; init; }

            public required double DesvioKm { get; init; }

            public required ResultadoRuta Recorrido { get; init; }
        }
    }
}
