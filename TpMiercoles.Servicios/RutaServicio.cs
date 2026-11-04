using AutoMapper;

using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios {
    public interface IRutaServicio {
        Task CrearRutaAsync(int usuarioId, CrearRutaDTO rutaDTO);
        Task<List<RutaDTO>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId);
        Task<List<RutaDTO>> ObtenerRutasDeUnUsuarioAsync(int usuarioId);
        Task<RutaDTO> ObtenerDetalleAsync(int rutaId, int usuarioId);
        Task DesactivarRutaAsync(int rutaId, int usuarioId);
        Task ActivarRutaAsync(int rutaId, int usuarioId);
        Task<EditarRutaDTO> ObtenerParaEditarAsync(int rutaId, int usuarioId);
        Task EditarRutaAsync(int usuarioId, int rutaId, EditarRutaDTO dto);
    }

    public class RutaServicio : IRutaServicio {
        private readonly IRutaRepositorio _rutaRepositorio;
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IMapper _mapper;
        private readonly IRecorridoServicio _recorridoServicio;

        public RutaServicio(IRutaRepositorio rutaRepositorio, IUsuarioRepositorio usuarioRepositorio,
              IMapper mapper, IRecorridoServicio recorridoServicio) {
            _rutaRepositorio = rutaRepositorio;
            _usuarioRepositorio = usuarioRepositorio;
            _mapper = mapper;
            _recorridoServicio = recorridoServicio;
        }

        public async Task<RutaDTO> ObtenerDetalleAsync(int rutaId, int usuarioId) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para ver esta ruta.");

            return _mapper.Map<RutaDTO>(ruta);
        }

        public async Task CrearRutaAsync(int usuarioId, CrearRutaDTO rutaDTO) {
            ValidarRuta(rutaDTO);
            Usuario usuario = await _usuarioRepositorio.BuscarPorIdAsync(usuarioId);

            Ruta ruta = _mapper.Map<Ruta>(rutaDTO);
            ruta.Usuario = usuario;

            await CalcularRecorridoAsync(ruta, rutaDTO.DireccionOrigen, rutaDTO.DireccionDestino, rutaDTO.Paradas);

            await _rutaRepositorio.CrearRutaAsync(ruta);
        }

        public async Task<List<RutaDTO>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId) {
            List<Ruta> rutas = await _rutaRepositorio.ObtenerRutasActivasDeUnUsuarioAsync(usuarioId);
            return _mapper.Map<List<RutaDTO>>(rutas);
        }

        public async Task<List<RutaDTO>> ObtenerRutasDeUnUsuarioAsync(int usuarioId) {
            List<Ruta> rutas = await _rutaRepositorio.ObtenerRutasDeUnUsuarioAsync(usuarioId);
            return _mapper.Map<List<RutaDTO>>(rutas);
        }

        public async Task DesactivarRutaAsync(int rutaId, int usuarioId) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdParaModificarAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            ruta.Estado = EstadoRuta.INACTIVA;
            await _rutaRepositorio.GuardarCambiosAsync();
        }

        public async Task ActivarRutaAsync(int rutaId, int usuarioId) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdParaModificarAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            ruta.Estado = EstadoRuta.ACTIVA;
            await _rutaRepositorio.GuardarCambiosAsync();
        }

        public async Task<EditarRutaDTO> ObtenerParaEditarAsync(int rutaId, int usuarioId) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdParaModificarAsync(rutaId)
                         ?? await _rutaRepositorio.ObtenerPorIdAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            return _mapper.Map<EditarRutaDTO>(ruta);
        }

        public async Task EditarRutaAsync(int usuarioId, int rutaId, EditarRutaDTO dto) {
            ValidarRuta(dto);

            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdParaModificarAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            // Validar capacidad contra viajes comprometidos:
            // si algún viaje futuro (FechaSalida >= DateTime.Now o Estado == ACTIVO) tiene más paquetes
            // asignados activos (Estado != CANCELADO) que dto.CapacidadMaxPaquetes o mayor peso que dto.PesoMax,
            // lanzar ValidacionRutaException.
            if (ruta.Viajes != null) {
                var ahora = DateTime.Now;
                var viajesComprometidos = ruta.Viajes.Where(v => v.FechaSalida >= ahora || v.Estado == EstadoActividadViaje.ACTIVO);
                foreach (var viaje in viajesComprometidos) {
                    var asignacionesActivas = viaje.Asignaciones.Where(a => a.Estado != EstadoViaje.CANCELADO).ToList();
                    int paquetesActivos = asignacionesActivas.Count;
                    if (paquetesActivos > dto.CapacidadMaxPaquetes) {
                        throw new ValidacionRutaException($"No es posible reducir la capacidad a {dto.CapacidadMaxPaquetes} paquetes porque el viaje del {viaje.FechaSalida:dd/MM/yyyy} tiene {paquetesActivos} paquete(s) asignado(s).");
                    }

                    double pesoActivo = asignacionesActivas.Sum(a => a.Envio?.Paquete?.PesoKg ?? 0);
                    if (pesoActivo > dto.PesoMax) {
                        throw new ValidacionRutaException($"No es posible reducir el peso máximo a {dto.PesoMax:0.##} kg porque el viaje del {viaje.FechaSalida:dd/MM/yyyy} tiene {pesoActivo:0.##} kg en paquetes asignados.");
                    }
                }
            }

            // Detectar si cambiaron origen, destino o paradas (o si no tenía distancia calculada)
            bool paradasCambiaron = ruta.Paradas.Count != dto.Paradas.Count
                || ruta.Paradas.OrderBy(p => p.Orden).Zip(dto.Paradas.OrderBy(p => p.Orden))
                    .Any(pair => pair.First.Orden != pair.Second.Orden || ClaveDireccion(pair.First.Direccion) != Clave(pair.Second.Direccion));

            bool recorridoCambio = ruta.DistanciaMetros == null
                || ruta.DireccionOrigen == null
                || ruta.DireccionDestino == null
                || ClaveDireccion(ruta.DireccionOrigen) != Clave(dto.DireccionOrigen)
                || ClaveDireccion(ruta.DireccionDestino) != Clave(dto.DireccionDestino)
                || paradasCambiaron;

            // Actualizar campos simples
            ruta.PesoMax = dto.PesoMax;
            ruta.CapacidadMaxPaquetes = dto.CapacidadMaxPaquetes;
            ruta.FechaInicio = dto.FechaInicio;
            ruta.FechaFin = dto.FechaFin;
            ruta.HoraSalida = dto.HoraSalida;
            ruta.HoraLlegada = dto.HoraLlegada;

            // Actualizar DireccionOrigen y DireccionDestino
            if (ruta.DireccionOrigen == null) {
                ruta.DireccionOrigen = new Direccion();
            }
            ruta.DireccionOrigen.Texto = dto.DireccionOrigen.Texto?.Trim() ?? string.Empty;
            ruta.DireccionOrigen.Latitud = dto.DireccionOrigen.Latitud;
            ruta.DireccionOrigen.Longitud = dto.DireccionOrigen.Longitud;

            if (ruta.DireccionDestino == null) {
                ruta.DireccionDestino = new Direccion();
            }
            ruta.DireccionDestino.Texto = dto.DireccionDestino.Texto?.Trim() ?? string.Empty;
            ruta.DireccionDestino.Latitud = dto.DireccionDestino.Latitud;
            ruta.DireccionDestino.Longitud = dto.DireccionDestino.Longitud;

            // Actualizar RutaDias (reemplazar elementos)
            ruta.RutaDias.Clear();
            foreach (var diaDto in dto.RutaDias) {
                ruta.RutaDias.Add(_mapper.Map<RutaDia>(diaDto));
            }

            // Actualizar Paradas (reemplazar elementos con sus direcciones y orden)
            ruta.Paradas.Clear();
            foreach (var paradaDto in dto.Paradas) {
                ruta.Paradas.Add(_mapper.Map<Parada>(paradaDto));
            }

            // Si cambiaron origen, destino o paradas (o si no tenía distancia calculada), recalcular
            if (recorridoCambio) {
                await CalcularRecorridoAsync(ruta, dto.DireccionOrigen, dto.DireccionDestino, dto.Paradas);
            }

            await _rutaRepositorio.GuardarCambiosAsync();
        }

        private void ValidarRuta(CrearRutaDTO rutaDTO) =>
            ValidarDatosRuta(rutaDTO.DireccionOrigen, rutaDTO.DireccionDestino, rutaDTO.RutaDias,
                rutaDTO.PesoMax, rutaDTO.CapacidadMaxPaquetes, rutaDTO.HoraSalida, rutaDTO.HoraLlegada,
                rutaDTO.FechaInicio, rutaDTO.FechaFin, rutaDTO.Paradas);

        private void ValidarRuta(EditarRutaDTO rutaDTO) =>
            ValidarDatosRuta(rutaDTO.DireccionOrigen, rutaDTO.DireccionDestino, rutaDTO.RutaDias,
                rutaDTO.PesoMax, rutaDTO.CapacidadMaxPaquetes, rutaDTO.HoraSalida, rutaDTO.HoraLlegada,
                rutaDTO.FechaInicio, rutaDTO.FechaFin, rutaDTO.Paradas);

        private void ValidarDatosRuta(DireccionDTO direccionOrigen, DireccionDTO direccionDestino,
            List<RutaDiaDTO> rutaDias, double pesoMax, int capacidadMaxPaquetes,
            TimeOnly horaSalida, TimeOnly horaLlegada, DateTime fechaInicio, DateTime? fechaFin,
            List<ParadaDTO> paradas) {

            if (direccionOrigen == null || direccionDestino == null)
                throw new ValidacionRutaException("El origen y el destino son obligatorios.");

            var origen = Clave(direccionOrigen);
            var destino = Clave(direccionDestino);

            if (origen == destino)
                throw new ValidacionRutaException("El origen y el destino deben ser diferentes.");

            if (rutaDias.Count == 0)
                throw new ValidacionRutaException("Debe seleccionar al menos un día.");

            if (pesoMax <= 0 || capacidadMaxPaquetes <= 0)
                throw new ValidacionRutaException("Peso y capacidad deben ser mayores que cero.");

            if (horaSalida >= horaLlegada)
                throw new ValidacionRutaException("La hora de salida debe ser anterior a la de llegada.");
            if (fechaFin < fechaInicio)
                throw new ValidacionRutaException("La fecha de fin no puede ser anterior a la de inicio.");

            var paradasVistas = new HashSet<(decimal, decimal)>();

            foreach (var p in paradas) {
                if (p.Direccion == null)
                    throw new ValidacionRutaException("La parada debe tener una dirección válida.");

                var clave = Clave(p.Direccion);
                if (clave == origen || clave == destino)
                    throw new ValidacionRutaException("La parada no puede ser igual a la direccion de origen o destino");

                if (!paradasVistas.Add(clave))
                    throw new ValidacionRutaException("No se pueden repetir paradas.");
            }
            var ordenes = paradas.Select(p => p.Orden).OrderBy(o => o).ToList();

            if (!ordenes.SequenceEqual(Enumerable.Range(1, paradas.Count)))
                throw new ValidacionRutaException("El orden de las paradas debe ser consecutivo, empezando en 1.");
        }

        private (decimal Lat, decimal Lng) Clave(DireccionDTO d)
            => (Math.Round(d.Latitud, 5), Math.Round(d.Longitud, 5));

        private (decimal Lat, decimal Lng) ClaveDireccion(Direccion d)
            => (Math.Round(d.Latitud, 5), Math.Round(d.Longitud, 5));

        private async Task CalcularRecorridoAsync(Ruta ruta, DireccionDTO direccionOrigen, DireccionDTO direccionDestino, List<ParadaDTO> paradas) {
            try {
                ResultadoRuta resultado = await _recorridoServicio.CalcularAsync(new RecorridoDTO {
                    Origen = direccionOrigen,
                    Destino = direccionDestino,
                    Paradas = paradas
                });

                ruta.DistanciaMetros = resultado.DistanciaMetros;
                ruta.DuracionSegundos = resultado.DuracionSegundos;
                ruta.GeometriaJson = resultado.GeometriaJson; 
            } catch (RutaNoEncontradaException) {
                // Si el servicio de rutas falla, la ruta se guarda igual sin distancia.
            }
        }
    }
}
