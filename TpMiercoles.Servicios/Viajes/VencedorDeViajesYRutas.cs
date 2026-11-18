using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios.Viajes
{
    public interface IVencedorDeViajesYRutas
    {
        // Pasa a INACTIVO los viajes que ya vencieron y a INACTIVA las rutas que ya vencieron.
        // ahora: la fecha y hora actual.
        Task VencerAsync(DateTime ahora);
    }

    // Lo usa el proceso que corre cada minuto (MaterializacionDeViajesHostedService).
    public class VencedorDeViajesYRutas : IVencedorDeViajesYRutas
    {
        private readonly IViajeRepositorio _viajeRepositorio;
        private readonly IRutaRepositorio _rutaRepositorio;

        public VencedorDeViajesYRutas(IViajeRepositorio viajeRepositorio, IRutaRepositorio rutaRepositorio)
        {
            _viajeRepositorio = viajeRepositorio;
            _rutaRepositorio = rutaRepositorio;
        }

        public async Task VencerAsync(DateTime ahora)
        {
            // Viajes activos que ya salieron: los que ademas ya llegaron pasan a inactivo.
            List<Viaje> viajes = await _viajeRepositorio.ObtenerActivosQueYaSalieronAsync(ahora);

            foreach (Viaje viaje in viajes)
            {
                if (Vencimiento.EstaVencido(viaje, ahora))
                {
                    viaje.Estado = EstadoActividadViaje.INACTIVO;
                }
            }

            await _viajeRepositorio.GuardarCambiosAsync();

            // Rutas activas cuya fecha "hasta" es hoy o antes: las que ya vencieron pasan a inactiva.
            List<Ruta> rutas = await _rutaRepositorio.ObtenerActivasConFechaFinHastaAsync(ahora.Date);

            foreach (Ruta ruta in rutas)
            {
                if (Vencimiento.EstaVencida(ruta, ahora))
                {
                    ruta.Estado = EstadoRuta.INACTIVA;
                }
            }

            await _rutaRepositorio.GuardarCambiosAsync();
        }
    }
}
