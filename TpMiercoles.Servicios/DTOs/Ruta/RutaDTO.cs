using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs.Direccion;

namespace TpMiercoles.Servicios.DTOs.Ruta
{
    public class RutaDTO
    {
        public int Id { get; set; }
        public UsuarioDTO Usuario { get; set; } = null!;
        public DireccionDTO DireccionOrigen { get; set; } = null!;
        public DireccionDTO DireccionDestino { get; set; } = null!;
        public double PesoMax { get; set; }
        public int CapacidadMaxPaquetes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<RutaDiaDTO> RutaDias { get; set; } = [];
        public List<ParadaDTO> Paradas { get; set; } = [];
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly HoraLlegada { get; set; }
        public EstadoRuta Estado { get; set; } = EstadoRuta.ACTIVA;
    }
}
