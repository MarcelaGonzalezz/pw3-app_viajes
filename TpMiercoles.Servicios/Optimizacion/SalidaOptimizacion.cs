using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class SalidaOptimizacion
    {
        public required Ruta Ruta { get; init; }

        public Viaje? Viaje { get; init; }

        public required DateTime FechaSalidaProgramada { get; init; }

        public required DateTime MomentoEvaluacion { get; init; }

        public int RutaId => Ruta.Id;

        public int? ViajeId => Viaje?.Id;

        public Direccion Origen =>
            Viaje?.DireccionOrigen ?? Ruta.DireccionOrigen;

        public Direccion Destino =>
            Viaje?.DireccionDestino ?? Ruta.DireccionDestino;

        public IEnumerable<Parada> Paradas =>
            (Viaje?.Paradas ?? Ruta.Paradas)
                .OrderBy(parada => parada.Orden)
                .ThenBy(parada => parada.Id);

        public double PesoMaximoKg =>
            Viaje?.PesoMax ?? Ruta.PesoMax;

        public int CantidadMaximaPaquetes =>
            Viaje?.CapacidadMaxPaquetes ?? Ruta.CapacidadMaxPaquetes;

        public IEnumerable<Asignacion> Asignaciones =>
            Viaje?.Asignaciones ?? Enumerable.Empty<Asignacion>();
    }
}
