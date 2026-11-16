using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Servicios
{
    public interface IHistorialServicio
    {
        // Historial del usuario. Solo devuelve operaciones propias.
        //  - tipo: null trae todo.
        //  - desde / hasta: rango inclusivo de fechas, ambos opcionales. La fecha de un viaje
        //    es la de su ultima entrega (cuando se acredita la ganancia).
        // El resumen (viajes, envios transportados, ganancia) respeta el rango de fechas
        // pero no el tipo, para que las tarjetas no cambien al tocar un chip.
        //  - estado: Activo/Inactivo del viaje. Solo aplica a las filas de viaje: con este filtro
        //    puesto no se listan envios. Tampoco cambia el resumen ni los contadores.
        Task<HistorialDTO> ObtenerAsync(
            int usuarioId, TipoOperacionHistorial? tipo, EstadoActividadViaje? estado, DateTime? desde, DateTime? hasta);
    }

    public class HistorialServicio : IHistorialServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        private readonly IHistorialRepositorio _repositorio;
        private readonly TimeProvider _reloj;

        public HistorialServicio(IHistorialRepositorio repositorio, TimeProvider reloj)
        {
            _repositorio = repositorio;
            _reloj = reloj;
        }

        public async Task<HistorialDTO> ObtenerAsync(
            int usuarioId, TipoOperacionHistorial? tipo, EstadoActividadViaje? estado, DateTime? desde, DateTime? hasta)
        {
            // Si vienen al reves los damos vuelta en vez de devolver un historial vacio.
            if (desde is not null && hasta is not null && desde > hasta)
            {
                (desde, hasta) = (hasta, desde);
            }

            List<Viaje> viajes = await _repositorio.ListarViajesRealizadosAsync(usuarioId, Ahora());
            List<Envio> envios = await _repositorio.ListarEnviosEntregadosAsync(usuarioId);

            List<HistorialItemDTO> itemsViajes = viajes.Select(ArmarItemDeViaje).ToList();
            List<HistorialItemDTO> itemsEnvios = envios.Select(ArmarItemDeEnvio).ToList();

            // 1) Filtro por fecha: afecta resumen y lista.
            List<HistorialItemDTO> enRango = itemsViajes.Concat(itemsEnvios)
                .Where(i => EstaEnRango(i.Fecha, desde, hasta))
                .ToList();

            List<HistorialItemDTO> viajesEnRango = enRango
                .Where(i => i.Tipo == TipoOperacionHistorial.ViajeRealizado).ToList();

            // 2) Filtro por tipo y por estado del viaje: afecta solo la lista.
            List<HistorialItemDTO> lista = enRango
                .Where(i => tipo is null || i.Tipo == tipo.Value)
                .Where(i => estado is null
                            || (i.Tipo == TipoOperacionHistorial.ViajeRealizado && i.EstadoDelViaje == estado.Value))
                .OrderByDescending(i => i.Fecha)
                .ThenByDescending(i => i.Id)
                .ToList();

            return new HistorialDTO
            {
                ViajesRealizados = viajesEnRango.Count,
                EnviosTransportados = viajesEnRango.Sum(v => v.CantidadPaquetes),
                GananciaEfectiva = viajesEnRango.Sum(v => v.Monto),
                TotalOperaciones = enRango.Count,
                TotalEnviosEntregados = enRango.Count(i => i.Tipo == TipoOperacionHistorial.EnvioEntregado),
                Items = lista
            };
        }

        // Fecha y hora de Argentina, sin depender de la zona horaria del servidor.
        private DateTime Ahora() =>
            TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), ZonaArgentina).DateTime;

        // La ganancia efectiva es solo lo que se entrego: un paquete retirado o en camino
        // todavia es ganancia potencial, y una asignacion cancelada no cuenta.
        // La ganancia se acredita al entregar, asi que la fecha del viaje es la de su ultima entrega.
        private static HistorialItemDTO ArmarItemDeViaje(Viaje viaje)
        {
            List<Asignacion> entregadas = viaje.Asignaciones
                .Where(a => a.Estado != EstadoViaje.CANCELADO && a.Envio.Estado == EstadoEnvio.Entregado)
                .ToList();

            DateTime? ultimaEntrega = entregadas.Max(a => a.FechaEntrega);

            return new HistorialItemDTO
            {
                Tipo = TipoOperacionHistorial.ViajeRealizado,
                Id = viaje.Id,
                EstadoDelViaje = viaje.Estado,
                Origen = viaje.DireccionOrigen.Texto,
                Destino = viaje.DireccionDestino.Texto,
                Fecha = ultimaEntrega ?? viaje.FechaSalida,
                CantidadPaquetes = entregadas.Count,
                Monto = entregadas.Sum(a => a.Envio.Monto)
            };
        }

        private static HistorialItemDTO ArmarItemDeEnvio(Envio envio)
        {
            DateTime? fechaEntrega = envio.Asignaciones
                .Where(a => a.FechaEntrega is not null)
                .Select(a => a.FechaEntrega)
                .Max();

            return new HistorialItemDTO
            {
                Tipo = TipoOperacionHistorial.EnvioEntregado,
                Id = envio.Id,
                Origen = envio.DireccionRetiro.Texto,
                Destino = envio.DireccionEntrega.Texto,
                Fecha = fechaEntrega ?? envio.FechaHasta,
                Descripcion = envio.Paquete.Descripcion,
                Monto = envio.Monto
            };
        }

        // hasta es inclusivo: si el usuario elige el 15, entran las operaciones de todo el dia 15.
        private static bool EstaEnRango(DateTime fecha, DateTime? desde, DateTime? hasta)
        {
            if (desde is not null && fecha < desde.Value.Date)
            {
                return false;
            }

            if (hasta is not null && fecha >= hasta.Value.Date.AddDays(1))
            {
                return false;
            }

            return true;
        }
    }
}