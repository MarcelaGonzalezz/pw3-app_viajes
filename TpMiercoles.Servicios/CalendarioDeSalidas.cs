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

        // True si la ruta realmente sale en ese momento: el dia de la semana esta en
        // la recurrencia, la fecha cae dentro de la vigencia y la hora es la de salida.
        public static bool EsSalidaValida(Ruta ruta, DateTime fechaSalida)
        {
            if (fechaSalida.Date < ruta.FechaInicio.Date)
            {
                return false;
            }

            if (ruta.FechaFin is not null && fechaSalida.Date > ruta.FechaFin.Value.Date)
            {
                return false;
            }

            if (!ruta.RutaDias.Any(d => d.DiaSemana == fechaSalida.DayOfWeek))
            {
                return false;
            }

            return fechaSalida.TimeOfDay == ruta.HoraSalida.ToTimeSpan();
        }

        // Todas las salidas de la ruta entre desde y hasta, ambas inclusive.
        public static List<DateTime> Todas(Ruta ruta, DateTime desde, DateTime hasta)
        {
            List<DateTime> salidas = new();
            DateTime primerDia = ruta.FechaInicio.Date > desde.Date ? ruta.FechaInicio.Date : desde.Date;
            DateTime ultimoDia = ruta.FechaFin is not null && ruta.FechaFin.Value.Date < hasta.Date ? ruta.FechaFin.Value.Date : hasta.Date;

            for (DateTime dia = primerDia; dia <= ultimoDia; dia = dia.AddDays(1)) //Se usa addDays porque conoce el calendario
            {
                if (!ruta.RutaDias.Any(d => d.DiaSemana== dia.DayOfWeek))
                {
                    continue;
                }

                DateTime salida = dia + ruta.HoraSalida.ToTimeSpan();

                if (salida >= desde && salida<= hasta)
                {
                    salidas.Add(salida);
                }
            }
            return salidas;
        }

        public static TimeSpan Duracion(Ruta ruta)
            => ruta.HoraLlegada.ToTimeSpan() - ruta.HoraSalida.ToTimeSpan();
    }
}
