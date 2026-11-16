using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface ISeedDeTransportes
    {
        Task SembrarAsync();
    }

    // Datos para probar "Mis transportes": un viaje de Ana para hoy con tres paquetes de Carla,
    // uno en cada paso del ciclo (asignado, retirado, en camino). Ana puede avanzarlos hasta
    // entregado. Va en un viaje propio para no tocar el viaje que usa el seed de oportunidades.
    // Es idempotente: si ya existe el paquete "Campera de abrigo" de Carla, no hace nada.
    public class SeedDeTransportes : ISeedDeTransportes
    {
        private readonly IUsuarioRepositorio _usuarios;
        private readonly IRutaRepositorio _rutas;
        private readonly IViajeRepositorio _viajes;
        private readonly IEnvioRepositorio _envios;
        private readonly ILogger<SeedDeTransportes> _logger;

        public SeedDeTransportes(
            IUsuarioRepositorio usuarios,
            IRutaRepositorio rutas,
            IViajeRepositorio viajes,
            IEnvioRepositorio envios,
            ILogger<SeedDeTransportes> logger)
        {
            _usuarios = usuarios;
            _rutas = rutas;
            _viajes = viajes;
            _envios = envios;
            _logger = logger;
        }

        public async Task SembrarAsync()
        {
            try
            {
                Usuario? ana = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailConductor);
                Usuario? carla = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailCliente);

                if (ana is null || carla is null)
                {
                    return;
                }

                List<Envio> yaAsignados = await _envios.ListarPorUsuarioAsync(carla.Id, EstadoEnvio.ConductorAsignado);
                if (yaAsignados.Any(e => e.Paquete.Descripcion == "Campera de abrigo"))
                {
                    return;
                }

                Ruta? ruta = (await _rutas.ObtenerRutasActivasDeUnUsuarioAsync(ana.Id))
                    .OrderBy(r => r.Id)
                    .FirstOrDefault();

                if (ruta is null)
                {
                    return;
                }

                DateTime hoy = DateTime.Today;

                Viaje viaje = new()
                {
                    RutaId = ruta.Id,
                    DireccionOrigenId = ruta.DireccionOrigenId,
                    DireccionDestinoId = ruta.DireccionDestinoId,
                    PesoMax = ruta.PesoMax,
                    CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
                    FechaSalida = hoy.AddHours(8),
                    FechaLlegada = hoy.AddHours(9).AddMinutes(30),
                    Estado = EstadoActividadViaje.ACTIVO
                };

                viaje.Asignaciones.Add(Asignar(carla, "Campera de abrigo", 3500m, 2,
                    "Av. Rivadavia 100, Ciudadela", "Av. Pueyrredon 200, Once",
                    EstadoEnvio.ConductorAsignado, hoy));
                viaje.Asignaciones.Add(Asignar(carla, "Cargador portatil", 2500m, 1,
                    "Av. Rivadavia 11500, Liniers", "Av. Corrientes 800, Microcentro",
                    EstadoEnvio.Retirado, hoy));
                viaje.Asignaciones.Add(Asignar(carla, "Libro de cocina", 3000m, 1,
                    "Av. Acoyte 300, Caballito", "Av. Santa Fe 3200, Palermo",
                    EstadoEnvio.EnCamino, hoy));

                // Viaje de hace dos semanas, ya finalizado (inactivo) con un paquete entregado:
                // aparece en Mis viajes (inactivos), en Mis transportes (entregado) y en el Historial.
                DateTime antes = hoy.AddDays(-14);
                Viaje viajePasado = new()
                {
                    RutaId = ruta.Id,
                    DireccionOrigenId = ruta.DireccionOrigenId,
                    DireccionDestinoId = ruta.DireccionDestinoId,
                    PesoMax = ruta.PesoMax,
                    CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
                    FechaSalida = antes.AddHours(8),
                    FechaLlegada = antes.AddHours(9).AddMinutes(30),
                    Estado = EstadoActividadViaje.INACTIVO
                };
                viajePasado.Asignaciones.Add(Asignar(carla, "Auriculares inalambricos", 4000m, 1,
                    "Av. Rivadavia 11500, Liniers", "Av. Pueyrredon 200, Once",
                    EstadoEnvio.Entregado, antes));

                await _viajes.AgregarAsync([viaje, viajePasado]);

                _logger.LogInformation(
                    "Transportes de prueba cargados para {Email}: 3 paquetes en distintos estados y un viaje pasado con un paquete entregado. Entra a /transportes.",
                    SeedDeDesarrollo.EmailConductor);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "No se pudieron guardar los transportes de prueba: la base no coincide con el modelo.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron sembrar los transportes de prueba.");
            }
        }

        // Asignacion con el paquete ya en el estado pedido y las fechas coherentes con ese estado.
        private static Asignacion Asignar(
            Usuario remitente, string descripcion, decimal monto, double pesoKg,
            string retiro, string entrega, EstadoEnvio estado, DateTime hoy)
        {
            Envio envio = new()
            {
                UsuarioId = remitente.Id,
                DireccionRetiro = new Direccion { Texto = retiro, Latitud = -34.62m, Longitud = -58.45m },
                DireccionEntrega = new Direccion { Texto = entrega, Latitud = -34.60m, Longitud = -58.40m },
                FechaDesde = hoy.AddDays(-3),
                FechaHasta = hoy.AddDays(5),
                Monto = monto,
                Estado = estado,
                Paquete = new Paquete
                {
                    PesoKg = pesoKg,
                    Tamanio = pesoKg <= 2 ? TamanioEnvio.Pequenio : TamanioEnvio.Mediano,
                    Descripcion = descripcion
                }
            };

            return new Asignacion
            {
                Envio = envio,
                Estado = EstadoViaje.CONFIRMADO,
                FechaAceptacion = hoy.AddDays(-1),
                FechaRetiro = estado is EstadoEnvio.Retirado or EstadoEnvio.EnCamino or EstadoEnvio.Entregado
                    ? hoy.AddHours(8).AddMinutes(20) : null,
                FechaEntrega = estado == EstadoEnvio.Entregado ? hoy.AddHours(9).AddMinutes(10) : null
            };
        }
    }
}
