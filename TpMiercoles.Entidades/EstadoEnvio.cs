using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades {
    public enum EstadoEnvio {
        BuscandoConductor = 0,
        ConductorAsignado = 1,
        Retirado = 2,
        EnCamino = 3,
        Entregado = 4,
        Cancelado = 5
    }
}
