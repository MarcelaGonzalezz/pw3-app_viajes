using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Oportunidades
{
    public class OportunidadDetalleDTO : OportunidadDTO
    {
        public int RutaId { get; set; }

        // null si todavia no se acepto ningun envio para esta salida
        public int? ViajeId { get; set; }

        public string RutaOrigen { get; set; } = string.Empty;
        public string RutaDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }

        public double DistanciaHabitualKm { get; set; }
        public double DistanciaConEnvioKm { get; set; }
        public double TiempoHabitualMin { get; set; }
        public double TiempoConEnvioMin { get; set; }

        // cantidad de paquetes restante
        public int CapacidadRestante { get; set; }
        public double PesoRestanteKg { get; set; }

        // por que no se puede aceptar (vacio = se puede aceptar)
        public List<string> Motivos { get; set; } = [];
        public bool EsAceptable => Motivos.Count == 0;
    }
}
