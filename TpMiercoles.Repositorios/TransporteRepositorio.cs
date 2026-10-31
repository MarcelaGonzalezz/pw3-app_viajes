using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios
{
    public interface ITransporteRepositorio
    {
        // Asignaciones vigentes cuyos viajes son del conductor. Si estado es null
        // trae todas. El estado que se filtra es el del Envio, no el de la Asignacion.
        Task<List<Asignacion>> ListarPorConductorAsync(int conductorId, EstadoEnvio? estado);

        // Una asignacion con todo lo que necesita la pantalla de detalle.
        // Devuelve null si no existe.
        Task<Asignacion?> ObtenerDetalleAsync(int asignacionId);

        // Igual que ObtenerDetalle pero con seguimiento de cambios, para poder
        // avanzar el estado. Incluye el viaje y su ruta para saber de quien es.
        Task<Asignacion?> ObtenerParaModificarAsync(int asignacionId);

        Task GuardarCambiosAsync();
    }

    public class TransporteRepositorio : ITransporteRepositorio
    {
        private readonly ViajesDbContext _viajesDbContext;

        public TransporteRepositorio(ViajesDbContext viajesDbContext)
        {
            _viajesDbContext = viajesDbContext;
        }

        public async Task<List<Asignacion>> ListarPorConductorAsync(int conductorId, EstadoEnvio? estado)
        {
            IQueryable<Asignacion> consulta = _viajesDbContext.Set<Asignacion>()
                .AsNoTracking()
                .Include(a => a.Envio).ThenInclude(e => e.Usuario)
                .Include(a => a.Envio).ThenInclude(e => e.DireccionRetiro)
                .Include(a => a.Envio).ThenInclude(e => e.DireccionEntrega)
                .Include(a => a.Viaje).ThenInclude(v => v.Ruta)
                .Where(a => a.Viaje.Ruta.UsuarioId == conductorId
                            && a.Estado != EstadoViaje.CANCELADO);

            if (estado is not null)
            {
                consulta = consulta.Where(a => a.Envio.Estado == estado.Value);
            }

            return await consulta
                .OrderBy(a => a.Viaje.FechaSalida)
                .ThenBy(a => a.Id)
                .ToListAsync();
        }

        public async Task<Asignacion?> ObtenerDetalleAsync(int asignacionId)
        {
            return await _viajesDbContext.Set<Asignacion>()
                .AsNoTracking()
                .Include(a => a.Envio).ThenInclude(e => e.Usuario)
                .Include(a => a.Envio).ThenInclude(e => e.DireccionRetiro)
                .Include(a => a.Envio).ThenInclude(e => e.DireccionEntrega)
                .Include(a => a.Viaje).ThenInclude(v => v.Ruta)
                .Include(a => a.Viaje).ThenInclude(v => v.DireccionOrigen)
                .Include(a => a.Viaje).ThenInclude(v => v.DireccionDestino)
                .FirstOrDefaultAsync(a => a.Id == asignacionId);
        }

        public async Task<Asignacion?> ObtenerParaModificarAsync(int asignacionId)
        {
            return await _viajesDbContext.Set<Asignacion>()
                .Include(a => a.Envio).ThenInclude(e => e.Usuario)
                .Include(a => a.Envio).ThenInclude(e => e.DireccionRetiro)
                .Include(a => a.Envio).ThenInclude(e => e.DireccionEntrega)
                .Include(a => a.Viaje).ThenInclude(v => v.Ruta)
                .FirstOrDefaultAsync(a => a.Id == asignacionId);
        }

        public Task GuardarCambiosAsync() => _viajesDbContext.SaveChangesAsync();
    }
}
