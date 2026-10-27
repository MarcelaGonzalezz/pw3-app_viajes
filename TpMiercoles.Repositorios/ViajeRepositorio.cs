using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios
{
    public interface IViajeRepositorio
    {
        // Trae un viaje con todo lo que necesita la pantalla de detalle:
        // origen y destino, su ruta (con paradas y dias), y los envios asignados.
        // Devuelve null si el viaje no existe.
        Task<Viaje?> ObtenerDetalleAsync(int viajeId);

        // Viajes cuya ruta pertenece al usuario. Si estado es null trae todos.
        Task<List<Viaje>> ListarPorUsuarioAsync(int usuarioId, EstadoActividadViaje? estado);

        // Igual que ObtenerDetalle pero con seguimiento de cambios, para poder modificarlo.
        // Incluye la ruta (para saber de quien es) y las asignaciones (para validar capacidad).
        Task<Viaje?> ObtenerParaModificarAsync(int viajeId);

        Task GuardarCambiosAsync();

        Task<bool> ExisteAlgunoAsync();

        Task AgregarAsync(IEnumerable<Viaje> viajes);
    }

    public class ViajeRepositorio : IViajeRepositorio
    {
        private readonly ViajesDbContext _viajesDbContext;

        public ViajeRepositorio(ViajesDbContext viajesDbContext)
        {
            _viajesDbContext = viajesDbContext;
        }

        public async Task<Viaje?> ObtenerDetalleAsync(int viajeId)
        {
            return await _viajesDbContext.Viajes
                .AsNoTracking()
                .Include(v => v.DireccionOrigen)
                .Include(v => v.DireccionDestino)
                .Include(v => v.Ruta)
                .Include(v => v.Ruta).ThenInclude(r => r.Paradas).ThenInclude(p => p.Direccion)
                .Include(v => v.Ruta).ThenInclude(r => r.RutaDias)
                .Include(v => v.Asignaciones).ThenInclude(a => a.Envio).ThenInclude(e => e.Usuario)
                .Include(v => v.Asignaciones).ThenInclude(a => a.Envio).ThenInclude(e => e.DireccionRetiro)
                .Include(v => v.Asignaciones).ThenInclude(a => a.Envio).ThenInclude(e => e.DireccionEntrega)
                .FirstOrDefaultAsync(v => v.Id == viajeId);
        }

        public async Task<List<Viaje>> ListarPorUsuarioAsync(int usuarioId, EstadoActividadViaje? estado)
        {
            IQueryable<Viaje> consulta = _viajesDbContext.Viajes
                .AsNoTracking()
                .Include(v => v.DireccionOrigen)
                .Include(v => v.DireccionDestino)
                .Include(v => v.Asignaciones)
                .Where(v => v.Ruta.UsuarioId == usuarioId);

            if (estado is not null)
            {
                consulta = consulta.Where(v => v.Estado == estado.Value);
            }

            return await consulta
                .OrderBy(v => v.FechaSalida)
                .ThenBy(v => v.Id)
                .ToListAsync();
        }

        public async Task<Viaje?> ObtenerParaModificarAsync(int viajeId)
        {
            return await _viajesDbContext.Viajes
                .Include(v => v.DireccionOrigen)
                .Include(v => v.DireccionDestino)
                .Include(v => v.Ruta)
                .Include(v => v.Asignaciones)
                .FirstOrDefaultAsync(v => v.Id == viajeId);
        }

        public Task GuardarCambiosAsync() => _viajesDbContext.SaveChangesAsync();

        public Task<bool> ExisteAlgunoAsync() => _viajesDbContext.Viajes.AnyAsync();

        public async Task AgregarAsync(IEnumerable<Viaje> viajes)
        {
            _viajesDbContext.Viajes.AddRange(viajes);
            await _viajesDbContext.SaveChangesAsync();
        }
    }
}
