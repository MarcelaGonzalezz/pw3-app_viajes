using Microsoft.EntityFrameworkCore.Storage.Json;
using System;
using System.Collections.Generic;
using System.Text;

using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs.Oportunidades
{
    public class OportunidadDTO
    {
        public int EnvioId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public TamanioEnvio Tamanio { get; set; }
        public double PesoKg { get; set; }
        public decimal Pago { get; set; }
        public string RetiroDireccion { get; set; } = string.Empty;
        public string EntregaDireccion { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public string RemitenteNombre { get; set; } = string.Empty;

        public double DesvioKm { get; set; }
        public double TiempoAdicionalMin { get; set; }

        // 100 = queda de paso, 0 = no queda de paso. baja a medida que el desvio alarga el viaje
        public int Compatibilidad { get; set; }

        // NULL cuando el desvio es despreciable (no tiene sentido dividir por ~0)
        public decimal? GananciaPorKmAdicional { get; set; }
    }
}
