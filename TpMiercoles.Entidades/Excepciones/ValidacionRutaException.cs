using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades.Excepciones
{
    public class ValidacionRutaException : Exception
    {
        public ValidacionRutaException(string messaje) : base(messaje)
        {

        }
    }
}
