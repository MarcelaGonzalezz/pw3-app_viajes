using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;


namespace TpMiercoles.Servicios.DTOs {
    public class EnvioDTO {
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public double PesoKg { get; set; }
        public TamanioEnvio Tamanio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }
}
