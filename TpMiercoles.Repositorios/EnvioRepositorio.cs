using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios {
    public interface IEnvioRepositorio {
        Task CrearAsync(Envio envio);
        Task<List<Envio>> ListarPorUsuarioAsync(int usuarioId, EstadoEnvio? estado);
        Task<Envio?> ObtenerDetalleAsync(int envioId);
        Task<Envio?> ObtenerParaModificarAsync(int envioId);
        Task GuardarCambiosAsync();
    }
    public class EnvioRepositorio : IEnvioRepositorio {
        private readonly ViajesDbContext _viajesDbContext;

        public EnvioRepositorio(ViajesDbContext viajesDbContext) {
            _viajesDbContext = viajesDbContext;
        }

        public async Task CrearAsync(Envio envio) {
            _viajesDbContext.Envios.Add(envio);
            await _viajesDbContext.SaveChangesAsync();
        }

        public async Task<List<Envio>> ListarPorUsuarioAsync(int usuarioId, EstadoEnvio? estado) {
            IQueryable<Envio> consulta = _viajesDbContext.Envios
                .AsNoTracking()
                .Include(e => e.DireccionRetiro)
                .Include(e => e.DireccionEntrega)
                .Where(e => e.UsuarioId == usuarioId);

            if (estado.HasValue) {
                consulta = consulta.Where(e => e.Estado == estado.Value);
            }

            return await consulta
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }

        public async Task<Envio?> ObtenerDetalleAsync(int envioId) {
            return await _viajesDbContext.Envios
                .AsNoTracking()
                .Include(e => e.DireccionRetiro)
                .Include(e => e.DireccionEntrega)
                .Include(e => e.Asignaciones)
                    .ThenInclude(a => a.Viaje)
                        .ThenInclude(v => v.Ruta)
                            .ThenInclude(r => r.Usuario)
                .FirstOrDefaultAsync(e => e.Id == envioId);
        }

        public async Task<Envio?> ObtenerParaModificarAsync(int envioId) {
            return await _viajesDbContext.Envios
                .Include(e => e.DireccionRetiro)
                .Include(e => e.DireccionEntrega)
                .Include(e => e.Asignaciones)
                .FirstOrDefaultAsync(e => e.Id == envioId);
        }

        public Task GuardarCambiosAsync() => _viajesDbContext.SaveChangesAsync();
    }
}
