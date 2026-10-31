using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs
{
    public class NuevoPaqueteDTO
    {
        public double PesoKg { get; set; } = 0;
        public TamanioEnvio? Tamanio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
