using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TpMiercoles.Entidades {

    [ComplexType]
    public class Paquete {
        public double PesoKg { get; set; }
        public TamanioEnvio Tamanio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
