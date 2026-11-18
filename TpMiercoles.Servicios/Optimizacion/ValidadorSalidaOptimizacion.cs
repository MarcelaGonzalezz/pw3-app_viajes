using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class ValidadorSalidaOptimizacion
    {
        public IReadOnlyList<string> Validar(SalidaOptimizacion salida)
        {
            ArgumentNullException.ThrowIfNull(salida);

            var errores = new List<string>();

            if (salida.Ruta.Estado != EstadoRuta.ACTIVA)
            {
                errores.Add(
                    "La ruta debe estar activa para optimizar sus salidas.");
            }

            if (salida.Viaje is not null
                && salida.Viaje.Estado != EstadoActividadViaje.ACTIVO)
            {
                errores.Add(
                    "El viaje debe estar activo para poder optimizarlo.");
            }

            return errores.AsReadOnly();
        }


        public IReadOnlyList<string> ValidarCapacidad(
        SalidaOptimizacion salida,
        CompromisosSalida compromisos)
        {
            ArgumentNullException.ThrowIfNull(salida);
            ArgumentNullException.ThrowIfNull(compromisos);

            var errores = new List<string>();

            if (!double.IsFinite(salida.PesoMaximoKg)
                || salida.PesoMaximoKg <= 0)
            {
                errores.Add(
                    "El peso máximo de la salida debe ser un número "
                    + "finito mayor a cero.");
            }

            if (salida.CantidadMaximaPaquetes <= 0)
            {
                errores.Add(
                    "La cantidad máxima de paquetes debe ser mayor a cero.");
            }

            if (!double.IsFinite(compromisos.PesoComprometidoKg)
                || compromisos.PesoComprometidoKg < 0)
            {
                errores.Add(
                    "El peso comprometido de la salida no es válido.");
            }

            if (compromisos.PesoDisponibleKg < 0)
            {
                errores.Add(
                    "Los envíos comprometidos superan el peso máximo "
                    + "permitido para la salida.");
            }

            if (compromisos.CantidadDisponiblePaquetes < 0)
            {
                errores.Add(
                    "Los envíos comprometidos superan la cantidad máxima "
                    + "de paquetes permitida para la salida.");
            }

            return errores.AsReadOnly();
        }
    }
}
