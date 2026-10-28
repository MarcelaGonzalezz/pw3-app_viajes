using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios {
    public interface IEnvioRepositorio {
        Task CrearAsync(Envio envio);
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
    }
}
