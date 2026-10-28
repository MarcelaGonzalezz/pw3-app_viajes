
using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios
{

    public interface IRutaRepositorio
    {
        Task CrearRutaAsync(Ruta ruta);
        Task<List<Ruta>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId);
    }
    public class RutaRepositorio : IRutaRepositorio

    {
        private readonly ViajesDbContext _viajesDbContext;

        public RutaRepositorio(ViajesDbContext viajesDbContext)
        {
            _viajesDbContext = viajesDbContext;
        }

        public async Task CrearRutaAsync(Ruta ruta)
        {
            _viajesDbContext.Add(ruta);
            await _viajesDbContext.SaveChangesAsync();
        }

        public async Task<List<Ruta>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId)
        {
            return await _viajesDbContext.Rutas
                .Where(r => r.UsuarioId == usuarioId && r.Estado == EstadoRuta.ACTIVA)
                .ToListAsync();
        }
    }
}
