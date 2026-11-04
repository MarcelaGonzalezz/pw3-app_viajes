using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios
{
    // Calcula las salidas concretas de una ruta combinando sus dias, su horario y su vigencia.
    internal static class CalendarioDeSalidas
    {
        // Proxima salida estrictamente futura. null si la ruta no tiene salidas dentro de su vigencia.
        public static DateTime? Proxima(Ruta ruta, DateTime ahora)
        {
            DateTime desde = ruta.FechaInicio.Date > ahora.Date
                ? ruta.FechaInicio.Date
                : ahora.Date;

            for (int i = 0; i <= 7; i++)
            {
                DateTime dia = desde.AddDays(i);

                if (ruta.FechaFin is not null && dia > ruta.FechaFin.Value.Date)
                {
                    break;
                }

                if (!ruta.RutaDias.Any(d => d.DiaSemana == dia.DayOfWeek))
                {
                    continue;
                }

                DateTime salida = dia + ruta.HoraSalida.ToTimeSpan();

                if (salida > ahora)
                {
                    return salida;
                }
            }

            return null;
        }

        // Salida de hoy si ya llego la hora. Se usa para materializar el viaje automaticamente.
        public static DateTime? YaSalioHoy(Ruta ruta, DateTime ahora)
        {
            if (ruta.FechaInicio.Date > ahora.Date)
            {
                return null;
            }

            if (ruta.FechaFin is not null && ahora.Date > ruta.FechaFin.Value.Date)
            {
                return null;
            }

            if (!ruta.RutaDias.Any(d => d.DiaSemana == ahora.DayOfWeek))
            {
                return null;
            }

            DateTime salida = ahora.Date + ruta.HoraSalida.ToTimeSpan();

            return salida <= ahora ? salida : null;
        }

        public static TimeSpan Duracion(Ruta ruta)
            => ruta.HoraLlegada.ToTimeSpan() - ruta.HoraSalida.ToTimeSpan();
    }
}
