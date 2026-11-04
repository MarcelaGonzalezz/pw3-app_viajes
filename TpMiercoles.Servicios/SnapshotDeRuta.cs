using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios
{
    // Crea el viaje como copia independiente de la ruta para una salida concreta.
    public static class SnapshotDeRuta
    {
        public static Viaje Crear(Ruta ruta, DateTime fechaSalida, DateTime fechaLlegada) => new()
        {
            RutaId = ruta.Id,
            DireccionOrigenId = ruta.DireccionOrigenId,
            DireccionDestinoId = ruta.DireccionDestinoId,
            PesoMax = ruta.PesoMax,
            CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
            FechaSalida = fechaSalida,
            FechaLlegada = fechaLlegada,
            Estado = EstadoActividadViaje.ACTIVO,
            Paradas = ruta.Paradas
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .Select(p => new Parada
                {
                    DireccionId = p.DireccionId,
                    Orden = p.Orden
                })
                .ToList()
        };
    }
}
