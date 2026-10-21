using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Direccion
    {
        public int Id { get; set; }
        public string Texto { get; set; } = null!;
        [Precision(12, 9)]
        public decimal Latitud { get; set; }
        [Precision(12, 9)]
        public decimal Longitud { get; set; }
    }
}
