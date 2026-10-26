using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.DTOs.Direccion;

namespace TpMiercoles.Servicios.DTOs.Ruta
{
    public class ParadaDTO
    {
        public DireccionDTO Direccion { get; set; } = null!;
        public int Orden { get; set; }
    }
}
