using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Mapa;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios.Viajes {
    public interface IMapaViajeServicio {
        // Devuelve null si el viaje no existe o no es del usuario.
        Task<MapaViajeDTO?> ObtenerAsync(int viajeId, int usuarioId, CancellationToken ct = default);

    }

    public class MapaViajeServicio : IMapaViajeServicio {
        private readonly IViajeRepositorio _repositorio;
        private readonly IServicioRutas _rutas;

        public MapaViajeServicio(IViajeRepositorio repositorio, IServicioRutas rutas) {
            _repositorio = repositorio;
            _rutas = rutas;
        }

        public async Task<MapaViajeDTO?> ObtenerAsync(int viajeId, int usuarioId, CancellationToken ct = default) {
            Viaje? viaje = await _repositorio.ObtenerDetalleAsync(viajeId);

            // Mismo criterio que el detalle: si no es del usuario, no existe para él.
            if (viaje is null || viaje.Ruta.UsuarioId != usuarioId) {
                return null;
            }
            List<Parada> paradas = viaje.Paradas
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .ToList();

            List<Envio> envios = viaje.Asignaciones
                .Where(a => a.Estado != EstadoViaje.CANCELADO && a.Envio is not null)
                .Select(a => a.Envio)
                .Where(e => e.Estado != EstadoEnvio.Cancelado)
                .OrderBy(e => e.Id)
                .ToList();

            var mapa = new MapaViajeDTO {
                Puntos = ArmarPuntos(viaje, paradas, envios)
            };
            List<Coordenada> recorridoBase =
            [
                ACoordenada(viaje.DireccionOrigen),
                .. paradas.Select(p => ACoordenada(p.Direccion)),
                ACoordenada(viaje.DireccionDestino)
            ];

            try {
                await CompletarRecorridoBaseAsync(mapa, viaje.Ruta, recorridoBase, ct);

                if (envios.Count > 0) {
                    List<Coordenada> recorridoConEnvios = InsertarEnvios(recorridoBase, envios);
                    ResultadoRuta conEnvios = await CalcularAsync(recorridoConEnvios, ct);

                    mapa.GeometriaConEnvios = conEnvios.GeometriaJson;
                    mapa.DistanciaConEnviosKm = Math.Round(conEnvios.DistanciaKm, 1);

                    // Sin trazado = distancia estimada (ORS no disponible). Si el habitual es real, se mantiene:
                    // el desvío se estima comparando mock con mock y se suma a la distancia real.
                    if (string.IsNullOrWhiteSpace(conEnvios.GeometriaJson)
                        && !mapa.DistanciasEstimadas
                        && mapa.DistanciaBaseKm is double baseRealKm) {
                        ResultadoRuta baseEstimada = await CalcularAsync(recorridoBase, ct);
                        double desvioEstimadoKm = Math.Max(0, conEnvios.DistanciaKm - baseEstimada.DistanciaKm);

                        mapa.DistanciaConEnviosKm = Math.Round(baseRealKm + desvioEstimadoKm, 1);
                        mapa.DistanciasEstimadas = true;
                    }
                }
            } catch (RutaNoEncontradaException) {
                // Sin servicio de rutas igual devolvemos los puntos: el mapa muestra los marcadores sin líneas.
            }

            return mapa;
        }
        // Reusa el trazado guardado en la ruta (ya calculado al crearla) para no gastar una consulta.
        // Reusa el trazado guardado en la ruta (ya calculado al crearla) para no gastar una consulta.
        private async Task CompletarRecorridoBaseAsync(
            MapaViajeDTO mapa, Ruta ruta, List<Coordenada> recorridoBase, CancellationToken ct) {
            if (!string.IsNullOrWhiteSpace(ruta.GeometriaJson) && ruta.DistanciaMetros is double metros) {
                mapa.GeometriaBase = ruta.GeometriaJson;
                mapa.DistanciaBaseKm = Math.Round(metros / 1000.0, 1);
                return;
            }

            ResultadoRuta resultado = await CalcularAsync(recorridoBase, ct);
            mapa.GeometriaBase = resultado.GeometriaJson;
            mapa.DistanciaBaseKm = Math.Round(resultado.DistanciaKm, 1);
            mapa.DistanciasEstimadas = string.IsNullOrWhiteSpace(resultado.GeometriaJson);
        }

        // Mismo criterio que usa el optimizador: cada retiro/entrega va donde menos alarga el recorrido.
        private static List<Coordenada> InsertarEnvios(List<Coordenada> recorridoBase, List<Envio> envios) {
            List<Coordenada> recorrido = new(recorridoBase);

            foreach (Envio envio in envios) {
                recorrido = PlanificadorDeRecorrido.InsertarEnvio(
                    recorrido,
                    ACoordenada(envio.DireccionRetiro),
                    ACoordenada(envio.DireccionEntrega));
            }

            return recorrido;
        }

        private Task<ResultadoRuta> CalcularAsync(List<Coordenada> recorrido, CancellationToken ct)
            => _rutas.CalcularRutaAsync(recorrido[0], recorrido[^1], recorrido[1..^1], ct);

        private static List<PuntoMapaDTO> ArmarPuntos(Viaje viaje, List<Parada> paradas, List<Envio> envios) {
            List<PuntoMapaDTO> puntos = [CrearPunto(TipoPuntoMapa.Origen, viaje.DireccionOrigen)];

            puntos.AddRange(paradas.Select(p => {
                PuntoMapaDTO punto = CrearPunto(TipoPuntoMapa.Parada, p.Direccion);
                punto.Orden = p.Orden;
                return punto;
            }));

            foreach (Envio envio in envios) {
                PuntoMapaDTO retiro = CrearPunto(TipoPuntoMapa.Retiro, envio.DireccionRetiro);
                retiro.EnvioId = envio.Id;
                puntos.Add(retiro);

                PuntoMapaDTO entrega = CrearPunto(TipoPuntoMapa.Entrega, envio.DireccionEntrega);
                entrega.EnvioId = envio.Id;
                puntos.Add(entrega);
            }

            puntos.Add(CrearPunto(TipoPuntoMapa.Destino, viaje.DireccionDestino));
            return puntos;
        }

        private static PuntoMapaDTO CrearPunto(TipoPuntoMapa tipo, Direccion direccion) => new() {
            Tipo = tipo,
            Texto = direccion.Texto,
            Latitud = direccion.Latitud,
            Longitud = direccion.Longitud
        };

        private static Coordenada ACoordenada(Direccion direccion) => new(direccion.Latitud, direccion.Longitud);
    }
}