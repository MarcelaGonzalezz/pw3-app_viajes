using System.Globalization;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Web.Models
{
    public class HistorialItemViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public TipoOperacionHistorial Tipo { get; set; }
        public int Id { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int CantidadPaquetes { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }

        public bool EsViaje => Tipo == TipoOperacionHistorial.ViajeRealizado;

        public string TipoTexto => Tipo switch
        {
            TipoOperacionHistorial.ViajeRealizado => "Viaje realizado",
            TipoOperacionHistorial.EnvioEntregado => "Envío entregado",
            _ => Tipo.ToString()
        };

        // Clase de color reutilizada de detalle.css: todo lo que figura en el historial se completó.
        public string ChipClase => "vd-chip--confirmado";

        // Viaje:  "octubre 2026 · 3 paquetes"
        // Envio:  "Enviaste: Repuesto · 12/10/2026"
        public string Subtitulo => EsViaje
            ? $"{Fecha.ToString("MMMM yyyy", Argentina)} · {CantidadPaquetes} {(CantidadPaquetes == 1 ? "paquete" : "paquetes")}"
            : $"Enviaste: {Descripcion} · {Fecha:dd/MM/yyyy}";

        public string MontoTexto => Monto.ToString("C0", Argentina);
    }
}