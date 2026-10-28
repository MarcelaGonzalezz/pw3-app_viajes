using TpMiercoles.Entidades;

namespace TpMiercoles.Tests.Helpers
{
    public static class ViajeTestData
    {
        public static readonly DateTime FechaSalida = new(2026, 11, 1, 8, 0, 0);
        public static readonly DateTime FechaLLegada = FechaSalida.AddMinutes(90);

        public static Direccion CrearDireccion(int id, string texto)
            => new() { Id = id, Texto = texto };

        public static RutaDia CrearDia(int id, DayOfWeek dia)
            => new() { Id = id, DiaSemana = dia };

        public static Parada CrearParada(int id, string texto)
            => new()
            {
                Id = id,
                DireccionId = id,
                Direccion = CrearDireccion(id, texto)
            };

        public static Asignacion CrearAsignacion(
            int id,
            int envioId = 100,
            string nombreRemitente = "Carla",
            string apellidoRemitente = "Méndez",
            string direccionRetiro = "Liniers",
            string direccionEntrega = "Once",
            EstadoViaje estado = EstadoViaje.PENDIENTE,
            DateTime? fechaAceptacion = null,
            DateTime? fechaRetiro = null,
            DateTime? fechaEntrega = null)
        {
            var usuario = new Usuario
            {
                Id = envioId,
                Nombre = nombreRemitente,
                Apellido = apellidoRemitente,
                Email = $"remitente{envioId}@test.com",
                ContraseniaHash = "hash"
            };

            var envio = new Envio
            {
                Id = envioId,
                UsuarioId = usuario.Id,
                Usuario = usuario,
                DireccionRetiroId = id,
                DireccionEntregaId = id + 1,
                DireccionRetiro = CrearDireccion(id, direccionRetiro),
                DireccionEntrega = CrearDireccion(id + 1, direccionEntrega),
                FechaDesde = new DateTime(2026, 10, 28),
                FechaHasta = new DateTime(2026, 11, 5)
            };

            return new Asignacion
            {
                Id = id,
                EnvioId = envioId,
                Envio = envio,
                Estado = estado,
                FechaAceptacion = fechaAceptacion,
                FechaRetiro = fechaRetiro,
                FechaEntrega = fechaEntrega
            };
        }

        public static Viaje CrearViaje(
            int id = 1,
            int usuarioId = 1,
            EstadoActividadViaje estado = EstadoActividadViaje.ACTIVO,
            DateTime? fechaSalida = null,
            DateTime? fechaLlegada = null,
            double pesoMax = 25,
            int capacidadMaxPaquetes = 4,
            IEnumerable<Asignacion>? asignaciones = null,
            IEnumerable<Parada>? paradas = null,
            IEnumerable<RutaDia>? dias = null)
        {
            fechaSalida ??= FechaSalida;
            fechaLlegada ??= fechaSalida.Value.AddMinutes(90);

            var ruta = new Ruta
            {
                Id = id,
                UsuarioId = usuarioId,
                DireccionOrigen = CrearDireccion(1000 + id, "Origen de ruta"),
                DireccionDestino = CrearDireccion(2000 + id, "Destino de ruta"),
                RutaDias = dias?.ToList() ?? [],
                Paradas = paradas?.ToList() ?? []
            };

            return new Viaje
            {
                Id = id,
                RutaId = ruta.Id,
                Ruta = ruta,
                DireccionOrigenId = 3000 + id,
                DireccionDestinoId = 4000 + id,
                DireccionOrigen = CrearDireccion(3000 + id, "Ramos Mejía"),
                DireccionDestino = CrearDireccion(4000 + id, "Microcentro"),
                FechaSalida = fechaSalida.Value,
                FechaLlegada = fechaLlegada,
                PesoMax = pesoMax,
                CapacidadMaxPaquetes = capacidadMaxPaquetes,
                Estado = estado,
                Asignaciones = asignaciones?.ToList() ?? []
            };
        }
    }
}
