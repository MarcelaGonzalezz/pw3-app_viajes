using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Oportunidades
{
    public class OportunidadesDelViajeDTO
    {
        public int ViajeId { get; set; }
        public string ViajeOrigen { get; set; } = string.Empty;
        public string ViajeDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public int CapacidadRestante { get; set; }
        public double PesoRestanteKg { get; set; }
        public double DistanciaHabitualKm { get; set; }
        public double TiempoHabitualMin { get; set; }

        // si el viaje no puede sumar paquetes (inactivo, ya salio, esta lleno) se explica aca
        // y no se buscan envios
        public List<string> AvisosDelViaje { get; set; } = [];
        public List<OportunidadDTO> Oportunidades { get; set; } = [];
    }
}
