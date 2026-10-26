
using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Direccion
{
    public class DireccionDTO
    {
        public string Texto { get; set; } = null!;
  
        public decimal Latitud { get; set; }
        
        public decimal Longitud { get; set; }
    }
}
