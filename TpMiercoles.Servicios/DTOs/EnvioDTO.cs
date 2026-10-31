using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs.Direccion;


namespace TpMiercoles.Servicios.DTOs {
    public class EnvioDTO {
        public DireccionDTO Origen { get; set; } = new();
        public DireccionDTO Destino { get; set; } = new ();
        public DateTime FechaHasta { get; set; }
        public NuevoPaqueteDTO Paquete { get; set; }
        public decimal Monto { get; set; }
    }
}
