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
               double? desvioMaximoKm = null,
               CancellationToken ct = default);
    }

    public sealed class GananciasServicio : IGananciasServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        private const double SinLimite = double.MaxValue;

        private readonly IRutaRepositorio _rutas;
        private readonly IOptimizadorServicio _optimizador;
        private readonly IHistorialServicio _historial;
        private readonly IEnvioRepositorio _envios;
        private readonly TimeProvider _reloj;

        public GananciasServicio(
            IRutaRepositorio rutas,
            IOptimizadorServicio optimizador,
            IHistorialServicio historial,
            IEnvioRepositorio envios,
            TimeProvider reloj)
        {
            _rutas = rutas;
            _optimizador = optimizador;
            _historial = historial;
            _envios = envios;
            _reloj = reloj;
        }

        public async Task<GananciasMensualDTO> CalcularAsync(int conductorId, int anio, int mes, CriterioOptimizacion criterio,
            double? desvioMaximoKm = null, CancellationToken ct = default)
        {
            (DateTime inicioMes, DateTime finMes) = LimitesDelMes(anio, mes);

            DateTime ahora = Ahora();
            DateTime potencialDesde = ahora > inicioMes ? ahora : inicioMes;
            DateTime efectivaHasta = ahora < inicioMes ? inicioMes : ahora > finMes ? finMes : ahora;

            HistorialDTO historial =
                await _historial.ObtenerAsync(conductorId, null, null, inicioMes, finMes);

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
                    ruta, salida, conductorId, criterio, desvioMaximoKm, enviosUsados, ct);

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

            await CompletarPaquetesAsync(salidas);

            return new GananciasMensualDTO
            {
                Anio = anio,
                Mes = mes,
                Criterio = criterio,
                DesvioMaximoKm = desvioMaximoKm,

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

        private static (DateTime inicio, DateTime fin) LimitesDelMes(int anio, int mes)
        {
            DateTime inicio = new DateTime(anio, mes, 1);
            DateTime fin = inicio.AddMonths(1).AddTicks(-1);

            return (inicio, fin);
        }

        private async Task<SalidaEstimadaDTO?> EstimarSalidaAsync(
            Ruta ruta,
            DateTime salida,
            int conductorId,
            CriterioOptimizacion criterio,
            double? desvioMaximoKm,
            IReadOnlyCollection<int> yaUsados,
            CancellationToken ct)
        {
            SolicitudOptimizacionDTO solicitud = new()
            {
                RutaId = ruta.Id,
                FechaSalida = salida,
                DesvioMaximoKm = desvioMaximoKm ?? SinLimite,
                EnviosExcluidos = [.. yaUsados],
                // Son unas 12 salidas por carga: con rutas reales se pasaria del limite por minuto
                // de ORS. Como es una estimacion del mes, se calcula todo con distancias estimadas.
                SoloEstimado = true
            };

            ResultadoOptimizacionDTO resultado =
                await _optimizador.CalcularAsync(solicitud, conductorId, ct);

            if (resultado.Estado != EstadoResultadoOptimizacion.Correcto)
            {
                return null;
            }

            PropuestaOptimizacionDTO? propuesta = ElegirPropuesta(resultado, criterio);

            return new SalidaEstimadaDTO
            {
                RutaId = ruta.Id,
                FechaSalida = salida,
                Origen = ruta.DireccionOrigen.Texto,
                Destino = ruta.DireccionDestino.Texto,

                PropuestaElegida = propuesta is null ? null : EtiquetaDe(criterio),
                EnviosIds = propuesta?.EnviosNuevosIds ?? [],

                GananciaPotencial = propuesta?.IngresoAdicional ?? 0m,
                GananciaComprometida = resultado.IngresoComprometido,

                PesoKg = propuesta?.PesoAdicionalKg ?? 0,
                // solo lo que agregan los paquetes propuestos, no el desvio de los ya aceptados
                DesvioKm = propuesta?.DesvioAdicionalKm ?? 0,
                DistanciaKm = propuesta?.DistanciaTotalKm ?? (ruta.DistanciaMetros ?? 0) / 1000,
                DuracionMinutos = propuesta?.DuracionTotalMinutos ?? (ruta.DuracionSegundos ?? 0) / 60,

                CapacidadMaximaPaquetes = ruta.CapacidadMaxPaquetes,
                PesoMaximoKg = ruta.PesoMax
            };
        }

        private async Task CompletarPaquetesAsync(List<SalidaEstimadaDTO> salidas)
        {
            List<int> ids = salidas.SelectMany(s => s.EnviosIds).Distinct().ToList();

            if (ids.Count == 0)
            {
                return;
            }

            Dictionary<int, Envio> envios = (await _envios.ListarPorIdsAsync(ids))
                .ToDictionary(e => e.Id);

            foreach (SalidaEstimadaDTO salida in salidas)
            {
                salida.Paquetes = salida.EnviosIds
                    .Where(envios.ContainsKey)
                    .Select(id => ArmarPaquete(envios[id]))
                    .ToList();
            }
        }

        private static PaquetePropuestoDTO ArmarPaquete(Envio envio) => new()
        {
            EnvioId = envio.Id,
            Descripcion = envio.Paquete.Descripcion,
            PesoKg = envio.Paquete.PesoKg,
            Monto = envio.Monto,
            Retiro = envio.DireccionRetiro.Texto,
            Entrega = envio.DireccionEntrega.Texto
        };

        private static string EtiquetaDe(CriterioOptimizacion criterio) => criterio switch
        {
            CriterioOptimizacion.MayorGanancia => "Mayor ganancia",
            CriterioOptimizacion.MenorDesvio => "Menor desvío",
            _ => "Equilibrada"
        };

        private static PropuestaOptimizacionDTO? ElegirPropuesta(
            ResultadoOptimizacionDTO resultado,
            CriterioOptimizacion criterio)
        {
            string etiqueta = EtiquetaDe(criterio);

            return resultado.Propuestas.FirstOrDefault(p => p.Etiquetas.Contains(etiqueta));
        }

        private static List<SemanaDeGananciasDTO> AgruparPorSemana(
            IReadOnlyList<SalidaEstimadaDTO> salidas,
            DateTime desde,
            DateTime hasta)
        {
            List<SemanaDeGananciasDTO> semanas = [];

            if (desde > hasta)
            {
                return semanas;
            }

            int diasDesdeElLunes = ((int)desde.DayOfWeek + 6) % 7; //cuántos días pasaron desde el dia desde el lunes de esa semana
            DateTime lunes = desde.Date.AddDays(-diasDesdeElLunes);
            int numero = 1;

            while (lunes <= hasta)
            {
                DateTime finDeSemana = lunes.AddDays(7).AddTicks(-1);

                DateTime inicio = lunes > desde ? lunes : desde;
                DateTime fin = finDeSemana < hasta ? finDeSemana : hasta;

                List<SalidaEstimadaDTO> deLaSemana = salidas
                    .Where(s => s.FechaSalida >= inicio && s.FechaSalida <= fin)
                    .ToList();

                semanas.Add(new SemanaDeGananciasDTO
                {
                    Numero = numero,
                    Desde = inicio,
                    Hasta = fin,
                    Viajes = deLaSemana.Count,
                    Paquetes = deLaSemana.Sum(s => s.CantidadPaquetes),
                    GananciaPotencial = deLaSemana.Sum(s => s.GananciaPotencial)
                });

                lunes = lunes.AddDays(7);
                numero++;
            }

            return semanas;
        }
    }
}