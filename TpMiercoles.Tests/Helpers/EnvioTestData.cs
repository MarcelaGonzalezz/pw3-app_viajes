using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Tests.Helpers
{
    public static class EnvioTestData
    {
        public static Envio CrearEnvio(
            int id = 1,
            int usuarioId = 1,
            EstadoEnvio estado = EstadoEnvio.BuscandoConductor,
            decimal monto = 1500m,
            IEnumerable<Asignacion>? asignaciones = null)
            => new()
            {
                Id = id,
                UsuarioId = usuarioId,
                DireccionRetiroId = id * 2,
                DireccionEntregaId = id * 2 + 1,
                DireccionRetiro = ViajeTestData.CrearDireccion(id * 2, "Ramos Mejía"),
                DireccionEntrega = ViajeTestData.CrearDireccion(id * 2 + 1, "Ciudadela"),
                FechaDesde = new DateTime(2026, 3, 1),
                FechaHasta = new DateTime(2026, 3, 20),
                Paquete = new Paquete
                {
                    PesoKg = 2.5,
                    Tamanio = TamanioEnvio.Mediano,
                    Descripcion = "Zapatillas"
                },
                Monto = monto,
                Estado = estado,
                Asignaciones = asignaciones?.ToList() ?? []
            };

        /// <summary>
        /// Asignación con su viaje y el conductor (Viaje.Ruta.Usuario) ya cargados.
        /// </summary>
        public static Asignacion CrearAsignacionConConductor(
            int id,
            EstadoViaje estado = EstadoViaje.CONFIRMADO,
            string nombre = "Carlos",
            string apellido = "Gomez",
            string email = "carlos@mail.com",
            bool conViaje = true)
        {
            Viaje viaje = null!;

            if (conViaje)
            {
                viaje = ViajeTestData.CrearViaje(id: id * 10, usuarioId: 50 + id);
                viaje.Ruta.Usuario = new Usuario
                {
                    Id = 50 + id,
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    ContraseniaHash = "hash"
                };
            }

            return new Asignacion
            {
                Id = id,
                EnvioId = 1,
                ViajeId = id * 10,
                Viaje = viaje,
                Estado = estado,
                FechaAceptacion = new DateTime(2026, 3, 2),
                FechaRetiro = new DateTime(2026, 3, 3),
                FechaEntrega = new DateTime(2026, 3, 4)
            };
        }

        public static EnvioDTO CrearEnvioDTO(DateTime fechaHasta)
            => new()
            {
                Origen = new DireccionDTO { Texto = "Ramos Mejía", Latitud = -34.64m, Longitud = -58.56m },
                Destino = new DireccionDTO { Texto = "Ciudadela", Latitud = -34.63m, Longitud = -58.54m },
                FechaHasta = fechaHasta,
                Monto = 1500m,
                Paquete = new NuevoPaqueteDTO
                {
                    PesoKg = 2.5,
                    Tamanio = TamanioEnvio.Grande,
                    Descripcion = "Zapatillas"
                }
            };

        public static EnvioListadoDTO CrearListadoDTO(int id, EstadoEnvio estado)
            => new()
            {
                Id = id,
                Origen = "Ramos Mejía",
                Destino = "Ciudadela",
                FechaDesde = new DateTime(2026, 3, 1),
                FechaHasta = new DateTime(2026, 3, 20),
                PesoKg = 2.5,
                Tamanio = TamanioEnvio.Mediano,
                Descripcion = "Zapatillas",
                Monto = 1500m,
                Estado = estado
            };

        public static NuevoEnvioViewModel CrearNuevoEnvioViewModel()
            => new()
            {
                Origen = new DireccionViewModel { Texto = "Ramos Mejía", Latitud = -34.64m, Longitud = -58.56m },
                Destino = new DireccionViewModel { Texto = "Ciudadela", Latitud = -34.63m, Longitud = -58.54m },
                FechaHasta = new DateTime(2026, 3, 20),
                Monto = 1500m,
                Paquete = new NuevoPaqueteViewModel
                {
                    PesoKg = 2.5,
                    Tamanio = TamanioEnvio.Grande,
                    Descripcion = "Zapatillas"
                }
            };
    }
}
