using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades.Excepciones
{
    public class EmailExistenteException :  Exception
    {
        public EmailExistenteException(string mensaje, Exception? inner = null) : base(mensaje, inner)
        {

        }
    }
}
