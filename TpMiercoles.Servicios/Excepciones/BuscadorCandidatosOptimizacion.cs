using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.Optimizacion;

namespace TpMiercoles.Servicios.Excepciones
{
    public interface IBuscadorCandidatosOptimizacion
    {
        Task<IReadOnlyList<Envio>> BuscarAsync(
            SalidaOptimizacion salida,
            CompromisosSalida compromisos);
    }

    public sealed class BuscadorCandidatosOptimizacion: IBuscadorCandidatosOptimizacion {
        private readonly IEnvioRepositorio _envioRepositorio;

        public BuscadorCandidatosOptimizacion(IEnvioRepositorio envioRepositorio) {
            _envioRepositorio = envioRepositorio;
        }

        public async Task<IReadOnlyList<Envio>> BuscarAsync(SalidaOptimizacion salida, CompromisosSalida compromisos)
        {
            ArgumentNullException.ThrowIfNull(salida);
            ArgumentNullException.ThrowIfNull(compromisos);

            if (compromisos.CantidadDisponiblePaquetes <= 0
                || compromisos.PesoDisponibleKg <= 0)
            {
                return Array.Empty<Envio>();
            }

            var disponibles =
                await _envioRepositorio.ListarDisponiblesAsync(
                    salida.MomentoEvaluacion,
                    salida.Ruta.UsuarioId);

            var candidatos = disponibles
                .Where(envio =>
                    double.IsFinite(envio.Paquete.PesoKg)
                    && envio.Paquete.PesoKg > 0
                    && envio.Paquete.PesoKg <= compromisos.PesoDisponibleKg
                    && envio.Monto > 0)
                .ToList();

            return candidatos.AsReadOnly();
        }
    }
}
