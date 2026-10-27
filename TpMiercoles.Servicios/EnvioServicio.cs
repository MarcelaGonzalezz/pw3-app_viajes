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
    }
}
