using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios
{
    public interface IOportunidadServicio
    {
        // rutas activas del conductor con su proxima salida. Es lo que llena el selector.
        Task<List<SalidaDeRutaDTO>> ListarSalidasAsync(int conductorId, CancellationToken ct = default);

        // envios compatibles con una salida concreta de la ruta. NULL si la ruta no existe,
        // no es del conductor o no tiene una salida valida.
        Task<OportunidadesDeRutaDTO?> ListarAsync(int rutaId, int conductorId, DateTime? fechaSalida = null, CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia, double? desvioMaximoKm = null, CancellationToken ct = default);

        // compara la ruta habitual con la ruta llevando el paquete. NULL si la ruta o el envio no existen.
        // Si no se puede aceptar, explica por que.
        Task<OportunidadDetalleDTO?> ObtenerDetalleAsync(int rutaId, int envioId, int conductorId, DateTime? fechaSalida = null, CancellationToken ct = default);

        // reserva el envio para la salida. Si todavia no existe, materializa el Viaje como
        // snapshot de la ruta. Vuelve a validar todo: entre que se vio la oportunidad y se
        // acepto, otro conductor pudo haberla tomado.
        Task<ResultadoOperacionOportunidad> AceptarAsync(int rutaId, int envioId, int conductorId, DateTime? fechaSalida = null, int? viajeId = null);
        Task<ResultadoAceptacionPropuestaDTO> AceptarPropuestaAsync(int rutaId, int? viajeId, DateTime fechaSalidaProgramada, IReadOnlyList<int> envioIds, int conductorId);
    }

    public class OportunidadServicio : IOportunidadServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        // por debajo de esto, el envio es ruido del calculo de rutas y no se considera desvio
        private const double DESVIO_DESPRECIABLE_KM = 0.1;

        private readonly IRutaRepositorio _rutaRepositorio;
        private readonly IViajeRepositorio _viajeRepositorio;
        private readonly IEnvioRepositorio _envioRepositorio;
        private readonly IServicioRutas _servicioRutas;
        private readonly TimeProvider _reloj;

        public OportunidadServicio(IRutaRepositorio rutaRepositorio, IViajeRepositorio viajeRepositorio, IEnvioRepositorio envioRepositorio, IServicioRutas servicioRutas, TimeProvider reloj)
        {
            _rutaRepositorio = rutaRepositorio;
            _viajeRepositorio = viajeRepositorio;
            _envioRepositorio = envioRepositorio;
            _servicioRutas = servicioRutas;
            _reloj = reloj;
        }

        public async Task<List<SalidaDeRutaDTO>> ListarSalidasAsync(int conductorId, CancellationToken ct = default)
        {
            List<Ruta> rutas = await _rutaRepositorio.ObtenerRutasActivasDeUnUsuarioAsync(conductorId);

            DateTime ahora = Ahora();
            List<SalidaDeRutaDTO> salidas = [];

            foreach (Ruta ruta in rutas)
            {
                DateTime? fecha = CalendarioDeSalidas.Proxima(ruta, ahora);

                if (fecha is null)
                {
                    continue;
                }

                Viaje? viaje = await _viajeRepositorio.ObtenerPorRutaYFechaAsync(ruta.Id, fecha.Value);

                salidas.Add(new SalidaDeRutaDTO
                {
                    RutaId = ruta.Id,
                    Origen = viaje?.DireccionOrigen.Texto ?? ruta.DireccionOrigen.Texto,
                    Destino = viaje?.DireccionDestino.Texto ?? ruta.DireccionDestino.Texto,
                    FechaSalida = fecha.Value,
                    FechaLlegada = viaje?.FechaLlegada
                        ?? fecha.Value + CalendarioDeSalidas.Duracion(ruta),
                    ViajeId = viaje?.Id
                });
            }

            return salidas.OrderBy(s => s.FechaSalida).ToList();
        }

        public async Task<OportunidadesDeRutaDTO?> ListarAsync(int rutaId, int conductorId, DateTime? fechaSalida = null, CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia, double? desvioMaximoKm = null, CancellationToken ct = default)
        {
            Salida? salida = await ResolverSalidaAsync(rutaId, conductorId, fechaSalida, paraModificar: false);

            if (salida is null)
            {
                return null;
            }

            OportunidadesDeRutaDTO resultado = new()
            {
                RutaId = salida.RutaId,
                ViajeId = salida.ViajeId,
                RutaOrigen = salida.Origen.Texto,
                RutaDestino = salida.Destino.Texto,
                FechaSalida = salida.FechaSalida,
                CapacidadRestante = CapacidadRestante(salida),
                PesoRestanteKg = PesoRestante(salida),
                AvisosDeLaSalida = MotivosDeLaSalida(salida)
            };

            if (resultado.AvisosDeLaSalida.Count > 0)
            {
                return resultado;
            }

            List<Envio> candidatos = await _envioRepositorio.ListarDisponiblesAsync(salida.FechaSalida, conductorId);

            if (candidatos.Count == 0)
            {
                return resultado;
            }

            ResultadoRuta habitual = await CalcularHabitualAsync(salida, ct);
            resultado.DistanciaHabitualKm = Redondear(habitual.DistanciaKm);
            resultado.TiempoHabitualMin = Redondear(habitual.DuracionMinutos);

            foreach (Envio envio in candidatos)
            {
                // fecha, peso y capacidad se chequean antes de pedir la ruta (son gratis)
                if (MotivosDelEnvio(salida, envio, conductorId).Count > 0)
                {
                    continue;
                }

                ResultadoRuta conEnvio;
                try
                {
                    conEnvio = await CalcularConEnvioAsync(salida, envio, ct);
                }
                catch (RutaNoEncontradaException)
                {
                    // si no hay camino posible pasando por el retiro y la entrega, no es oportunidad
                    continue;
                }

                OportunidadDTO oportunidad = Completar(new OportunidadDTO(), envio, habitual, conEnvio);

                if (desvioMaximoKm is not null && oportunidad.DesvioKm > desvioMaximoKm.Value)
                {
                    continue;
                }

                resultado.Oportunidades.Add(oportunidad);
            }

            resultado.Oportunidades = Ordenar(resultado.Oportunidades, orden);
            return resultado;
        }

        public async Task<OportunidadDetalleDTO?> ObtenerDetalleAsync(int rutaId, int envioId, int conductorId, DateTime? fechaSalida = null, CancellationToken ct = default)
        {
            Salida? salida = await ResolverSalidaAsync(rutaId, conductorId, fechaSalida, paraModificar: false);

            if (salida is null)
            {
                return null;
            }

            Envio? envio = await _envioRepositorio.ObtenerParaOportunidadAsync(envioId);

            if (envio is null)
            {
                return null;
            }

            ResultadoRuta habitual = await CalcularHabitualAsync(salida, ct);
            ResultadoRuta conEnvio = await CalcularConEnvioAsync(salida, envio, ct);

            OportunidadDetalleDTO detalle = Completar(new OportunidadDetalleDTO(), envio, habitual, conEnvio);

            detalle.RutaId = salida.RutaId;
            detalle.ViajeId = salida.ViajeId;
            detalle.RutaOrigen = salida.Origen.Texto;
            detalle.RutaDestino = salida.Destino.Texto;
            detalle.FechaSalida = salida.FechaSalida;
            detalle.DistanciaHabitualKm = Redondear(habitual.DistanciaKm);
            detalle.DistanciaConEnvioKm = Redondear(conEnvio.DistanciaKm);
            detalle.TiempoHabitualMin = Redondear(habitual.DuracionMinutos);
            detalle.TiempoConEnvioMin = Redondear(conEnvio.DuracionMinutos);
            detalle.CapacidadRestante = CapacidadRestante(salida);
            detalle.PesoRestanteKg = PesoRestante(salida);
            detalle.Motivos = Motivos(salida, envio, conductorId);

            return detalle;
        }

        public async Task<ResultadoOperacionOportunidad> AceptarAsync(int rutaId, int envioId, int conductorId, DateTime? fechaSalida = null, int? viajeId = null)
        {
            Salida? salida = await ResolverSalidaAsync(rutaId, conductorId, fechaSalida, paraModificar: true, viajeId: viajeId);
            if (salida is null)
            {
                return ResultadoOperacionOportunidad.NoExiste();
            }

            Envio? envio = await _envioRepositorio.ObtenerParaOportunidadAsync(envioId);

            if (envio is null)
            {
                return ResultadoOperacionOportunidad.NoExiste();
            }

            List<string> motivos = Motivos(salida, envio, conductorId);

            if (motivos.Count > 0)
            {
                return ResultadoOperacionOportunidad.EnConflicto(string.Join(" ", motivos));
            }

            // El viaje se materializa recien aca, con el snapshot de la ruta.
            Viaje viaje = salida.Viaje ?? SnapshotDeRuta.Crear(salida.Ruta, salida.FechaSalida, salida.FechaLlegada);

            if (salida.Viaje is null)
            {
                await _viajeRepositorio.AgregarSinGuardarAsync(viaje);
            }

            Asignacion asignacion = new()
            {
                Envio = envio,
                EnvioId = envio.Id,
                Estado = EstadoViaje.CONFIRMADO,
                FechaAceptacion = Ahora()
            };

            viaje.Asignaciones.Add(asignacion);
            envio.Estado = EstadoEnvio.ConductorAsignado;

            // El viaje nuevo, la asignacion y el estado del envio se persisten juntos.
            await _viajeRepositorio.GuardarCambiosAsync();

            return ResultadoOperacionOportunidad.Exitoso(asignacion.Id, viaje.Id);
        }
        public async Task<ResultadoAceptacionPropuestaDTO> AceptarPropuestaAsync(int rutaId, int? viajeId, DateTime fechaSalidaProgramada, IReadOnlyList<int> envioIds, int conductorId)
        {
            ArgumentNullException.ThrowIfNull(envioIds);

            if (rutaId <= 0 || conductorId <= 0
                || (viajeId.HasValue && viajeId.Value <= 0)
                || fechaSalidaProgramada == default)
            {
                return new ResultadoAceptacionPropuestaDTO
                {
                    Error = "Los datos de la salida no son válidos."
                };
            }

            if (envioIds.Count == 0)
            {
                return new ResultadoAceptacionPropuestaDTO
                {
                    Error = "La propuesta debe incluir al menos un envío."
                };
            }

            if (envioIds.Any(id => id <= 0))
            {
                return new ResultadoAceptacionPropuestaDTO
                {
                    Error = "La propuesta contiene identificadores de envío inválidos."
                };
            }

            if (envioIds.Distinct().Count() != envioIds.Count)
            {
                return new ResultadoAceptacionPropuestaDTO
                {
                    Error = "La propuesta contiene envíos repetidos."
                };
            }

            var aceptados = new List<int>();
            int? viajeDestinoId = viajeId;

            foreach (int envioId in envioIds)
            {
                ResultadoOperacionOportunidad resultado =
                    await AceptarAsync(rutaId, envioId, conductorId, fechaSalidaProgramada, viajeDestinoId);

                if (!resultado.EsExitoso)
                {
                    return new ResultadoAceptacionPropuestaDTO
                    {
                        EnviosAceptadosIds = aceptados.AsReadOnly(),
                        ViajeId = aceptados.Count > 0 ? viajeDestinoId : null,
                        EnvioFallidoId = envioId,
                        Error = resultado.NoEncontrado
                            ? $"No se encontró la salida o el envío #{envioId}."
                            : resultado.Conflicto
                                ?? $"No se pudo aceptar el envío #{envioId}."
                    };
                }

                aceptados.Add(envioId);
                viajeDestinoId = resultado.ViajeId;
            }

            return new ResultadoAceptacionPropuestaDTO
            {
                EnviosAceptadosIds = aceptados.AsReadOnly(),
                ViajeId = viajeDestinoId
            };
        }

        // HELPERS

        #region Salida
        private sealed class Salida
        {
            public required Ruta Ruta { get; init; }
            public Viaje? Viaje { get; init; }
            public required DateTime FechaSalida { get; init; }
            public required DateTime FechaLlegada { get; init; }

            public int RutaId => Ruta.Id;
            public int? ViajeId => Viaje?.Id;
            public Direccion Origen => Viaje?.DireccionOrigen ?? Ruta.DireccionOrigen;
            public Direccion Destino => Viaje?.DireccionDestino ?? Ruta.DireccionDestino;
            public IEnumerable<Parada> Paradas => Viaje?.Paradas ?? Ruta.Paradas;
            public double PesoMax => Viaje?.PesoMax ?? Ruta.PesoMax;
            public int CapacidadMax => Viaje?.CapacidadMaxPaquetes ?? Ruta.CapacidadMaxPaquetes;
            public IEnumerable<Asignacion> Asignaciones => Viaje?.Asignaciones ?? [];
        }

        private async Task<Salida?> ResolverSalidaAsync(
    int rutaId,
    int conductorId,
    DateTime? fecha,
    bool paraModificar,
    int? viajeId = null)
        {
            // Si se eligió un viaje existente, usamos su snapshot.
            if (viajeId is int id)
            {
                Viaje? viajeSeleccionado = paraModificar
                    ? await _viajeRepositorio.ObtenerParaAsignarAsync(id)
                    : await _viajeRepositorio.ObtenerDetalleAsync(id);

                if (viajeSeleccionado is null
                    || viajeSeleccionado.RutaId != rutaId
                    || viajeSeleccionado.Ruta.UsuarioId != conductorId)
                {
                    return null;
                }

                return new Salida
                {
                    Ruta = viajeSeleccionado.Ruta,
                    Viaje = viajeSeleccionado,
                    FechaSalida = viajeSeleccionado.FechaSalida,
                    FechaLlegada = viajeSeleccionado.FechaLlegada
                        ?? viajeSeleccionado.FechaSalida
                            + CalendarioDeSalidas.Duracion(
                                viajeSeleccionado.Ruta)
                };
            }

            // Si no hay un viaje seleccionado, resolvemos una salida de la ruta.
            Ruta? ruta = paraModificar
                ? await _rutaRepositorio.ObtenerPorIdParaModificarAsync(rutaId)
                : await _rutaRepositorio.ObtenerPorIdAsync(rutaId);

            if (ruta is null || ruta.UsuarioId != conductorId)
            {
                return null;
            }

            DateTime ahora = Ahora();
            DateTime fechaSalida;

            if (fecha is null)
            {
                DateTime? proxima = CalendarioDeSalidas.Proxima(ruta, ahora);

                if (proxima is null)
                {
                    return null;
                }

                fechaSalida = proxima.Value;
            }
            else
            {
                DateTime dia = fecha.Value.Date;

                bool dentroDeVigencia =
                    dia >= ruta.FechaInicio.Date
                    && (ruta.FechaFin is null
                        || dia <= ruta.FechaFin.Value.Date);

                bool esDiaDeLaRuta = ruta.RutaDias.Any(
                    d => d.DiaSemana == dia.DayOfWeek);

                if (!dentroDeVigencia || !esDiaDeLaRuta)
                {
                    return null;
                }

                fechaSalida = dia + ruta.HoraSalida.ToTimeSpan();
            }

            Viaje? viaje = paraModificar
                ? await _viajeRepositorio.ObtenerParaAsignarPorRutaYFechaAsync(
                    ruta.Id,
                    fechaSalida)
                : await _viajeRepositorio.ObtenerPorRutaYFechaAsync(
                    ruta.Id,
                    fechaSalida);

            return new Salida
            {
                Ruta = ruta,
                Viaje = viaje,
                FechaSalida = viaje?.FechaSalida ?? fechaSalida,
                FechaLlegada = viaje?.FechaLlegada
                    ?? fechaSalida + CalendarioDeSalidas.Duracion(ruta)
            };
        }
        #endregion

        #region Reglas
        private List<string> Motivos(Salida salida, Envio envio, int conductorId)
        {
            List<string> motivos = MotivosDeLaSalida(salida);
            motivos.AddRange(MotivosDelEnvio(salida, envio, conductorId));
            return motivos;
        }

        private List<string> MotivosDeLaSalida(Salida salida)
        {
            List<string> motivos = [];

            if (salida.Ruta.Estado != EstadoRuta.ACTIVA)
            {
                motivos.Add(
                    "La ruta está inactiva: activala para volver a buscar oportunidades.");
            }

            if (salida.Viaje is { Estado: EstadoActividadViaje.INACTIVO })
            {
                motivos.Add("El viaje de esa salida está inactivo.");
            }

            if (salida.Viaje is null && salida.FechaSalida < Ahora())
            {
                motivos.Add("La salida ya pasó.");
            }

            if (CapacidadRestante(salida) <= 0)
            {
                motivos.Add("El viaje no tiene lugar para más paquetes.");
            }

            return motivos;
        }

        private List<string> MotivosDelEnvio(Salida salida, Envio envio, int conductorId)
        {
            List<string> motivos = [];

            if (envio.UsuarioId == conductorId)
            {
                motivos.Add("Usted mismo publicó este envío.");
            }

            if (envio.Estado == EstadoEnvio.Cancelado)
            {
                motivos.Add("El envío fue cancelado.");
            }
            else if (envio.Estado != EstadoEnvio.BuscandoConductor
                || envio.Asignaciones.Any(asignacion => asignacion.Estado != EstadoViaje.CANCELADO))
            {
                motivos.Add("El envío ya fue asignado a otro conductor.");
            }

            DateTime momentoEvaluacion =
                ReferenciaTemporalOptimizacion.DeterminarMomentoEvaluacion(salida.FechaSalida, Ahora());

            DateTime dia = momentoEvaluacion.Date;

            if (dia < envio.FechaDesde.Date || dia > envio.FechaHasta.Date)
            {
                motivos.Add(
                    $"La fecha evaluada es {dia:dd/MM/yyyy} "
                    + $"y el envío se puede llevar del "
                    + $"{envio.FechaDesde:dd/MM/yyyy} "
                    + $"al {envio.FechaHasta:dd/MM/yyyy}.");
            }

            decimal pesoRestanteKg = (decimal)PesoRestante(salida);
            decimal pesoEnvioKg = (decimal)envio.Paquete.PesoKg;

            if (pesoEnvioKg > pesoRestanteKg)
            {
                motivos.Add(
                    $"El paquete del envío pesa {pesoEnvioKg:0.##} kg "
                    + $"y al viaje le quedan {pesoRestanteKg:0.##} kg.");
            }

            return motivos;
        }

        // las asignaciones canceladas liberan el lugar y el peso
        private static IEnumerable<Asignacion> AsignacionesVigentes(Salida salida) => salida.Asignaciones.Where(a => a.Estado != EstadoViaje.CANCELADO);

        private static int CapacidadRestante(Salida salida) => salida.CapacidadMax - AsignacionesVigentes(salida).Count();

        private static double PesoRestante(Salida salida)
        {
            decimal pesoMaximoKg = (decimal)salida.PesoMax;

            decimal pesoAsignadoKg = AsignacionesVigentes(salida)
                .Sum(asignacion => (decimal)(asignacion.Envio?.Paquete.PesoKg ?? 0));

            return (double)(pesoMaximoKg - pesoAsignadoKg);
        }
        #endregion

        #region Rutas
        private Task<ResultadoRuta> CalcularHabitualAsync(Salida salida, CancellationToken ct)
            => _servicioRutas.CalcularRutaAsync(ACoordenada(salida.Origen), ACoordenada(salida.Destino), Paradas(salida), ct);

        private Task<ResultadoRuta> CalcularConEnvioAsync(Salida salida, Envio envio, CancellationToken ct)
        {
            List<Coordenada> recorrido = [ACoordenada(salida.Origen), .. Paradas(salida), ACoordenada(salida.Destino)];

            List<Coordenada> conEnvio = PlanificadorDeRecorrido.InsertarEnvio(recorrido, ACoordenada(envio.DireccionRetiro), ACoordenada(envio.DireccionEntrega));

            // el primero y el ultimo siguen siendo origen y destino. lo del medio son paradas
            return _servicioRutas.CalcularRutaAsync(conEnvio[0], conEnvio[^1], conEnvio.GetRange(1, conEnvio.Count - 2), ct);
        }

        private static List<Coordenada> Paradas(Salida salida)
            => salida.Paradas
            .OrderBy(p => p.Orden)
            .ThenBy(p => p.Id)
            .Select(p => ACoordenada(p.Direccion))
            .ToList();

        private static Coordenada ACoordenada(Direccion direccion) => new(direccion.Latitud, direccion.Longitud);
        #endregion

        #region Armado
        // el where T : OportunidadDTO es para que se pueda usar con OportunidadDTO y OportunidadDetalleDTO, que heredan de ese tipo
        private static T Completar<T>(T dto, Envio envio, ResultadoRuta habitual, ResultadoRuta conEnvio) where T : OportunidadDTO {
            double desvioKm = Math.Max(0, conEnvio.DistanciaKm - habitual.DistanciaKm);
            double minutosExtra = Math.Max(0, conEnvio.DuracionMinutos - habitual.DuracionMinutos);

            dto.EnvioId = envio.Id;
            dto.Descripcion = envio.Paquete.Descripcion;
            dto.Tamanio = envio.Paquete.Tamanio;
            dto.PesoKg = envio.Paquete.PesoKg;
            dto.Pago = envio.Monto;
            dto.RetiroDireccion = envio.DireccionRetiro?.Texto ?? string.Empty;
            dto.EntregaDireccion = envio.DireccionEntrega?.Texto ?? string.Empty;
            dto.FechaDesde = envio.FechaDesde;
            dto.FechaHasta = envio.FechaHasta;
            dto.RemitenteNombre = envio.Usuario is null ? string.Empty : $"{envio.Usuario.Nombre} {envio.Usuario.Apellido}".Trim();
            dto.DesvioKm = Redondear(desvioKm);
            dto.TiempoAdicionalMin = Redondear(minutosExtra);
            dto.Compatibilidad = conEnvio.DistanciaKm <= 0 ? 100 : (int)Math.Round(100 * Math.Min(1, habitual.DistanciaKm / conEnvio.DistanciaKm));
            dto.GananciaPorKmAdicional = desvioKm < DESVIO_DESPRECIABLE_KM ? null : Math.Round(envio.Monto / (decimal)desvioKm, 2);

            return dto;
        }

        private static List<OportunidadDTO> Ordenar(List<OportunidadDTO> oportunidades, CriterioOrdenOportunidad orden)
            => orden switch
            {
                CriterioOrdenOportunidad.Desvio => oportunidades.OrderBy(o => o.DesvioKm).ThenByDescending(o => o.Pago).ToList(),
                CriterioOrdenOportunidad.Compatibilidad => oportunidades.OrderByDescending(o => o.Compatibilidad).ThenByDescending(o => o.Pago).ToList(),
                // segun el enunciado, a igual ganancia, gana el de menor desvio
                _ => oportunidades.OrderByDescending(o => o.Pago).ThenBy(o => o.DesvioKm).ToList()
            };

        private static double Redondear(double valor) => Math.Round(valor, 1);

        private DateTime Ahora() => TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), ZonaArgentina).DateTime;
        #endregion
    }
}
