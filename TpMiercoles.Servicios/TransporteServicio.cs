using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios
{
    public interface ITransporteServicio
    {
        // Paquetes que el conductor acepto llevar. Si estado es null trae todos.
        Task<List<TransporteListadoDTO>> ListarAsync(int conductorId, EstadoEnvio? estado);

        // Devuelve null si la asignacion no existe o si el viaje no es del conductor.
        Task<TransporteDetalleDTO?> ObtenerDetalleAsync(int asignacionId, int conductorId);

        // Mueve el transporte al siguiente estado. El estado vive en el Envio, que
        // es el mismo que ve el remitente en "Mis envios": avanzar aca actualiza
        // las dos pantallas.
        Task<ResultadoOperacionTransporte> AvanzarAsync(int asignacionId, int conductorId, EstadoEnvio destino);
    }

    public class TransporteServicio : ITransporteServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        // Ciclo de vida de un transporte. Cada paso solo se puede dar desde el
        // anterior: no se puede entregar un paquete que no fue retirado, ni
        // volver atras. Lo que no esta en esta tabla no es una transicion valida.
        private static readonly Dictionary<EstadoEnvio, EstadoEnvio> EstadoPrevioRequerido = new()
        {
            [EstadoEnvio.Retirado] = EstadoEnvio.ConductorAsignado,
            [EstadoEnvio.EnCamino] = EstadoEnvio.Retirado,
            [EstadoEnvio.Entregado] = EstadoEnvio.EnCamino
        };

        private readonly ITransporteRepositorio _repositorio;
        private readonly TimeProvider _reloj;

        public TransporteServicio(ITransporteRepositorio repositorio, TimeProvider reloj)
        {
            _repositorio = repositorio;
            _reloj = reloj;
        }

        public async Task<List<TransporteListadoDTO>> ListarAsync(int conductorId, EstadoEnvio? estado)
        {
            List<Asignacion> asignaciones = await _repositorio.ListarPorConductorAsync(conductorId, estado);
            return asignaciones.Select(ArmarListado).ToList();
        }

        public async Task<TransporteDetalleDTO?> ObtenerDetalleAsync(int asignacionId, int conductorId)
        {
            Asignacion? asignacion = await _repositorio.ObtenerDetalleAsync(asignacionId);

            // Las asignaciones canceladas no estan en el listado, asi que tampoco
            // tienen detalle: las dos pantallas muestran lo mismo.
            if (!EsDelConductor(asignacion, conductorId) || asignacion!.Estado == EstadoViaje.CANCELADO)
            {
                return null;
            }

            return ArmarDetalle(asignacion);
        }

        public async Task<ResultadoOperacionTransporte> AvanzarAsync(
            int asignacionId, int conductorId, EstadoEnvio destino)
        {
            Asignacion? asignacion = await _repositorio.ObtenerParaModificarAsync(asignacionId);

            // No existe o es de otro conductor: contestamos lo mismo en los dos
            // casos para no delatar que el transporte existe.
            if (!EsDelConductor(asignacion, conductorId))
            {
                return ResultadoOperacionTransporte.NoExiste();
            }

            Envio envio = asignacion!.Envio;

            if (asignacion.Estado == EstadoViaje.CANCELADO || envio.Estado == EstadoEnvio.Cancelado)
            {
                return ResultadoOperacionTransporte.EnConflicto(
                    "El envío fue cancelado: ya no se puede avanzar el transporte.");
            }

            if (!EstadoPrevioRequerido.TryGetValue(destino, out EstadoEnvio previoRequerido))
            {
                return ResultadoOperacionTransporte.EnConflicto(
                    $"{Texto(destino)} no es un paso válido del transporte.");
            }

            if (envio.Estado != previoRequerido)
            {
                return ResultadoOperacionTransporte.EnConflicto(MotivoDelRechazo(envio.Estado, destino));
            }

            DateTime ahora = Ahora();
            envio.Estado = destino;

            if (destino == EstadoEnvio.Retirado)
            {
                asignacion.FechaRetiro = ahora;
            }
            else if (destino == EstadoEnvio.Entregado)
            {
                asignacion.FechaEntrega = ahora;
            }

            await _repositorio.GuardarCambiosAsync();

            return ResultadoOperacionTransporte.Exitoso(ArmarListado(asignacion));
        }

        // Fecha y hora de Argentina, sin depender de la zona horaria del servidor.
        private DateTime Ahora() =>
            TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), ZonaArgentina).DateTime;

        private static bool EsDelConductor(Asignacion? asignacion, int conductorId)
            => asignacion?.Viaje?.Ruta is not null && asignacion.Viaje.Ruta.UsuarioId == conductorId;

        private static TransporteListadoDTO ArmarListado(Asignacion asignacion)
            => Completar(new TransporteListadoDTO(), asignacion);

        private static TransporteDetalleDTO ArmarDetalle(Asignacion asignacion)
        {
            var dto = Completar(new TransporteDetalleDTO(), asignacion);

            dto.RemitenteEmail = asignacion.Envio.Usuario?.Email;
            dto.ViajeOrigen = asignacion.Viaje.DireccionOrigen?.Texto ?? string.Empty;
            dto.ViajeDestino = asignacion.Viaje.DireccionDestino?.Texto ?? string.Empty;
            dto.FechaLlegadaViaje = asignacion.Viaje.FechaLlegada;
            dto.FechaAceptacion = asignacion.FechaAceptacion;
            dto.FechaRetiro = asignacion.FechaRetiro;
            dto.FechaEntrega = asignacion.FechaEntrega;

            return dto;
        }

        private static T Completar<T>(T dto, Asignacion asignacion) where T : TransporteListadoDTO
        {
            Envio envio = asignacion.Envio;
            Usuario? remitente = envio.Usuario;

            dto.AsignacionId = asignacion.Id;
            dto.ViajeId = asignacion.ViajeId;
            dto.EnvioId = asignacion.EnvioId;
            dto.Descripcion = envio.Paquete.Descripcion;
            dto.PesoKg = envio.Paquete.PesoKg;
            dto.Tamanio = envio.Paquete.Tamanio;
            dto.Origen = envio.DireccionRetiro?.Texto ?? string.Empty;
            dto.Destino = envio.DireccionEntrega?.Texto ?? string.Empty;
            dto.Ganancia = envio.Monto;
            dto.Estado = envio.Estado;
            dto.RemitenteNombre = remitente is null
                ? string.Empty
                : $"{remitente.Nombre} {remitente.Apellido}".Trim();
            dto.FechaSalidaViaje = asignacion.Viaje.FechaSalida;
            dto.ProximoEstado = ProximoEstadoDe(envio.Estado);

            return dto;
        }

        // El siguiente paso del ciclo, o null si ya termino o esta cancelado.
        public static EstadoEnvio? ProximoEstadoDe(EstadoEnvio estadoActual)
        {
            foreach (var paso in EstadoPrevioRequerido)
            {
                if (paso.Value == estadoActual)
                {
                    return paso.Key;
                }
            }

            return null;
        }

        private static string MotivoDelRechazo(EstadoEnvio actual, EstadoEnvio destino)
        {
            if (destino == EstadoEnvio.Entregado && actual == EstadoEnvio.ConductorAsignado)
            {
                return "No se puede marcar como entregado un paquete que todavía no fue retirado.";
            }

            if (actual == EstadoEnvio.Entregado)
            {
                return "El paquete ya fue entregado.";
            }

            return $"El paquete está {Texto(actual).ToLowerInvariant()}: no corresponde marcarlo como " +
                   $"{Texto(destino).ToLowerInvariant()}.";
        }

        private static string Texto(EstadoEnvio estado) => estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.EnCamino => "En camino",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            _ => estado.ToString()
        };
    }
}
