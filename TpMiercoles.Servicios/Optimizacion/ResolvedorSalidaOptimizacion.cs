using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Oportunidades;

namespace TpMiercoles.Servicios.Optimizacion
{
    public interface IResolvedorSalidaOptimizacion
    {
        Task<SalidaOptimizacion?> ResolverViajeAsync(
            int rutaId,
            int viajeId,
            int conductorId);
        Task<SalidaOptimizacion?> ResolverProximaSalidaAsync(
    int rutaId,
    int conductorId);
        Task<List<SalidaDeRutaDTO>> ListarSalidasAsync(
    int conductorId,
    CancellationToken ct = default);
    }

    public sealed class ResolvedorSalidaOptimizacion : IResolvedorSalidaOptimizacion
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById(
                "America/Argentina/Buenos_Aires");

        private readonly IViajeRepositorio _viajeRepositorio;
        private readonly TimeProvider _reloj;
        private readonly IRutaRepositorio _rutaRepositorio;

        public ResolvedorSalidaOptimizacion(IViajeRepositorio viajeRepositorio, TimeProvider reloj, IRutaRepositorio rutaRepositorio){
            _viajeRepositorio = viajeRepositorio;
            _reloj = reloj;
            _rutaRepositorio = rutaRepositorio;
        }

        public async Task<SalidaOptimizacion?> ResolverViajeAsync(int rutaId, int viajeId, int conductorId)
        {
            var viaje = await _viajeRepositorio.ObtenerDetalleAsync(viajeId);

            if (viaje is null)
            {
                return null;
            }

            if (viaje.RutaId != rutaId
                || viaje.Ruta.UsuarioId != conductorId)
            {
                return null;
            }

            DateTime ahora = TimeZoneInfo.ConvertTime(
                _reloj.GetUtcNow(),
                ZonaArgentina).DateTime;

            DateTime momentoEvaluacion =
                ReferenciaTemporalOptimizacion.DeterminarMomentoEvaluacion(
                    viaje.FechaSalida,
                    ahora);

            return new SalidaOptimizacion
            {
                Ruta = viaje.Ruta,
                Viaje = viaje,
                FechaSalidaProgramada = viaje.FechaSalida,
                MomentoEvaluacion = momentoEvaluacion
            };
        }
        public async Task<SalidaOptimizacion?> ResolverProximaSalidaAsync(int rutaId, int conductorId){
            var ruta = await _rutaRepositorio.ObtenerPorIdAsync(rutaId);

            if (ruta is null || ruta.UsuarioId != conductorId)
            {
                return null;
            }

            DateTime ahora = TimeZoneInfo.ConvertTime(
                _reloj.GetUtcNow(),
                ZonaArgentina).DateTime;

            DateTime? fechaSalida =
                CalendarioDeSalidas.Proxima(ruta, ahora);

            if (fechaSalida is null)
            {
                return null;
            }

            var viaje = await _viajeRepositorio.ObtenerPorRutaYFechaAsync(
                ruta.Id,
                fechaSalida.Value);

            if (viaje is not null)
            {
                return await ResolverViajeAsync(
                    ruta.Id,
                    viaje.Id,
                    conductorId);
            }

            DateTime momentoEvaluacion =
                ReferenciaTemporalOptimizacion.DeterminarMomentoEvaluacion(
                    fechaSalida.Value,
                    ahora);

            return new SalidaOptimizacion
            {
                Ruta = ruta,
                Viaje = null,
                FechaSalidaProgramada = fechaSalida.Value,
                MomentoEvaluacion = momentoEvaluacion
            };
        }
        public async Task<List<SalidaDeRutaDTO>> ListarSalidasAsync(
    int conductorId,
    CancellationToken ct = default)
        {
            if (conductorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conductorId));
            }

            ct.ThrowIfCancellationRequested();

            var rutas = await _rutaRepositorio
                .ObtenerRutasActivasDeUnUsuarioAsync(conductorId);

            ct.ThrowIfCancellationRequested();

            var viajes = await _viajeRepositorio.ListarPorUsuarioAsync(
                conductorId,
                EstadoActividadViaje.ACTIVO);

            ct.ThrowIfCancellationRequested();

            DateTime ahora = TimeZoneInfo.ConvertTime(
                _reloj.GetUtcNow(),
                ZonaArgentina).DateTime;

            var rutasPorId = rutas.ToDictionary(ruta => ruta.Id);

            var salidas = new List<SalidaDeRutaDTO>();

            var salidasIncluidas = new HashSet<(
                int RutaId,
                DateTime FechaSalida)>();

            foreach (var viaje in viajes)
            {
                ct.ThrowIfCancellationRequested();

                if (!rutasPorId.TryGetValue(viaje.RutaId, out var ruta))
                {
                    continue;
                }

                salidas.Add(new SalidaDeRutaDTO
                {
                    RutaId = viaje.RutaId,
                    ViajeId = viaje.Id,
                    Origen = viaje.DireccionOrigen.Texto,
                    Destino = viaje.DireccionDestino.Texto,
                    FechaSalida = viaje.FechaSalida,
                    FechaLlegada = viaje.FechaLlegada
                        ?? viaje.FechaSalida + CalendarioDeSalidas.Duracion(ruta)
                });

                salidasIncluidas.Add((
                    viaje.RutaId,
                    viaje.FechaSalida));
            }

            foreach (var ruta in rutas)
            {
                ct.ThrowIfCancellationRequested();

                DateTime? proximaSalida =
                    CalendarioDeSalidas.Proxima(ruta, ahora);

                if (proximaSalida is null)
                {
                    continue;
                }

                var clave = (
                    RutaId: ruta.Id,
                    FechaSalida: proximaSalida.Value);

                if (salidasIncluidas.Contains(clave))
                {
                    continue;
                }

                // Puede existir un viaje inactivo para esa salida.
                // No debemos ofrecerlo como si todavía no estuviera creado.
                var viajeExistente =
                    await _viajeRepositorio.ObtenerPorRutaYFechaAsync(
                        ruta.Id,
                        proximaSalida.Value);

                ct.ThrowIfCancellationRequested();

                if (viajeExistente is not null
                    && viajeExistente.Estado != EstadoActividadViaje.ACTIVO)
                {
                    continue;
                }

                salidas.Add(new SalidaDeRutaDTO
                {
                    RutaId = ruta.Id,
                    ViajeId = viajeExistente?.Id,

                    Origen = viajeExistente?.DireccionOrigen.Texto
                        ?? ruta.DireccionOrigen.Texto,

                    Destino = viajeExistente?.DireccionDestino.Texto
                        ?? ruta.DireccionDestino.Texto,

                    FechaSalida = proximaSalida.Value,

                    FechaLlegada = viajeExistente?.FechaLlegada
                        ?? proximaSalida.Value
                            + CalendarioDeSalidas.Duracion(ruta)
                });

                salidasIncluidas.Add(clave);
            }

            return salidas
                .OrderBy(salida => salida.FechaSalida)
                .ThenBy(salida => salida.RutaId)
                .ThenBy(salida => salida.ViajeId)
                .ToList();
        }
    }



}
