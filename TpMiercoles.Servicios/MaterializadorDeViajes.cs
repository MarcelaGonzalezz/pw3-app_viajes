using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface IMaterializadorDeViajes
    {
        // Crea los viajes de las rutas activas cuya salida de hoy ya llego y todavia no
        // tienen un viaje materializado. Devuelve cuantos viajes creo.
        Task<int> MaterializarSalidasAsync(CancellationToken ct = default);
    }

    public class MaterializadorDeViajes : IMaterializadorDeViajes
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        private readonly IRutaRepositorio _rutaRepositorio;
        private readonly IViajeRepositorio _viajeRepositorio;
        private readonly TimeProvider _reloj;

        public MaterializadorDeViajes(
            IRutaRepositorio rutaRepositorio,
            IViajeRepositorio viajeRepositorio,
            TimeProvider reloj)
        {
            _rutaRepositorio = rutaRepositorio;
            _viajeRepositorio = viajeRepositorio;
            _reloj = reloj;
        }

        public async Task<int> MaterializarSalidasAsync(CancellationToken ct = default)
        {
            DateTime ahora = TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), ZonaArgentina).DateTime;
            List<Ruta> rutas = await _rutaRepositorio.ObtenerRutasActivasAsync();
            int creados = 0;

            foreach (Ruta ruta in rutas)
            {
                DateTime? salida = CalendarioDeSalidas.YaSalioHoy(ruta, ahora);

                if (salida is null)
                {
                    continue;
                }

                Viaje? existente =
                    await _viajeRepositorio.ObtenerParaAsignarPorRutaYFechaAsync(ruta.Id, salida.Value);

                if (existente is not null)
                {
                    continue;
                }

                DateTime llegada = salida.Value + CalendarioDeSalidas.Duracion(ruta);
                Viaje viaje = SnapshotDeRuta.Crear(ruta, salida.Value, llegada);

                await _viajeRepositorio.AgregarSinGuardarAsync(viaje);
                await _viajeRepositorio.GuardarCambiosAsync();
                creados++;
            }

            return creados;
        }
    }
}
