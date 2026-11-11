using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class CalculadorCompromisosSalida
    {
        public CompromisosSalida Calcular(SalidaOptimizacion salida)
    {
        ArgumentNullException.ThrowIfNull(salida);

        var asignacionesVigentes = salida.Asignaciones
            .Where(asignacion =>
                asignacion.Estado != EstadoViaje.CANCELADO)
            .ToList();

        if (asignacionesVigentes.Any(asignacion =>
            asignacion.Envio is null))
        {
            throw new InvalidOperationException(
                "Se deben cargar los envíos de las asignaciones "
                + "para calcular los compromisos de la salida.");
        }

        var envios = asignacionesVigentes
            .Select(asignacion => asignacion.Envio)
            .ToList();

        double pesoComprometidoKg = envios.Sum(
            envio => envio.Paquete.PesoKg);

        decimal ingresoComprometido = envios.Sum(
            envio => envio.Monto);

        double pesoDisponibleKg =
            salida.PesoMaximoKg - pesoComprometidoKg;

        int cantidadDisponiblePaquetes =
            salida.CantidadMaximaPaquetes - envios.Count;

            return new CompromisosSalida
            {
                Envios = envios.AsReadOnly(),
                PesoComprometidoKg = pesoComprometidoKg,
                PesoDisponibleKg = pesoDisponibleKg,
                CantidadDisponiblePaquetes = cantidadDisponiblePaquetes,
                IngresoComprometido = ingresoComprometido
            };
        }
}
}
