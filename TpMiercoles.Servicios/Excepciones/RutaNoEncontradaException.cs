using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Excepciones {
    public class RutaNoEncontradaException : Exception {
        public RutaNoEncontradaException(string mensaje) : base(mensaje) { }

        public RutaNoEncontradaException(string mensaje, Exception inner) : base(mensaje, inner) { }

        // Código HTTP con el que respondió el servicio de rutas, si la falla vino de una respuesta.
        // Permite distinguir, por ejemplo, la cuota agotada (403) del exceso de pedidos por minuto (429).
        public int? CodigoHttp { get; init; }
    }
}
