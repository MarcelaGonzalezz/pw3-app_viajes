using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    public record Coordenada(decimal Latitud, decimal Longitud);
    //punto en el mapa. Se utiliza para evitar el uso de la entidad direccion y que la abstraccion de la ruta no quede atada a la BD

    //clase record: clase inmutable para guardar datos. ya tiene constructor, propiedades y comparacion por valor
}
