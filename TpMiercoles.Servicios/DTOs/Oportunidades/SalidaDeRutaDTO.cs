namespace TpMiercoles.Servicios.DTOs.Oportunidades
{
    public class SalidaDeRutaDTO
    {
        public int RutaId { get; set; }
        public int? ViajeId { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public DateTime FechaLlegada { get; set; }
    }
}
