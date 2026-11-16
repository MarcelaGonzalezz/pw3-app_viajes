using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios
{
    public interface IRutaRepositorio
    {
        Task CrearRutaAsync(Ruta ruta);
        Task<List<Ruta>> ObtenerRutasActivasAsync();
        Task<List<Ruta>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId);
        Task<List<Ruta>> ObtenerRutasDeUnUsuarioAsync(int usuarioId);
        Task<Ruta?> ObtenerPorIdAsync(int id);
        Task<Ruta?> ObtenerPorIdParaModificarAsync(int id);

        // Igual que ObtenerPorIdParaModificarAsync, pero ademas trae los viajes ya creados
        // con sus asignaciones, para validar la edicion contra ellos.
        Task<Ruta?> ObtenerParaEditarAsync(int id);

        // Borran las filas de paradas y dias de la ruta. Hace falta borrarlas explicitamente:
        // quitarlas de la coleccion solo dejaria el RutaId en null (y la parada violaria CK_Parada_UnDuenio).
        void EliminarParadas(IEnumerable<Parada> paradas);
        void EliminarDias(IEnumerable<RutaDia> dias);

        Task GuardarCambiosAsync();
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

        public async Task<List<Ruta>> ObtenerRutasActivasAsync()
        {
            return await _viajesDbContext.Rutas
                .Include(r => r.DireccionOrigen)
                .Include(r => r.DireccionDestino)
                .Include(r => r.RutaDias)
                .Include(r => r.Paradas)
                    .ThenInclude(p => p.Direccion)
                .Where(r => r.Estado == EstadoRuta.ACTIVA)
                .ToListAsync();
        }

        public async Task<List<Ruta>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId)
        {
            return await _viajesDbContext.Rutas
                .Include(r => r.DireccionOrigen)
                .Include(r => r.DireccionDestino)
                .Include(r => r.RutaDias)
                .Include(r => r.Paradas)
                    .ThenInclude(p => p.Direccion)
                .Where(r => r.UsuarioId == usuarioId && r.Estado == EstadoRuta.ACTIVA)
                .ToListAsync();
        }

        public async Task<List<Ruta>> ObtenerRutasDeUnUsuarioAsync(int usuarioId)
        {
            return await _viajesDbContext.Rutas
                .Include(r => r.DireccionOrigen)
                .Include(r => r.DireccionDestino)
                .Include(r => r.RutaDias)
                .Include(r => r.Paradas)
                    .ThenInclude(p => p.Direccion)
                .Where(r => r.UsuarioId == usuarioId && r.Estado != EstadoRuta.ELIMINADA)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<Ruta?> ObtenerPorIdAsync(int id)
        {
            return await _viajesDbContext.Rutas
                .Include(r => r.DireccionOrigen)
                .Include(r => r.DireccionDestino)
                .Include(r => r.RutaDias)
                .Include(r => r.Paradas)
                    .ThenInclude(p => p.Direccion)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Ruta?> ObtenerPorIdParaModificarAsync(int id)
        {
            return await _viajesDbContext.Rutas
                .Include(r => r.DireccionOrigen)
                .Include(r => r.DireccionDestino)
                .Include(r => r.RutaDias)
                .Include(r => r.Paradas)
                    .ThenInclude(p => p.Direccion)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Ruta?> ObtenerParaEditarAsync(int id)
        {
            return await _viajesDbContext.Rutas
                .Include(r => r.DireccionOrigen)
                .Include(r => r.DireccionDestino)
                .Include(r => r.RutaDias)
                .Include(r => r.Paradas)
                    .ThenInclude(p => p.Direccion)
                .Include(r => r.Viajes)
                    .ThenInclude(v => v.Asignaciones)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public void EliminarParadas(IEnumerable<Parada> paradas) => _viajesDbContext.RemoveRange(paradas);

        public void EliminarDias(IEnumerable<RutaDia> dias) => _viajesDbContext.RemoveRange(dias);

        public Task GuardarCambiosAsync() => _viajesDbContext.SaveChangesAsync();
    }
}
