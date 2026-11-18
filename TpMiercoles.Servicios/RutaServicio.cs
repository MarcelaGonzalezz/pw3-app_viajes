using AutoMapper;

using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios {
    public interface IRutaServicio {
        Task CrearRutaAsync(int usuarioId, CrearRutaDTO rutaDTO);
        Task<List<RutaDTO>> ObtenerRutasActivasDeUnUsuarioAsync(int usuarioId);
        // ahora (la fecha y hora actual) se usa para marcar que rutas ya vencieron.
        Task<List<RutaDTO>> ObtenerRutasDeUnUsuarioAsync(int usuarioId, DateTime ahora);
        Task<RutaDTO> ObtenerDetalleAsync(int rutaId, int usuarioId, DateTime ahora);
        Task DesactivarRutaAsync(int rutaId, int usuarioId);
        // Una ruta vencida (ya paso su fecha hasta) no se puede reactivar.
        Task ActivarRutaAsync(int rutaId, int usuarioId, DateTime ahora);
        Task<EditarRutaDTO> ObtenerParaEditarAsync(int rutaId, int usuarioId);
        Task<ResultadoEdicionRutaDTO> EditarRutaAsync(int usuarioId, int rutaId, EditarRutaDTO dto);
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

        public async Task<RutaDTO> ObtenerDetalleAsync(int rutaId, int usuarioId, DateTime ahora) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para ver esta ruta.");
            // Rutas viejas (seed, o creadas con el mock) no tienen el trazado guardado:
            // lo calculamos ahora y lo guardamos para no repetir la consulta.
            if (string.IsNullOrWhiteSpace(ruta.GeometriaJson))
                await CompletarRecorridoFaltanteAsync(ruta);

            return MapearConVencimiento([ruta], ahora).Single();
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

        public async Task<List<RutaDTO>> ObtenerRutasDeUnUsuarioAsync(int usuarioId, DateTime ahora) {
            List<Ruta> rutas = await _rutaRepositorio.ObtenerRutasDeUnUsuarioAsync(usuarioId);
            return MapearConVencimiento(rutas, ahora);
        }

        // Mapea las rutas y marca cuales ya vencieron, para que la vista no ofrezca reactivarlas.
        private List<RutaDTO> MapearConVencimiento(List<Ruta> rutas, DateTime ahora) {
            return rutas
                .Select(ruta => {
                    RutaDTO dto = _mapper.Map<RutaDTO>(ruta);
                    dto.Vencida = Vencimiento.EstaVencida(ruta, ahora);
                    return dto;
                })
                .ToList();
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

        public async Task ActivarRutaAsync(int rutaId, int usuarioId, DateTime ahora) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdParaModificarAsync(rutaId);

            if (ruta == null)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            // Decision del grupo: lo vencido queda inactivo para siempre.
            if (Vencimiento.EstaVencida(ruta, ahora))
                throw new ValidacionRutaException(
                    "La ruta ya venció (pasó su fecha hasta): no se puede volver a activar.");

            ruta.Estado = EstadoRuta.ACTIVA;
            await _rutaRepositorio.GuardarCambiosAsync();
        }

        public async Task<EditarRutaDTO> ObtenerParaEditarAsync(int rutaId, int usuarioId) {
            Ruta? ruta = await _rutaRepositorio.ObtenerPorIdAsync(rutaId);

            if (ruta == null || ruta.Estado == EstadoRuta.ELIMINADA)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            return _mapper.Map<EditarRutaDTO>(ruta);
        }

        // Edita la "plantilla" de la ruta. Los viajes ya creados son copias independientes
        // (SnapshotDeRuta) y NO se modifican: los cambios aplican a los proximos viajes que se creen.
        public async Task<ResultadoEdicionRutaDTO> EditarRutaAsync(int usuarioId, int rutaId, EditarRutaDTO dto) {
            ValidarDatosRuta(dto.DireccionOrigen, dto.DireccionDestino, dto.RutaDias, dto.PesoMax,
                dto.CapacidadMaxPaquetes, dto.HoraSalida, dto.HoraLlegada, dto.FechaInicio, dto.FechaFin, dto.Paradas);

            Ruta? ruta = await _rutaRepositorio.ObtenerParaEditarAsync(rutaId);

            if (ruta == null || ruta.Estado == EstadoRuta.ELIMINADA)
                throw new RutaNoEncontradaException("La ruta no fue encontrada.");

            if (ruta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("No tienes permisos para modificar esta ruta.");

            // Viajes ya creados para hoy o mas adelante (activos o inactivos).
            List<Viaje> viajesCreados = ruta.Viajes
                .Where(v => v.FechaSalida.Date >= DateTime.Today)
                .OrderBy(v => v.FechaSalida)
                .ToList();

            // Un viaje se asocia a su salida por fecha y hora exactas. Si cambiara la hora de salida
            // con viajes ya creados, ese dia apareceria una segunda salida (y el materializador crearia otro viaje).
            if (dto.HoraSalida != ruta.HoraSalida && viajesCreados.Count > 0) {
                string fechas = string.Join(", ", viajesCreados.Select(v => v.FechaSalida.ToString("dd/MM/yyyy")));
                throw new ValidacionRutaException(
                    $"No se puede cambiar la hora de salida porque la ruta ya tiene viajes creados ({fechas}). " +
                    "Vas a poder cambiarla cuando esos viajes hayan pasado.");
            }

            ResultadoEdicionRutaDTO resultado = new();

            bool cambioCapacidad = dto.PesoMax != ruta.PesoMax || dto.CapacidadMaxPaquetes != ruta.CapacidadMaxPaquetes;
            int viajesConPaquetes = viajesCreados
                .Count(v => v.Asignaciones.Any(a => a.Estado != EstadoViaje.CANCELADO));

            if (cambioCapacidad && viajesConPaquetes > 0) {
                resultado.Avisos.Add(
                    $"Hay {viajesConPaquetes} viaje(s) con paquetes asignados que mantienen la capacidad y el peso anteriores. " +
                    "Los nuevos valores aplican a los próximos viajes.");
            } else if (viajesCreados.Count > 0) {
                resultado.Avisos.Add(
                    $"Los {viajesCreados.Count} viaje(s) ya creados mantienen los datos anteriores. " +
                    "Los cambios aplican a los próximos viajes.");
            }

            bool paradasCambiaron = ParadasCambiaron(ruta.Paradas, dto.Paradas);
            bool recorridoCambio = ruta.DistanciaMetros == null
                || !MismaDireccion(ruta.DireccionOrigen, dto.DireccionOrigen)
                || !MismaDireccion(ruta.DireccionDestino, dto.DireccionDestino)
                || paradasCambiaron;

            ruta.PesoMax = dto.PesoMax;
            ruta.CapacidadMaxPaquetes = dto.CapacidadMaxPaquetes;
            ruta.FechaInicio = dto.FechaInicio;
            ruta.FechaFin = dto.FechaFin;
            ruta.HoraSalida = dto.HoraSalida;
            ruta.HoraLlegada = dto.HoraLlegada;

            // Las direcciones se comparten con los viajes ya creados (el snapshot copia el Id),
            // por eso nunca se modifican: si cambian, se crea una direccion nueva.
            if (!MismaDireccion(ruta.DireccionOrigen, dto.DireccionOrigen))
                ruta.DireccionOrigen = _mapper.Map<Direccion>(dto.DireccionOrigen);

            if (!MismaDireccion(ruta.DireccionDestino, dto.DireccionDestino))
                ruta.DireccionDestino = _mapper.Map<Direccion>(dto.DireccionDestino);

            List<RutaDia> diasAnteriores = ruta.RutaDias.ToList();
            ruta.RutaDias.Clear();
            _rutaRepositorio.EliminarDias(diasAnteriores);
            foreach (RutaDiaDTO dia in dto.RutaDias)
                ruta.RutaDias.Add(_mapper.Map<RutaDia>(dia));

            if (paradasCambiaron) {
                List<Parada> paradasAnteriores = ruta.Paradas.ToList();
                ruta.Paradas.Clear();
                _rutaRepositorio.EliminarParadas(paradasAnteriores);
                foreach (ParadaDTO parada in dto.Paradas.OrderBy(p => p.Orden))
                    ruta.Paradas.Add(_mapper.Map<Parada>(parada));
            }

            if (recorridoCambio)
                await CalcularRecorridoAsync(ruta, dto.DireccionOrigen, dto.DireccionDestino, dto.Paradas);

            await _rutaRepositorio.GuardarCambiosAsync();
            return resultado;
        }

        private void ValidarRuta(CrearRutaDTO rutaDTO) =>
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

        private static bool MismaDireccion(Direccion? actual, DireccionDTO nueva)
            => actual != null
               && Math.Round(actual.Latitud, 5) == Math.Round(nueva.Latitud, 5)
               && Math.Round(actual.Longitud, 5) == Math.Round(nueva.Longitud, 5)
               && string.Equals(actual.Texto?.Trim(), nueva.Texto?.Trim(), StringComparison.Ordinal);

        private static bool ParadasCambiaron(List<Parada> actuales, List<ParadaDTO> nuevas)
            => actuales.Count != nuevas.Count
               || actuales.OrderBy(p => p.Orden).Zip(nuevas.OrderBy(p => p.Orden))
                   .Any(par => par.First.Orden != par.Second.Orden || !MismaDireccion(par.First.Direccion, par.Second.Direccion));
        private (decimal Lat, decimal Lng) Clave(DireccionDTO d)
    => (Math.Round(d.Latitud, 5), Math.Round(d.Longitud, 5));


        private async Task CalcularRecorridoAsync(Ruta ruta, DireccionDTO origen, DireccionDTO destino, List<ParadaDTO> paradas) {
            try {
                ResultadoRuta resultado = await _recorridoServicio.CalcularAsync(new RecorridoDTO {
                    Origen = origen,
                    Destino = destino,
                    Paradas = paradas
                });

                ruta.DistanciaMetros = resultado.DistanciaMetros;
                ruta.DuracionSegundos = resultado.DuracionSegundos;
                ruta.GeometriaJson = resultado.GeometriaJson; 
            } catch (RutaNoEncontradaException) {
                // Si el servicio de rutas falla, la ruta se guarda igual sin distancia.
            }
        }

        private async Task CompletarRecorridoFaltanteAsync(Ruta ruta) {
            try {
                ResultadoRuta resultado = await _recorridoServicio.CalcularAsync(new RecorridoDTO {
                    Origen = _mapper.Map<DireccionDTO>(ruta.DireccionOrigen),
                    Destino = _mapper.Map<DireccionDTO>(ruta.DireccionDestino),
                    Paradas = _mapper.Map<List<ParadaDTO>>(ruta.Paradas)
                });

                // El mock no devuelve trazado: no hay nada nuevo para guardar.
                if (string.IsNullOrWhiteSpace(resultado.GeometriaJson))
                    return;

                ruta.DistanciaMetros = resultado.DistanciaMetros;
                ruta.DuracionSegundos = resultado.DuracionSegundos;
                ruta.GeometriaJson = resultado.GeometriaJson;
                await _rutaRepositorio.GuardarCambiosAsync();
            } catch (RutaNoEncontradaException) {
                // Si ORS falla, mostramos el detalle igual (sin la línea) y se reintenta la próxima vez.
            }
        }
    }
}
