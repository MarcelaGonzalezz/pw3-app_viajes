using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Servicios
{
    public interface IHistorialServicio
    {
        // Historial del usuario. Solo devuelve operaciones propias.
        //  - tipo: null trae todo.
        //  - desde / hasta: rango inclusivo de fechas, ambos opcionales.
        // El resumen (viajes, envios transportados, ganancia) respeta el rango de fechas
        // pero no el tipo, para que las tarjetas no cambien al tocar un chip.
        Task<HistorialDTO> ObtenerAsync(int usuarioId, TipoOperacionHistorial? tipo, DateTime? desde, DateTime? hasta);
    }

    public class HistorialServicio : IHistorialServicio
    {
        private readonly IHistorialRepositorio _repositorio;

        public HistorialServicio(IHistorialRepositorio repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<HistorialDTO> ObtenerAsync(
            int usuarioId, TipoOperacionHistorial? tipo, DateTime? desde, DateTime? hasta)
        {
            // Si vienen al reves los damos vuelta en vez de devolver un historial vacio.
            if (desde is not null && hasta is not null && desde > hasta)
            {
                (desde, hasta) = (hasta, desde);
            }

            List<Viaje> viajes = await _repositorio.ListarViajesRealizadosAsync(usuarioId, DateTime.Now);
            List<Envio> envios = await _repositorio.ListarEnviosFinalizadosAsync(usuarioId);

            List<HistorialItemDTO> itemsViajes = viajes.Select(ArmarItemDeViaje).ToList();
            List<HistorialItemDTO> itemsEnvios = envios.Select(ArmarItemDeEnvio).ToList();

            // 1) Filtro por fecha: afecta resumen y lista.
            List<HistorialItemDTO> enRango = itemsViajes.Concat(itemsEnvios)
                .Where(i => EstaEnRango(i.Fecha, desde, hasta))
                .ToList();

            List<HistorialItemDTO> viajesEnRango = enRango
                .Where(i => i.Tipo == TipoOperacionHistorial.ViajeRealizado).ToList();

            // 2) Filtro por tipo: afecta solo la lista.
            List<HistorialItemDTO> lista = enRango
                .Where(i => tipo is null || i.Tipo == tipo.Value)
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
                TotalEnviosCancelados = enRango.Count(i => i.Tipo == TipoOperacionHistorial.EnvioCancelado),
                Items = lista
            };
        }

        // La ganancia efectiva es solo lo que se entrego: un paquete retirado o en camino
        // todavia es ganancia potencial, y una asignacion cancelada no cuenta.
        private static HistorialItemDTO ArmarItemDeViaje(Viaje viaje)
        {
            List<Envio> entregados = viaje.Asignaciones
                .Where(a => a.Estado != EstadoViaje.CANCELADO && a.Envio.Estado == EstadoEnvio.Entregado)
                .Select(a => a.Envio)
                .ToList();

            return new HistorialItemDTO
            {
                Tipo = TipoOperacionHistorial.ViajeRealizado,
                Id = viaje.Id,
                Origen = viaje.DireccionOrigen.Texto,
                Destino = viaje.DireccionDestino.Texto,
                Fecha = viaje.FechaSalida,
                CantidadPaquetes = entregados.Count,
                Monto = entregados.Sum(e => e.Monto)
            };
        }

        private static HistorialItemDTO ArmarItemDeEnvio(Envio envio)
        {
            bool entregado = envio.Estado == EstadoEnvio.Entregado;

            DateTime? fechaEntrega = envio.Asignaciones
                .Where(a => a.FechaEntrega is not null)
                .Select(a => a.FechaEntrega)
                .Max();

            return new HistorialItemDTO
            {
                Tipo = entregado ? TipoOperacionHistorial.EnvioEntregado : TipoOperacionHistorial.EnvioCancelado,
                Id = envio.Id,
                Origen = envio.DireccionRetiro.Texto,
                Destino = envio.DireccionEntrega.Texto,
                Fecha = entregado ? (fechaEntrega ?? envio.FechaHasta) : envio.FechaHasta,
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
