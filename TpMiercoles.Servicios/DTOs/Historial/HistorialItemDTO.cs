namespace TpMiercoles.Servicios.DTOs.Historial
{
    public class HistorialItemDTO
    {
        public TipoOperacionHistorial Tipo { get; set; }

        // Id del viaje o del envio, segun el tipo. Sirve para armar el link al detalle.
        public int Id { get; set; }

        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;

        // Fecha de la operacion: salida del viaje, entrega del envio o fecha limite si se cancelo.
        public DateTime Fecha { get; set; }

        // Solo viajes: paquetes entregados en ese viaje.
        public int CantidadPaquetes { get; set; }

        // Solo envios: lo que se escribio en la descripcion del paquete.
        public string Descripcion { get; set; } = string.Empty;

        // Viaje: ganancia efectiva (suma de lo entregado). Envio: monto ofrecido.
        public decimal Monto { get; set; }
    }
}
