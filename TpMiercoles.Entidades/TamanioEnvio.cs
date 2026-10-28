using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TpMiercoles.Entidades {
    public enum TamanioEnvio {
        [Display(Name = "Pequeño")]
        Pequenio = 0,
        [Display(Name = "Mediano")]
        Mediano = 1,
        [Display(Name = "Grande")]
        Grande = 2
    }
}
