using TpMiercoles.Entidades;

namespace TpMiercoles.Tests.Helpers
{
    public static class RutaTestData
    {
        public static Direccion CrearDireccion(int id, string texto)
            => new() { Id = id, Texto = texto, Latitud = -34.6m, Longitud = -58.5m };

        public static Parada CrearParada(int id, string texto, int orden)
            => new()
            {
                Id = id,
                DireccionId = id,
                Direccion = CrearDireccion(id, texto),
                Orden = orden
            };

        public static Ruta CrearRuta(
            int id = 1,
            int usuarioId = 1,
            EstadoRuta estado = EstadoRuta.ACTIVA,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            TimeOnly? horaSalida = null,
            TimeOnly? horaLlegada = null,
            IEnumerable<DayOfWeek>? dias = null,
            IEnumerable<Parada>? paradas = null,
            double pesoMax = 25,
            int capacidadMaxPaquetes = 4)
        {
            Direccion origen = CrearDireccion(1000 + id, "Origen de ruta");
            Direccion destino = CrearDireccion(2000 + id, "Destino de ruta");

            return new Ruta
            {
                Id = id,
                UsuarioId = usuarioId,
                DireccionOrigenId = origen.Id,
                DireccionOrigen = origen,
                DireccionDestinoId = destino.Id,
                DireccionDestino = destino,
                PesoMax = pesoMax,
                CapacidadMaxPaquetes = capacidadMaxPaquetes,
                FechaInicio = fechaInicio ?? new DateTime(2026, 11, 1),
                FechaFin = fechaFin,
                HoraSalida = horaSalida ?? new TimeOnly(8, 0),
                HoraLlegada = horaLlegada ?? new TimeOnly(9, 30),
                Estado = estado,
                RutaDias = (dias ?? [])
                    .Select((dia, indice) => new RutaDia { Id = indice + 1, DiaSemana = dia })
                    .ToList(),
                Paradas = paradas?.ToList() ?? []
            };
        }
    }
}
