namespace TpMiercoles.Servicios.DTOs
{
    // Agrega al listado los datos de contacto del remitente, el recorrido del
    // viaje y las marcas de tiempo de cada paso del transporte.
    public class TransporteDetalleDTO : TransporteListadoDTO
    {
        public string? RemitenteEmail { get; set; }

        public string ViajeOrigen { get; set; } = string.Empty;
        public string ViajeDestino { get; set; } = string.Empty;
        public DateTime? FechaLlegadaViaje { get; set; }

        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }
}
