using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios
{
    public interface IHistorialRepositorio
    {
        // Viajes del usuario (como conductor) que ya salieron y en los que se entrego
        // al menos un paquete. Trae asignaciones y envios para poder sumar la ganancia.
        Task<List<Viaje>> ListarViajesRealizadosAsync(int usuarioId, DateTime ahora);

        // Envios publicados por el usuario (como remitente) que ya terminaron:
        // entregados o cancelados.
        Task<List<Envio>> ListarEnviosFinalizadosAsync(int usuarioId);
    }

    public class HistorialRepositorio : IHistorialRepositorio
    {
        private readonly ViajesDbContext _viajesDbContext;

        public HistorialRepositorio(ViajesDbContext viajesDbContext)
        {
            _viajesDbContext = viajesDbContext;
        }

        public async Task<List<Viaje>> ListarViajesRealizadosAsync(int usuarioId, DateTime ahora)
        {
            return await _viajesDbContext.Viajes
                .AsNoTracking()
                .Include(v => v.DireccionOrigen)
                .Include(v => v.DireccionDestino)
                .Include(v => v.Asignaciones).ThenInclude(a => a.Envio)
                .Where(v => v.Ruta.UsuarioId == usuarioId
                            && v.FechaSalida <= ahora
                            && v.Asignaciones.Any(a => a.Estado != EstadoViaje.CANCELADO
                                                       && a.Envio.Estado == EstadoEnvio.Entregado))
                .OrderByDescending(v => v.FechaSalida)
                .ThenByDescending(v => v.Id)
                .ToListAsync();
        }

        public async Task<List<Envio>> ListarEnviosFinalizadosAsync(int usuarioId)
        {
            return await _viajesDbContext.Envios
                .AsNoTracking()
                .Include(e => e.DireccionRetiro)
                .Include(e => e.DireccionEntrega)
                .Include(e => e.Asignaciones)
                .Where(e => e.UsuarioId == usuarioId
                            && (e.Estado == EstadoEnvio.Entregado || e.Estado == EstadoEnvio.Cancelado))
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }
    }
}
