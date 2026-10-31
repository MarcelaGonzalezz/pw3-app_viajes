using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Excepciones {
    public class RutaNoEncontradaException : Exception {
        public RutaNoEncontradaException(string mensaje) : base(mensaje) { }

        public RutaNoEncontradaException(string mensaje, Exception inner) : base(mensaje, inner) { }
    }
}
