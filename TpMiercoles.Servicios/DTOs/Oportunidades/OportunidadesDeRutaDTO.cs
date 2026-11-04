using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Oportunidades
{
    public class OportunidadesDeRutaDTO
    {
        public int RutaId { get; set; }

        // null si todavia no se acepto ningun envio para esta salida
        public int? ViajeId { get; set; }

        public string RutaOrigen { get; set; } = string.Empty;
        public string RutaDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public int CapacidadRestante { get; set; }
        public double PesoRestanteKg { get; set; }
        public double DistanciaHabitualKm { get; set; }
        public double TiempoHabitualMin { get; set; }

        // si la salida no puede sumar paquetes (ruta inactiva, ya salio, esta llena) se explica aca
        // y no se buscan envios
        public List<string> AvisosDeLaSalida { get; set; } = [];
        public List<OportunidadDTO> Oportunidades { get; set; } = [];
    }
}
