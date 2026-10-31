namespace TpMiercoles.Servicios.DTOs.Historial
{
    public class HistorialDTO
    {
        // ---- Resumen del periodo filtrado (no depende del filtro por tipo) ----
        public int ViajesRealizados { get; set; }
        public int EnviosTransportados { get; set; }
        public decimal GananciaEfectiva { get; set; }

        // ---- Cantidades para los chips de filtro ----
        public int TotalOperaciones { get; set; }
        public int TotalEnviosEntregados { get; set; }
        public int TotalEnviosCancelados { get; set; }

        public List<HistorialItemDTO> Items { get; set; } = [];
    }
}
