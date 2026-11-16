using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Servicios.DTOs.Historial;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Optimizacion;

namespace TpMiercoles.Servicios
{
    public interface IGananciasServicio
    {
        Task<GananciasMensualDTO> CalcularAsync(
               int conductorId, int anio, int mes,
               CriterioOptimizacion criterio,
               CancellationToken ct = default);
    }

    public sealed class GananciasServicio : IGananciasServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        private readonly IRutaRepositorio _rutas;
        private readonly IOptimizadorServicio _optimizador;
        private readonly IHistorialServicio _historial;
        private readonly TimeProvider _reloj;

        public GananciasServicio(
            IRutaRepositorio rutas,
            IOptimizadorServicio optimizador,
            IHistorialServicio historial,
            TimeProvider reloj)
        {
            _rutas = rutas;
            _optimizador = optimizador;
            _historial = historial;
            _reloj = reloj;
        }

        public async Task<GananciasMensualDTO> CalcularAsync(int conductorId, int anio, int mes, CriterioOptimizacion criterio, CancellationToken ct = default)
        {
            (DateTime inicioMes, DateTime finMes) = LimitesDelMes(anio, mes);

            DateTime ahora = Ahora();
            DateTime potencialDesde = ahora > inicioMes ? ahora : inicioMes;
            DateTime efectivaHasta = ahora < inicioMes ? inicioMes : ahora > finMes ? finMes : ahora;

            HistorialDTO historial =
                await _historial.ObtenerAsync(conductorId, null, inicioMes, finMes);

            List<Ruta> rutas =
                await _rutas.ObtenerRutasActivasDeUnUsuarioAsync(conductorId);

            List<(Ruta Ruta, DateTime Salida)> programadas = rutas
                .SelectMany(ruta => CalendarioDeSalidas
                    .Todas(ruta, potencialDesde, finMes)
                    .Select(salida => (Ruta: ruta, Salida: salida)))
                .OrderBy(par => par.Salida)
                .ToList();

            HashSet<int> enviosUsados = [];
            List<SalidaEstimadaDTO> salidas = [];
            int descartadas = 0;

            foreach ((Ruta ruta, DateTime salida) in programadas)
            {
                ct.ThrowIfCancellationRequested();

                SalidaEstimadaDTO? estimada = await EstimarSalidaAsync(
                    ruta, salida, conductorId, criterio, enviosUsados, ct);

                if (estimada is null)
                {
                    descartadas++;
                    continue;
                }

                foreach (int envioId in estimada.EnviosIds)
                {
                    enviosUsados.Add(envioId);
                }

                salidas.Add(estimada);
            }

            return new GananciasMensualDTO
            {
                Anio = anio,
                Mes = mes,
                Criterio = criterio,

                EfectivaDesde = inicioMes,
                EfectivaHasta = efectivaHasta,
                PotencialDesde = potencialDesde,
                PotencialHasta = finMes,

                GananciaEfectiva = historial.GananciaEfectiva,
                GananciaComprometida = salidas.Sum(s => s.GananciaComprometida),
                GananciaPotencial = salidas.Sum(s => s.GananciaPotencial),

                ViajesAnalizados = salidas.Count,
                PaquetesPosibles = salidas.Sum(s => s.CantidadPaquetes),
                EnviosEntregados = historial.EnviosTransportados,
                DesvioTotalKm = salidas.Sum(s => s.DesvioKm),
                PesoTotalKg = salidas.Sum(s => s.PesoKg),

                Semanas = AgruparPorSemana(salidas, potencialDesde, finMes),
                Salidas = salidas,
                MejorRecorrido = salidas
                    .OrderByDescending(s => s.GananciaPotencial)
                    .FirstOrDefault(s => s.TuvoEnvios),

                SalidasDescartadas = descartadas,
                SalidasSinEnviosCompatibles = salidas.Count(s => !s.TuvoEnvios)
            };
        }

        private DateTime Ahora() =>
            TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), ZonaArgentina).DateTime;

        private static (DateTime inicio, DateTime fin) LimitesDelMes(int anio, int mes) =>
            throw new NotImplementedException();

        private async Task<SalidaEstimadaDTO?> EstimarSalidaAsync(
            Ruta ruta,
            DateTime salida,
            int conductorId,
            CriterioOptimizacion criterio,
            IReadOnlyCollection<int> yaUsados,
            CancellationToken ct) =>
            throw new NotImplementedException();

        private static PropuestaOptimizacionDTO? ElegirPropuesta(
            ResultadoOptimizacionDTO resultado,
            CriterioOptimizacion criterio) =>
            throw new NotImplementedException();

        private static List<SemanaDeGananciasDTO> AgruparPorSemana(
            IReadOnlyList<SalidaEstimadaDTO> salidas,
            DateTime desde,
            DateTime hasta) =>
            throw new NotImplementedException();
    }
}