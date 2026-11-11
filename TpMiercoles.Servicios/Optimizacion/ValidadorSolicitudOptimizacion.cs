using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.DTOs.Optimizacion;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class ValidadorSolicitudOptimizacion
    {
        public IReadOnlyList<string> Validar(SolicitudOptimizacionDTO solicitud)
        {
            ArgumentNullException.ThrowIfNull(solicitud);

            var errores = new List<string>();

            if (solicitud.RutaId <= 0)
            {
                errores.Add("Debe seleccionar una ruta válida.");
            }

            if (solicitud.ViajeId.HasValue
                && solicitud.ViajeId.Value <= 0)
            {
                errores.Add("El identificador del viaje debe ser mayor a cero.");
            }

            if (!double.IsFinite(solicitud.DesvioMaximoKm)
                || solicitud.DesvioMaximoKm < 0)
            {
                errores.Add(
                    "El desvío máximo debe ser un número finito mayor o igual a cero.");
            }

            return errores.AsReadOnly();
        }
    }
}
