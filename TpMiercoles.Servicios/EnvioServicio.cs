using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios {
    public interface IEnvioServicio {
        Task CrearAsync(EnvioDTO envio, int usuarioId);
        Task<List<EnvioListadoDTO>> ListarAsync(int usuarioId, EstadoEnvio? estado);
        Task<EnvioDetalleDTO?> ObtenerDetalleAsync(int envioId, int usuarioId);
        Task<ResultadoOperacionEnvio> CancelarAsync(int envioId, int usuarioId);
    }

    public class EnvioServicio : IEnvioServicio {
        private readonly IEnvioRepositorio _repositorio;

        public EnvioServicio(IEnvioRepositorio repositorio) {
            _repositorio = repositorio;
        }

        public async Task CrearAsync(EnvioDTO dto, int usuarioId) {
            var envio = new Envio {
                UsuarioId = usuarioId,
                DireccionRetiro = new Direccion { Texto = dto.Origen.Trim() },
                DireccionEntrega = new Direccion { Texto = dto.Destino.Trim() },
                FechaDesde = DateTime.Today,
                FechaHasta = dto.Fecha,
                Paquete = new Paquete {
                    PesoKg = dto.PesoKg,
                    Tamanio = dto.Tamanio,
                    Descripcion = dto.Descripcion.Trim()
                },
                Monto = dto.Monto,
                Estado = EstadoEnvio.BuscandoConductor
            };

            await _repositorio.CrearAsync(envio);
        }

        public async Task<List<EnvioListadoDTO>> ListarAsync(int usuarioId, EstadoEnvio? estado) {
            List<Envio> envios = await _repositorio.ListarPorUsuarioAsync(usuarioId, estado);

            return envios.Select(e => new EnvioListadoDTO {
                Id = e.Id,
                Origen = e.DireccionRetiro?.Texto ?? string.Empty,
                Destino = e.DireccionEntrega?.Texto ?? string.Empty,
                FechaDesde = e.FechaDesde,
                FechaHasta = e.FechaHasta,
                PesoKg = e.Paquete != null ? e.Paquete.PesoKg : 0,
                Tamanio = e.Paquete != null ? e.Paquete.Tamanio : default,
                Descripcion = e.Paquete?.Descripcion ?? string.Empty,
                Monto = e.Monto,
                Estado = e.Estado
            }).ToList();
        }

        public async Task<EnvioDetalleDTO?> ObtenerDetalleAsync(int envioId, int usuarioId) {
            Envio? envio = await _repositorio.ObtenerDetalleAsync(envioId);

            if (envio == null || envio.UsuarioId != usuarioId) {
                return null;
            }

            var dto = new EnvioDetalleDTO {
                Id = envio.Id,
                Origen = envio.DireccionRetiro?.Texto ?? string.Empty,
                Destino = envio.DireccionEntrega?.Texto ?? string.Empty,
                FechaDesde = envio.FechaDesde,
                FechaHasta = envio.FechaHasta,
                PesoKg = envio.Paquete != null ? envio.Paquete.PesoKg : 0,
                Tamanio = envio.Paquete != null ? envio.Paquete.Tamanio : default,
                Descripcion = envio.Paquete?.Descripcion ?? string.Empty,
                Monto = envio.Monto,
                Estado = envio.Estado
            };

            var asignacion = envio.Asignaciones?
                .FirstOrDefault(a => a.Estado != EstadoViaje.CANCELADO && a.Viaje != null)
                ?? envio.Asignaciones?.FirstOrDefault(a => a.Viaje != null);

            if (asignacion != null && asignacion.Viaje != null) {
                dto.TieneConductorAsignado = asignacion.Estado != EstadoViaje.CANCELADO;
                dto.ViajeId = asignacion.ViajeId;

                var conductor = asignacion.Viaje.Ruta?.Usuario;
                if (conductor != null) {
                    var nombreCompleto = $"{conductor.Nombre} {conductor.Apellido}".Trim();
                    dto.ConductorNombre = string.IsNullOrWhiteSpace(nombreCompleto) ? null : nombreCompleto;
                    dto.ConductorEmail = conductor.Email;
                }

                dto.FechaSalidaViaje = asignacion.Viaje.FechaSalida;
                dto.FechaAceptacion = asignacion.FechaAceptacion;
                dto.FechaRetiro = asignacion.FechaRetiro;
                dto.FechaEntrega = asignacion.FechaEntrega;
            }

            return dto;
        }

        public async Task<ResultadoOperacionEnvio> CancelarAsync(int envioId, int usuarioId) {
            Envio? envio = await _repositorio.ObtenerParaModificarAsync(envioId);

            if (envio is null || envio.UsuarioId != usuarioId) {
                return ResultadoOperacionEnvio.NoExiste();
            }

            if (envio.Estado != EstadoEnvio.BuscandoConductor) {
                return ResultadoOperacionEnvio.Invalido("Solo se pueden cancelar envíos que se encuentren en búsqueda de conductor.");
            }

            envio.Estado = EstadoEnvio.Cancelado;
            await _repositorio.GuardarCambiosAsync();

            return ResultadoOperacionEnvio.Exitoso();
        }
    }
}
