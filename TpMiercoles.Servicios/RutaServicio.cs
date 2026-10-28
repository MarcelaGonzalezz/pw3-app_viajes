using AutoMapper;

using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios
{
    public interface IRutaServicio
    {
        Task CrearRutaAsync(int usuarioId, CrearRutaDTO rutaDTO);
        Task<List<RutaDTO>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId);
    }

    public class RutaServicio : IRutaServicio
    {
        private readonly IRutaRepositorio _rutaRepositorio;
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IMapper _mapper;
        public RutaServicio(IRutaRepositorio rutaRepositorio, IUsuarioRepositorio usuarioRepositorio, IMapper mapper)
        {
            _rutaRepositorio = rutaRepositorio;
            _usuarioRepositorio = usuarioRepositorio;
            _mapper = mapper;
        }
        public async Task CrearRutaAsync(int usuarioId, CrearRutaDTO rutaDTO)
        {
            ValidarRuta(rutaDTO);
            Usuario usuario = await _usuarioRepositorio.BuscarPorIdAsync(usuarioId);

           

            Ruta ruta = _mapper.Map<Ruta>(rutaDTO);
            ruta.Usuario = usuario;
            await _rutaRepositorio.CrearRutaAsync(ruta);
        }

        public async Task<List<RutaDTO>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId)
        {
            List<Ruta> rutas = await _rutaRepositorio.ObtenerRutasActivasDeUnUsuarioAsync(usuarioId);
            return _mapper.Map<List<RutaDTO>>(rutas);
        }

        private void ValidarRuta(CrearRutaDTO rutaDTO)
        {

            var origen = Clave(rutaDTO.DireccionOrigen);
            var destino = Clave(rutaDTO.DireccionDestino);

            if (origen == destino)
                throw new ValidacionRutaException("El origen y el destino deben ser diferentes.");

            if (rutaDTO.RutaDias.Count == 0)
                throw new ValidacionRutaException("Debe seleccionar al menos un día.");

            if (rutaDTO.PesoMax <= 0 || rutaDTO.CapacidadMaxPaquetes <= 0)
                throw new ValidacionRutaException("Peso y capacidad deben ser mayores que cero.");

            if (rutaDTO.HoraSalida >= rutaDTO.HoraLlegada)
                throw new ValidacionRutaException("La hora de salida debe ser anterior a la de llegada.");
            if (rutaDTO.FechaFin < rutaDTO.FechaInicio)
                throw new ValidacionRutaException("La fecha de fin no puede ser anterior a la de inicio.");

            var paradasVistas = new HashSet<(decimal, decimal)>();

            foreach (var p in rutaDTO.Paradas)
            {
                var clave = Clave(p.Direccion);
                if (clave == origen || clave == destino)
                    throw new ValidacionRutaException("La parada no puede ser igual a la direccion de origen o destino");

                if (!paradasVistas.Add(clave))   
                    throw new ValidacionRutaException("No se pueden repetir paradas.");
            }
            var ordenes = rutaDTO.Paradas.Select(p => p.Orden).OrderBy(o => o).ToList();

            if (!ordenes.SequenceEqual(Enumerable.Range(1, rutaDTO.Paradas.Count)))
                throw new ValidacionRutaException("El orden de las paradas debe ser consecutivo, empezando en 1.");
        }
        private (decimal Lat, decimal Lng) Clave(DireccionDTO d)
    => (Math.Round(d.Latitud, 5), Math.Round(d.Longitud, 5));
    }
}
