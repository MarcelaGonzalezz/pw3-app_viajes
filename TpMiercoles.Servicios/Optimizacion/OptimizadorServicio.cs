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
            SolicitudOptimizacionDTO solicitud, int conductorId, CancellationToken ct = default);
        Task<List<SalidaDeRutaDTO>> ListarSalidasAsync( int conductorId, CancellationToken ct = default);
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
        private readonly IEstimadorDeRutas? _estimador;

        // Busqueda en dos pasos para no gastar la cuota de OpenRouteService:
        //  1) todas las combinaciones con el estimador (gratis), filtrando con un margen sobre el
        //     desvio maximo, porque lo estimado puede dar un poco mas que lo real;
        //  2) solo las mejores se confirman con rutas reales y ahi se aplica el limite exacto.
        // Si alguna confirmacion vuelve sin trazado (el plan B ya respondio con el mock), se usa
        // el paso 1 completo y el resultado se marca como estimado: nunca se mezclan fuentes.
        private const double MargenDelPaso1 = 1.2;
        private const int CantidadDeFinalistas = 5;
        private const string MensajeCompromisosSuperanDesvio =
            "El recorrido calculado para los envíos ya comprometidos supera el desvío máximo ingresado.";

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
            decimal proporcionMinimaIngreso = 0.80m,
            IEstimadorDeRutas? estimador = null)
        {
            if (proporcionMinimaIngreso <= 0m || proporcionMinimaIngreso > 1m)
            {
                throw new ArgumentOutOfRangeException( nameof(proporcionMinimaIngreso));
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
            _estimador = estimador;
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

        private static async Task<ResultadoRuta> CalcularRutaCombinacionAsync(
                IServicioRutas servicioRutas,
                ConstructorRecorridoOptimizacion constructorRecorrido,
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

            var recorrido = constructorRecorrido.ConstruirConEnvios( recorridoBase, enviosDelRecorrido, ct);

            var paradas = recorrido
                .Skip(1)
                .Take(recorrido.Count - 2)
                .ToList();

            return await servicioRutas.CalcularRutaAsync(
                recorrido[0],
                recorrido[^1],
                paradas,
                ct);
        }

        // Calcula el recorrido de una combinacion y su desvio respecto del recorrido base.
        private async Task<CombinacionEvaluada> EvaluarCombinacionAsync(
                IServicioRutas servicioRutas,
                IReadOnlyList<Coordenada> recorridoBase,
                ResultadoRuta rutaBase,
                CompromisosSalida compromisos,
                IReadOnlyDictionary<int, Envio> candidatosPorId,
                IReadOnlyList<int> idsSeleccionados,
                CancellationToken ct)
        {
            ResultadoRuta recorrido =
                await CalcularRutaCombinacionAsync(
                    servicioRutas,
                    _constructorRecorrido,
                    recorridoBase,
                    compromisos,
                    candidatosPorId,
                    idsSeleccionados,
                    ct);

            if (!double.IsFinite(recorrido.DistanciaMetros) || recorrido.DistanciaMetros < 0)
            {
                throw new InvalidOperationException(
                    "El servicio de rutas devolvió una distancia inválida.");
            }

            return new CombinacionEvaluada
            {
                EnviosSeleccionados = idsSeleccionados,
                IngresoAdicional = idsSeleccionados.Sum(envioId => candidatosPorId[envioId].Monto),
                PesoAdicionalKg = idsSeleccionados.Sum(envioId => candidatosPorId[envioId].Paquete.PesoKg),
                DesvioKm = Math.Max(0, recorrido.DistanciaKm - rutaBase.DistanciaKm),
                Recorrido = recorrido
            };
        }

        private async Task<List<CombinacionEvaluada>> EvaluarCombinacionesAsync(
                IServicioRutas servicioRutas,
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

            var candidatosPorId = candidatos.ToDictionary( envio => envio.Id);

            var combinaciones = _motor.GenerarCombinaciones(
                datosCandidatos,
                compromisos.PesoDisponibleKg,
                compromisos.CantidadDisponiblePaquetes,
                ct);

            var resultados = new List<CombinacionEvaluada>();

            foreach (var idsSeleccionados in combinaciones)
            {
                ct.ThrowIfCancellationRequested();

                CombinacionEvaluada evaluada = await EvaluarCombinacionAsync(
                    servicioRutas,
                    recorridoBase,
                    rutaBase,
                    compromisos,
                    candidatosPorId,
                    idsSeleccionados,
                    ct);

                if (evaluada.DesvioKm > desvioMaximoKm)
                {
                    continue;
                }

                resultados.Add(evaluada);
            }

            return resultados;
        }

        private static ( CombinacionEvaluada? MayorGanancia, CombinacionEvaluada? MenorDesvio, CombinacionEvaluada? Equilibrada) 
        SeleccionarAlternativas( IReadOnlyList<CombinacionEvaluada> combinaciones, decimal proporcionMinimaIngreso)
        {
            ArgumentNullException.ThrowIfNull(combinaciones);

            if (proporcionMinimaIngreso <= 0m || proporcionMinimaIngreso > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(proporcionMinimaIngreso),
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
            decimal proporcionMinimaIngreso,
            double distanciaConCompromisosKm)
        {
            ArgumentNullException.ThrowIfNull(compromisos);

            var alternativas = SeleccionarAlternativas( combinaciones, proporcionMinimaIngreso);

            var opciones = new (string Etiqueta, CombinacionEvaluada? Combinacion)[]
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

                int[] idsOrdenados = combinacion.EnviosSeleccionados.OrderBy(id => id).ToArray();

                string clave = string.Join(",", idsOrdenados);

                if (propuestasPorCombinacion.TryGetValue(clave,out var existente))
                {
                    existente.Etiquetas.Add(opcion.Etiqueta);
                    continue;
                }

                var propuesta = new PropuestaOptimizacionDTO
                {
                    Etiquetas = [opcion.Etiqueta],
                    EnviosNuevosIds = Array.AsReadOnly(idsOrdenados),
                    IngresoAdicional = combinacion.IngresoAdicional,
                    IngresoTotal = compromisos.IngresoComprometido + combinacion.IngresoAdicional,
                    PesoAdicionalKg = combinacion.PesoAdicionalKg,
                    PesoTotalKg = compromisos.PesoComprometidoKg + combinacion.PesoAdicionalKg,
                    CantidadTotalPaquetes = compromisos.CantidadComprometidaPaquetes + idsOrdenados.Length,
                    DesvioTotalKm = combinacion.DesvioKm,
                    DesvioAdicionalKm = Math.Max(0, combinacion.Recorrido.DistanciaKm - distanciaConCompromisosKm),
                    DistanciaTotalKm = combinacion.Recorrido.DistanciaKm,
                    DuracionTotalMinutos = combinacion.Recorrido.DuracionMinutos
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
            else if (solicitud.FechaSalida is DateTime fechaSalida)
            {
                salida = await _resolvedor.ResolverSalidaEnFechaAsync(
                    solicitud.RutaId,
                    fechaSalida,
                    conductorId);
            } else
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
            var erroresCapacidad = _validadorSalida.ValidarCapacidad(salida,compromisos);

            if (erroresCapacidad.Count > 0)
            {
                return new ResultadoOptimizacionDTO
                {
                    Estado = EstadoResultadoOptimizacion.SalidaNoHabilitada,
                    Mensajes = erroresCapacidad,
                    IngresoComprometido = compromisos.IngresoComprometido
                };
            }

            // Con un solo servicio de rutas (modo mock, o sin estimador) hay un solo paso.
            bool dosPasos = !solicitud.SoloEstimado
                && _estimador is not null
                && _servicioRutas is not IEstimadorDeRutas;

            IServicioRutas rutasPaso1 = (IServicioRutas?)_estimador ?? _servicioRutas;
            double limiteExacto = solicitud.DesvioMaximoKm;
            double limitePaso1 = dosPasos ? ConMargen(limiteExacto) : limiteExacto;

            var evaluacionDesvio = await _evaluadorDesvio.EvaluarAsync(
                salida,
                compromisos,
                limitePaso1,
                rutasPaso1,
                ct);

            var mensajes = new List<string>();
            var propuestas = new List<PropuestaOptimizacionDTO>();
            bool distanciasEstimadas = true;

            // recorrido con los envios ya aceptados, de la misma fuente que las combinaciones
            ResultadoRuta rutaConCompromisos = evaluacionDesvio.RutaConEnvios;

            if (evaluacionDesvio.SuperaLimite)
            {
                mensajes.Add(MensajeCompromisosSuperanDesvio);
            }else if (compromisos.PesoDisponibleKg == 0 || compromisos.CantidadDisponiblePaquetes == 0)
            {
                mensajes.Add(
                    "La salida no tiene capacidad disponible para nuevos envíos.");
            }else
            {
                var candidatos = await _buscadorCandidatos.BuscarAsync(salida, compromisos);
                ct.ThrowIfCancellationRequested();

                if (solicitud.EnviosExcluidos is { Count: > 0 })
                {
                    HashSet<int> excluidos = [.. solicitud.EnviosExcluidos]; // el .. arma un HashSet<int> con todos los elementos de EnviosExcluidos
                    candidatos = candidatos.Where(envio => !excluidos.Contains(envio.Id)).ToList();
                }

                if (candidatos.Count == 0)
                {
                    mensajes.Add(
                        "No hay envíos candidatos para esta salida.");
                }else
                {
                    var recorridoBase =
                        _constructorRecorrido.ConstruirBase(salida);

                    // Paso 1: todas las combinaciones con el estimador.
                    var combinaciones = await EvaluarCombinacionesAsync(
                        rutasPaso1,
                        recorridoBase,
                        evaluacionDesvio.RutaBase,
                        compromisos,
                        candidatos,
                        limitePaso1,
                        ct);

                    // Paso 2: las finalistas, con rutas reales.
                    if (dosPasos && combinaciones.Count > 0)
                    {
                        var confirmadas = await ConfirmarConRutasRealesAsync(
                            salida,
                            compromisos,
                            recorridoBase,
                            candidatos.ToDictionary(envio => envio.Id),
                            combinaciones,
                            limiteExacto,
                            ct);

                        if (confirmadas is not null)
                        {
                            distanciasEstimadas = false;
                            combinaciones = confirmadas.Combinaciones;
                            rutaConCompromisos = confirmadas.RutaConCompromisos ?? rutaConCompromisos;

                            if (confirmadas.CompromisosSuperanDesvio)
                            {
                                mensajes.Add(MensajeCompromisosSuperanDesvio);
                            }
                        }
                    }

                    // Sin confirmar con rutas reales: el limite exacto se aplica sobre lo estimado.
                    if (distanciasEstimadas)
                    {
                        if (evaluacionDesvio.DesvioKm > limiteExacto)
                        {
                            mensajes.Add(MensajeCompromisosSuperanDesvio);
                            combinaciones = [];
                        }else
                        {
                            combinaciones = combinaciones
                                .Where(combinacion => combinacion.DesvioKm <= limiteExacto)
                                .ToList();
                        }
                    }

                    if (mensajes.Count == 0)
                    {
                        propuestas = ArmarPropuestas(
                            combinaciones,
                            compromisos,
                            _proporcionMinimaIngreso,
                            rutaConCompromisos.DistanciaKm);

                        if (propuestas.Count == 0)
                        {
                            mensajes.Add(
                                "Ninguna combinación evaluada cumple todas las restricciones.");
                        }
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
                MomentoEvaluacion = salida.MomentoEvaluacion,
                IngresoComprometido = compromisos.IngresoComprometido,
                DistanciasEstimadas = distanciasEstimadas
            };
        }

        // Paso 2. Devuelve null si alguna respuesta vino sin trazado (estimada por el plan B) o si
        // ORS fallo por algo que no sea "no hay camino" (404): se descarta todo lo real y se usa el paso 1.
        private async Task<ConfirmacionConRutasReales?> ConfirmarConRutasRealesAsync(
                SalidaOptimizacion salida,
                CompromisosSalida compromisos,
                IReadOnlyList<Coordenada> recorridoBase,
                IReadOnlyDictionary<int, Envio> candidatosPorId,
                IReadOnlyList<CombinacionEvaluada> estimadas,
                double limiteExacto,
                CancellationToken ct)
        {
            EvaluacionDesvio evaluacionReal;
            try
            {
                evaluacionReal = await _evaluadorDesvio.EvaluarAsync(
                    salida,
                    compromisos,
                    limiteExacto,
                    _servicioRutas,
                    ct);
            }
            catch (RutaNoEncontradaException)
            {
                return null;
            }

            if (!TieneTrazado(evaluacionReal.RutaBase) || !TieneTrazado(evaluacionReal.RutaConEnvios))
            {
                return null;
            }

            if (evaluacionReal.SuperaLimite)
            {
                return new ConfirmacionConRutasReales { CompromisosSuperanDesvio = true };
            }

            var confirmadas = new List<CombinacionEvaluada>();

            foreach (var estimada in ElegirFinalistas(estimadas, CantidadDeFinalistas, _proporcionMinimaIngreso))
            {
                CombinacionEvaluada real;
                try
                {
                    real = await EvaluarCombinacionAsync(
                        _servicioRutas,
                        recorridoBase,
                        evaluacionReal.RutaBase,
                        compromisos,
                        candidatosPorId,
                        estimada.EnviosSeleccionados,
                        ct);
                }
                catch (RutaNoEncontradaException ex) when (ex.CodigoHttp == 404)
                {
                    // no hay camino pasando por esos envios: se descarta y queda la siguiente
                    continue;
                }
                catch (RutaNoEncontradaException)
                {
                    return null;
                }

                if (!TieneTrazado(real.Recorrido))
                {
                    return null;
                }

                // Si con los km reales se pasa del limite, se descarta y queda la siguiente.
                if (real.DesvioKm <= limiteExacto)
                {
                    confirmadas.Add(real);
                }
            }

            return new ConfirmacionConRutasReales
            {
                Combinaciones = confirmadas,
                RutaConCompromisos = evaluacionReal.RutaConEnvios
            };
        }

        // Las mejores de cada criterio, intercaladas (1a de mayor ganancia, 1a de menor desvio,
        // 1a equilibrada, 2a de mayor ganancia...), sin repetir, hasta completar la cantidad.
        private static List<CombinacionEvaluada> ElegirFinalistas(
                IReadOnlyList<CombinacionEvaluada> combinaciones,
                int cantidad,
                decimal proporcionMinimaIngreso)
        {
            decimal ingresoMaximo = combinaciones.Max(combinacion => combinacion.IngresoAdicional);

            List<CombinacionEvaluada>[] listas =
            [
                combinaciones
                    .OrderByDescending(combinacion => combinacion.IngresoAdicional)
                    .ThenBy(combinacion => combinacion.DesvioKm)
                    .ToList(),
                combinaciones
                    .OrderBy(combinacion => combinacion.DesvioKm)
                    .ThenByDescending(combinacion => combinacion.IngresoAdicional)
                    .ToList(),
                combinaciones
                    .Where(combinacion => combinacion.IngresoAdicional >= ingresoMaximo * proporcionMinimaIngreso)
                    .OrderBy(combinacion => combinacion.DesvioKm)
                    .ThenByDescending(combinacion => combinacion.IngresoAdicional)
                    .ToList()
            ];

            var finalistas = new List<CombinacionEvaluada>();

            for (int posicion = 0; finalistas.Count < cantidad && listas.Any(lista => posicion < lista.Count); posicion++)
            {
                foreach (var lista in listas)
                {
                    if (finalistas.Count < cantidad && posicion < lista.Count && !finalistas.Contains(lista[posicion]))
                    {
                        finalistas.Add(lista[posicion]);
                    }
                }
            }

            return finalistas;
        }

        private static bool TieneTrazado(ResultadoRuta ruta) => !string.IsNullOrEmpty(ruta.GeometriaJson);

        // Limite del paso 1 con margen, sin pasarse del maximo de double (Potencial usa double.MaxValue).
        private static double ConMargen(double desvioMaximoKm)
        {
            double conMargen = desvioMaximoKm * MargenDelPaso1;
            return double.IsFinite(conMargen) ? conMargen : double.MaxValue;
        }

        private sealed class ConfirmacionConRutasReales
        {
            public List<CombinacionEvaluada> Combinaciones { get; init; } = [];
            public bool CompromisosSuperanDesvio { get; init; }
            public ResultadoRuta? RutaConCompromisos { get; init; }
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
