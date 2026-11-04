using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios
{
    public interface IOportunidadServicio
    {
        // envios compatibles con el viaje. NULL si el viaje no existe o no es del conductor.
        Task<OportunidadesDelViajeDTO> ListarAsync(int viajeId, int conductorId, CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia, double? desvioMaximoKm = null, CancellationToken ct = default);

        // compara la ruta habitual con la ruta llevando el paquete. NULL si el viaje no es del conductor
        // o el envio no existe. Si no se puede aceptar, explica por que
        Task<OportunidadDetalleDTO> ObtenerDetalleAsync(int viajeId, int envioId, int conductorId, CancellationToken ct = default);

        // reserva el envio para el viaje. Vuelve a validar todo: entre que se vio la oportunidad y se acepto,
        // otro conductor pudo haberla tomado
        Task<ResultadoOperacionOportunidad> AceptarAsync(int viajeId, int envioId, int conductorId);
    }

    public class OportunidadServicio : IOportunidadServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        // por debajo de esto, el envio es ruido del calculo de rutas y no se considera desvio
        private const double DESVIO_DESPRECIABLE_KM = 0.1;

        private readonly IViajeRepositorio _viajeRepositorio;
        private readonly IEnvioRepositorio _envioRepositorio;
        private readonly IServicioRutas _servicioRutas;
        private readonly TimeProvider _reloj;

        public OportunidadServicio(IViajeRepositorio viajeRepositorio, IEnvioRepositorio envioRepositorio, IServicioRutas servicioRutas, TimeProvider reloj)
        {
            _viajeRepositorio = viajeRepositorio;
            _envioRepositorio = envioRepositorio;
            _servicioRutas = servicioRutas;
            _reloj = reloj;
        }

        public async Task<OportunidadesDelViajeDTO?> ListarAsync(int viajeId, int conductorId, CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia, double? desvioMaximoKm = null, CancellationToken ct = default)
        {
            Viaje? viaje = await _viajeRepositorio.ObtenerDetalleAsync(viajeId);

            if (!EsDelConductor(viaje, conductorId))
            {
                return null;
            }

            OportunidadesDelViajeDTO resultado = new()
            {
                ViajeId = viaje!.Id,
                ViajeOrigen = viaje.DireccionOrigen.Texto,
                ViajeDestino = viaje.DireccionDestino.Texto,
                FechaSalida = viaje.FechaSalida,
                CapacidadRestante = CapacidadRestante(viaje),
                PesoRestanteKg = PesoRestante(viaje),
                AvisosDelViaje = MotivosDelViaje(viaje)
            };


            if (resultado.AvisosDelViaje.Count > 0)
            {
                return resultado;
            }

            List<Envio> candidatos = await _envioRepositorio.ListarDisponiblesAsync(viaje.FechaSalida, conductorId);

            if (candidatos.Count == 0)
            {
                return resultado;
            }

            ResultadoRuta habitual = await CalcularHabitualAsync(viaje, ct);
            resultado.DistanciaHabitualKm = Redondear(habitual.DistanciaKm);
            resultado.TiempoHabitualMin = Redondear(habitual.DuracionMinutos);

            foreach (Envio envio in candidatos)
            {
                // fecha, peso y capacidad se chequean antes de pedir la ruta (son gratis)
                if (MotivosDelEnvio(viaje, envio, conductorId).Count > 0)
                {
                    continue;
                }

                ResultadoRuta conEnvio;
                try
                {
                    conEnvio = await CalcularConEnvioAsync(viaje, envio, ct);
                }
                catch(RutaNoEncontradaException)
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

        public async Task<OportunidadDetalleDTO> ObtenerDetalleAsync(int viajeId, int envioId, int conductorId, CancellationToken ct = default)
        {
            Viaje? viaje = await _viajeRepositorio.ObtenerDetalleAsync(viajeId);

            if (!EsDelConductor(viaje, conductorId))
            {
                return null;
            }

            Envio? envio = await _envioRepositorio.ObtenerParaOportunidadAsync(envioId);

            if (envio is null)
            {
                return null;
            }

            ResultadoRuta habitual = await CalcularHabitualAsync(viaje!, ct);
            ResultadoRuta conEnvio = await CalcularConEnvioAsync(viaje!, envio, ct);

            OportunidadDetalleDTO detalle = Completar(new OportunidadDetalleDTO(), envio, habitual, conEnvio);

            detalle.ViajeId = viaje!.Id;
            detalle.ViajeOrigen = viaje.DireccionOrigen.Texto;
            detalle.ViajeDestino = viaje.DireccionDestino.Texto;
            detalle.FechaSalida = viaje.FechaSalida;
            detalle.DistanciaHabitualKm = Redondear(habitual.DistanciaKm);
            detalle.DistanciaConEnvioKm = Redondear(conEnvio.DistanciaKm);
            detalle.TiempoHabitualMin = Redondear(habitual.DuracionMinutos);
            detalle.TiempoConEnvioMin = Redondear(conEnvio.DuracionMinutos);
            detalle.CapacidadRestante = CapacidadRestante(viaje);
            detalle.PesoRestanteKg = PesoRestante(viaje);
            detalle.Motivos = Motivos(viaje, envio, conductorId);

            return detalle;
        }

        public async Task<ResultadoOperacionOportunidad> AceptarAsync(int viajeId, int envioId, int conductorId)
        {
            Viaje? viaje = await _viajeRepositorio.ObtenerParaAsignarAsync(viajeId);

            if (!EsDelConductor(viaje, conductorId))
            {
                return null;
            }

            Envio? envio = await _envioRepositorio.ObtenerParaOportunidadAsync(envioId);

            if (envio is null)
            {
                return ResultadoOperacionOportunidad.NoExiste();
            }

            List<string> motivos = Motivos(viaje!, envio, conductorId);

            if (motivos.Count > 0)
            {
                return ResultadoOperacionOportunidad.EnConflicto(string.Join(" ", motivos));
            }

            Asignacion asignacion = new()
            {
                Envio = envio,
                EnvioId = envio.Id,
                Estado = EstadoViaje.CONFIRMADO,
                FechaAceptacion = Ahora()
            };

            viaje!.Asignaciones.Add(asignacion);
            envio.Estado = EstadoEnvio.ConductorAsignado;

            // viaje y envio vienen del mismo DbContext entonces un solo guardado persiste los dos cambios
            await _viajeRepositorio.GuardarCambiosAsync();

            return ResultadoOperacionOportunidad.Exitoso(asignacion.Id);
        }

        // HELPERS

        #region Reglas
        private List<string> Motivos(Viaje viaje, Envio envio, int conductorId)
        {
            List<string> motivos = MotivosDelViaje(viaje);
            motivos.AddRange(MotivosDelEnvio(viaje, envio, conductorId));
            return motivos;
        }

        private List<string> MotivosDelViaje(Viaje viaje)
        {
            List<string> motivos = [];

            if (viaje.Estado != EstadoActividadViaje.ACTIVO)
            {
                motivos.Add("El viaje está inactivo: activalo para comenzar a buscar oportunidades.");
            }

            if (viaje.FechaSalida < Ahora())
            {
                motivos.Add("El viaje ya salió.");
            }

            if (CapacidadRestante(viaje) <= 0)
            {
                motivos.Add("El viaje no tiene lugar para más paquetes.");
            }

            return motivos;
        }

        private List<string> MotivosDelEnvio(Viaje viaje, Envio envio, int conductorId)
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
            else if (envio.Estado != EstadoEnvio.BuscandoConductor ||
                     envio.Asignaciones.Any(a => a.Estado != EstadoViaje.CANCELADO))
            {
                motivos.Add("El envío ya fue asignado a otro conductor.");
            }

            DateTime dia = viaje.FechaSalida.Date;
            if (dia < envio.FechaDesde.Date || dia > envio.FechaHasta.Date)
            {
                motivos.Add($"El viaje sale el {dia:dd/MM/yyyy} y el envío se puede llevar del {envio.FechaDesde:dd/MM/yyyy} al {envio.FechaHasta:dd/MM/yyyy}.");
            }

            double pesoRestante = PesoRestante(viaje);
            if (envio.Paquete.PesoKg > pesoRestante)
            {
                motivos.Add($"El paquete del envío pesa {envio.Paquete.PesoKg:0.##} kg y al viaje le quedan {pesoRestante:0.##} kg.");
            }

            return motivos;
        }

        private static bool EsDelConductor(Viaje? viaje, int conductorId) => viaje?.Ruta is not null && viaje.Ruta.UsuarioId == conductorId;

        // las asignaciones canceladas liberan el lugar y el peso
        private static IEnumerable<Asignacion> AsignacionesVigentes(Viaje viaje) => viaje.Asignaciones.Where(a => a.Estado != EstadoViaje.CANCELADO);

        private static int CapacidadRestante(Viaje viaje) => viaje.CapacidadMaxPaquetes - AsignacionesVigentes(viaje).Count();

        private static double PesoRestante(Viaje viaje) => viaje.PesoMax - AsignacionesVigentes(viaje).Sum(a => a.Envio?.Paquete.PesoKg ?? 0);
        #endregion

        #region Rutas
        private Task<ResultadoRuta> CalcularHabitualAsync(Viaje viaje, CancellationToken ct)
            => _servicioRutas.CalcularRutaAsync(Origen(viaje), Destino(viaje), Paradas(viaje), ct);

        private Task<ResultadoRuta> CalcularConEnvioAsync(Viaje viaje, Envio envio, CancellationToken ct)
        {
            List<Coordenada> recorrido = [Origen(viaje), .. Paradas(viaje), Destino(viaje)];

            List<Coordenada> conEnvio = PlanificadorDeRecorrido.InsertarEnvio(recorrido, ACoordenada(envio.DireccionRetiro), ACoordenada(envio.DireccionEntrega));

            // el primero y el ultimo siguen siendo origen y destino. lo del medio son paradas
            return _servicioRutas.CalcularRutaAsync(conEnvio[0], conEnvio[^1], conEnvio.GetRange(1, conEnvio.Count - 2), ct);
        }

        private static Coordenada Origen(Viaje viaje) => ACoordenada(viaje.DireccionOrigen);

        private static Coordenada Destino(Viaje viaje) => ACoordenada(viaje.DireccionDestino);

        private static List<Coordenada> Paradas(Viaje viaje)
            => viaje.Ruta.Paradas
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
