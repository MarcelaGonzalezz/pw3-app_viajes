using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios {
    public interface IEnvioServicio {
        Task CrearAsync(EnvioDTO envio, int usuarioId);
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
    }
}
