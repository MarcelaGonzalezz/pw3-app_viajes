using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    // Agrega al listado el contacto del remitente, el viaje y las fechas de cada paso.
    public class TransporteDetalleViewModel : TransporteListadoViewModel
    {
        public string? RemitenteEmail { get; set; }

        public string ViajeOrigen { get; set; } = string.Empty;
        public string ViajeDestino { get; set; } = string.Empty;
        public DateTime? FechaLlegadaViaje { get; set; }

        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public string EstadoBadgeClass => Estado switch
        {
            EstadoEnvio.ConductorAsignado => "vd-estado--asignado",
            EstadoEnvio.Retirado => "vd-estado--retirado",
            EstadoEnvio.EnCamino => "vd-estado--encamino",
            EstadoEnvio.Entregado => "vd-estado--activo",
            _ => "vd-estado--inactivo"
        };

        // Linea de tiempo del transporte. Cada paso esta "hecho" si el envio ya llego
        // a ese estado o paso de largo, y el "actual" es el estado en el que esta ahora.
        public List<PasoTransporteViewModel> ObtenerPasos()
        {
            // Los estados del transporte van del 1 (asignado) al 4 (entregado).
            // Un envio cancelado no avanza en la linea de tiempo.
            int nivel = Estado == EstadoEnvio.Cancelado ? 0 : (int)Estado;

            return
            [
                Paso("Aceptado", FechaAceptacion, 1, nivel),
                Paso("Retirado", FechaRetiro, 2, nivel),
                Paso("En camino", null, 3, nivel),
                Paso("Entregado", FechaEntrega, 4, nivel)
            ];
        }

        private static PasoTransporteViewModel Paso(string texto, DateTime? fecha, int nivelDelPaso, int nivelActual)
            => new()
            {
                Texto = texto,
                Fecha = fecha,
                Hecho = nivelActual >= nivelDelPaso,
                Actual = nivelActual == nivelDelPaso
            };
    }

    public class PasoTransporteViewModel
    {
        public string Texto { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public bool Hecho { get; set; }
        public bool Actual { get; set; }
    }
}
