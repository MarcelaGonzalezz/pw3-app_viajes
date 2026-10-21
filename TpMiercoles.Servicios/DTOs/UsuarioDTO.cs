using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs
{
    public class UsuarioDTO
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string ContraseniaPlana { get; set; }
        public string Email { get; set; }
    }
}
