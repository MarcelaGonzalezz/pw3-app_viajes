using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.Viajes
{
    public static class Vencimiento
    {
        // Un viaje vence cuando ya paso su hora de llegada.
        // Si no tiene hora de llegada cargada, se usa la de salida.
        public static bool EstaVencido(Viaje viaje, DateTime ahora)
        {
            DateTime fin = viaje.FechaSalida;

            if (viaje.FechaLlegada != null)
            {
                fin = viaje.FechaLlegada.Value;
            }

            return fin < ahora;
        }

        // Una ruta vence cuando ya paso su fecha "hasta", a la hora de llegada.
        // Si no tiene fecha "hasta", no vence nunca.
        public static bool EstaVencida(Ruta ruta, DateTime ahora)
        {
            if (ruta.FechaFin == null)
            {
                return false;
            }

            DateTime fin = ruta.FechaFin.Value.Date
                .AddHours(ruta.HoraLlegada.Hour)
                .AddMinutes(ruta.HoraLlegada.Minute);

            return fin < ahora;
        }
    }
}
