using TpMiercoles.Entidades;

namespace TpMiercoles.Tests.Helpers
{
    public static class TransporteTestData
    {
        public static readonly DateTime FechaSalida = new(2026, 11, 1, 8, 0, 0);

        public static Asignacion CrearTransporte(
            int id = 1,
            int conductorId = 5,
            int viajeId = 10,
            int envioId = 100,
            int remitenteId = 7,
            EstadoEnvio estadoEnvio = EstadoEnvio.ConductorAsignado,
            EstadoViaje estadoAsignacion = EstadoViaje.CONFIRMADO,
            string descripcion = "Caja de libros",
            double pesoKg = 3.5,
            TamanioEnvio tamanio = TamanioEnvio.Mediano,
            decimal monto = 4500m,
            string origen = "Liniers",
            string destino = "Once",
            string remitenteNombre = "Carla",
            string remitenteApellido = "Méndez",
            DateTime? fechaSalida = null,
            DateTime? fechaLlegada = null,
            DateTime? fechaAceptacion = null,
            DateTime? fechaRetiro = null,
            DateTime? fechaEntrega = null)
        {
            fechaSalida ??= FechaSalida;

            var remitente = new Usuario
            {
                Id = remitenteId,
                Nombre = remitenteNombre,
                Apellido = remitenteApellido,
                Email = $"remitente{remitenteId}@test.com",
                ContraseniaHash = "hash"
            };

            var envio = new Envio
            {
                Id = envioId,
                UsuarioId = remitente.Id,
                Usuario = remitente,
                DireccionRetiroId = envioId,
                DireccionEntregaId = envioId + 1,
                DireccionRetiro = new Direccion { Id = envioId, Texto = origen },
                DireccionEntrega = new Direccion { Id = envioId + 1, Texto = destino },
                FechaDesde = new DateTime(2026, 10, 28),
                FechaHasta = new DateTime(2026, 11, 5),
                Monto = monto,
                Estado = estadoEnvio,
                Paquete = new Paquete
                {
                    PesoKg = pesoKg,
                    Tamanio = tamanio,
                    Descripcion = descripcion
                }
            };

            var ruta = new Ruta
            {
                Id = viajeId,
                UsuarioId = conductorId,
                DireccionOrigen = new Direccion { Id = 1000 + viajeId, Texto = "Origen de ruta" },
                DireccionDestino = new Direccion { Id = 2000 + viajeId, Texto = "Destino de ruta" }
            };

            var viaje = new Viaje
            {
                Id = viajeId,
                RutaId = ruta.Id,
                Ruta = ruta,
                DireccionOrigenId = 3000 + viajeId,
                DireccionDestinoId = 4000 + viajeId,
                DireccionOrigen = new Direccion { Id = 3000 + viajeId, Texto = "Ramos Mejía" },
                DireccionDestino = new Direccion { Id = 4000 + viajeId, Texto = "Microcentro" },
                FechaSalida = fechaSalida.Value,
                FechaLlegada = fechaLlegada ?? fechaSalida.Value.AddMinutes(90),
                PesoMax = 25,
                CapacidadMaxPaquetes = 4,
                Estado = EstadoActividadViaje.ACTIVO
            };

            var asignacion = new Asignacion
            {
                Id = id,
                EnvioId = envioId,
                Envio = envio,
                ViajeId = viajeId,
                Viaje = viaje,
                Estado = estadoAsignacion,
                FechaAceptacion = fechaAceptacion,
                FechaRetiro = fechaRetiro,
                FechaEntrega = fechaEntrega
            };

            envio.Asignaciones.Add(asignacion);
            viaje.Asignaciones.Add(asignacion);

            return asignacion;
        }
    }
}
