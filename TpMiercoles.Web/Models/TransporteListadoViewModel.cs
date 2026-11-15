using System.Globalization;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    // Un paquete que el conductor acepto llevar. El estado es el del ENVIO.
    public class TransporteListadoViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public int AsignacionId { get; set; }
        public int ViajeId { get; set; }
        public int EnvioId { get; set; }

        public string Descripcion { get; set; } = string.Empty;
        public double PesoKg { get; set; }
        public TamanioEnvio Tamanio { get; set; }

        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;

        public decimal Ganancia { get; set; }
        public EstadoEnvio Estado { get; set; }

        public string RemitenteNombre { get; set; } = string.Empty;
        public DateTime FechaSalidaViaje { get; set; }

        // Siguiente paso del ciclo; null si ya se entrego.
        public EstadoEnvio? ProximoEstado { get; set; }

        public string Titulo => string.IsNullOrWhiteSpace(Descripcion) ? $"Envío #{EnvioId}" : Descripcion;

        public string EstadoTexto => Estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.EnCamino => "En camino",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            _ => Estado.ToString()
        };

        // Clases de color reutilizadas de detalle.css (vd-chip--*).
        public string EstadoClase => Estado switch
        {
            EstadoEnvio.ConductorAsignado => "vd-chip--asignado",
            EstadoEnvio.Retirado => "vd-chip--retirado",
            EstadoEnvio.EnCamino => "vd-chip--encamino",
            EstadoEnvio.Entregado => "vd-chip--confirmado",
            EstadoEnvio.Cancelado => "vd-chip--cancelado",
            _ => "vd-chip--pendiente"
        };

        public bool EnCurso => Estado is not (EstadoEnvio.Entregado or EstadoEnvio.Cancelado);

        public string TamanioTexto => Tamanio switch
        {
            TamanioEnvio.Pequenio => "Pequeño",
            TamanioEnvio.Mediano => "Mediano",
            TamanioEnvio.Grande => "Grande",
            _ => Tamanio.ToString()
        };

        public string GananciaTexto => Ganancia.ToString("C0", Argentina);

        // ---- Boton de la accion siguiente ----

        public bool TieneAccion => ProximoEstado is not null;

        public string AccionTexto => ProximoEstado switch
        {
            EstadoEnvio.Retirado => "Marcar retirado",
            EstadoEnvio.EnCamino => "Marcar en camino",
            EstadoEnvio.Entregado => "Marcar entregado",
            _ => string.Empty
        };

        // Nombre de la accion del TransportesController que da ese paso.
        public string AccionNombre => ProximoEstado switch
        {
            EstadoEnvio.Retirado => "Retirar",
            EstadoEnvio.EnCamino => "MarcarEnCamino",
            EstadoEnvio.Entregado => "Entregar",
            _ => string.Empty
        };

        // Entregar cierra el transporte y no se puede deshacer, asi que pide confirmacion.
        public string? AccionConfirmacionJs => null;
    }
}
