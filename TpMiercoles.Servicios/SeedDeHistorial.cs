using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface ISeedDeHistorial
    {
        Task SembrarAsync();
    }

    // Datos de ejemplo para la pantalla Historial. Corre aparte de SeedDeDesarrollo porque
    // ese solo siembra cuando no hay ningun viaje: si la base ya tenia datos, nunca llegaria aca.
    // Se apoya en los usuarios que crea SeedDeDesarrollo (conductor, otro y cliente).
    // Es idempotente: si el conductor ya tiene viajes pasados, no hace nada.
    public class SeedDeHistorial : ISeedDeHistorial
    {
        private readonly IUsuarioRepositorio _usuarios;
        private readonly IViajeRepositorio _viajes;
        private readonly IEnvioRepositorio _envios;
        private readonly IHistorialRepositorio _historial;
        private readonly ILogger<SeedDeHistorial> _registro;

        public SeedDeHistorial(
            IUsuarioRepositorio usuarios,
            IViajeRepositorio viajes,
            IEnvioRepositorio envios,
            IHistorialRepositorio historial,
            ILogger<SeedDeHistorial> registro)
        {
            _usuarios = usuarios;
            _viajes = viajes;
            _envios = envios;
            _historial = historial;
            _registro = registro;
        }

        public async Task SembrarAsync()
        {
            try
            {
                Usuario? ana = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailConductor);
                Usuario? bruno = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailOtroConductor);
                Usuario? carla = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailCliente);

                if (ana is null || bruno is null || carla is null)
                {
                    _registro.LogInformation(
                        "No se sembro el historial: faltan los usuarios de prueba (conductor, otro y cliente).");
                    return;
                }

                DateTime hoy = DateTime.Today;

                List<Viaje> yaRealizados = await _historial.ListarViajesRealizadosAsync(ana.Id, DateTime.Now);
                if (yaRealizados.Count > 0)
                {
                    return;
                }

                // ---- Lugares ----
                Direccion ramos = Dir("Av. de Mayo 1200, Ramos Mejia", -34.640000m, -58.563000m);
                Direccion centro = Dir("Av. Corrientes 800, Microcentro", -34.603700m, -58.381600m);
                Direccion ciudadela = Dir("Av. Rivadavia 100, Ciudadela", -34.636000m, -58.535000m);
                Direccion liniers = Dir("Av. Rivadavia 11500, Liniers", -34.642000m, -58.524000m);
                Direccion caballito = Dir("Av. Acoyte 300, Caballito", -34.618000m, -58.440000m);
                Direccion once = Dir("Av. Pueyrredon 200, Once", -34.609000m, -58.405000m);
                Direccion avellaneda = Dir("Av. Mitre 500, Avellaneda", -34.662000m, -58.365000m);
                Direccion palermo = Dir("Av. Santa Fe 3200, Palermo", -34.588000m, -58.410000m);

                // ---- Viajes de Ana (conductora) ----
                Ruta rutaAna = new()
                {
                    UsuarioId = ana.Id,
                    DireccionOrigen = ramos,
                    DireccionDestino = centro,
                    PesoMax = 25,
                    CapacidadMaxPaquetes = 4,
                    FechaInicio = hoy.AddMonths(-3),
                    FechaFin = null,
                    HoraSalida = new TimeOnly(8, 0),
                    HoraLlegada = new TimeOnly(9, 30),
                    Estado = EstadoRuta.ACTIVA,
                    RutaDias =
                    {
                        new RutaDia { DiaSemana = DayOfWeek.Monday },
                        new RutaDia { DiaSemana = DayOfWeek.Wednesday },
                        new RutaDia { DiaSemana = DayOfWeek.Friday }
                    }
                };

                // V1: 3 paquetes entregados = $12.000
                Viaje v1 = CrearViaje(rutaAna, ramos, centro, hoy.AddDays(-45));
                Entregar(v1, carla.Id, liniers, once, "Zapatillas", 4000m, 2);
                Entregar(v1, carla.Id, ciudadela, centro, "Documentos", 5500m, 1);
                Entregar(v1, carla.Id, liniers, centro, "Repuesto", 2500m, 1);

                // V2: 2 paquetes entregados = $10.500
                Viaje v2 = CrearViaje(rutaAna, ramos, centro, hoy.AddDays(-38));
                Entregar(v2, carla.Id, ciudadela, once, "Caja de libros", 7000m, 8);
                Entregar(v2, carla.Id, liniers, caballito, "Campera", 3500m, 2);

                // V3: 1 entregado ($4.000) + una asignacion cancelada que NO suma.
                Viaje v3 = CrearViaje(rutaAna, ramos, centro, hoy.AddDays(-21));
                Entregar(v3, carla.Id, liniers, once, "Auriculares", 4000m, 1);
                v3.Asignaciones.Add(new Asignacion
                {
                    Envio = CrearEnvio(carla.Id, ciudadela, caballito, "Mate y termo", 3000m, 2,
                                       EstadoEnvio.BuscandoConductor, hoy.AddDays(-23), hoy.AddDays(-19)),
                    Estado = EstadoViaje.CANCELADO,
                    FechaAceptacion = hoy.AddDays(-22)
                });

                // V4: 3 paquetes entregados = $11.500
                Viaje v4 = CrearViaje(rutaAna, ramos, centro, hoy.AddDays(-9));
                Entregar(v4, carla.Id, liniers, once, "Mochila", 5500m, 3);
                Entregar(v4, carla.Id, ciudadela, centro, "Sobre con contrato", 3500m, 1);
                Entregar(v4, carla.Id, liniers, caballito, "Cargador", 2500m, 1);

                // V5: salio hace 2 dias pero su unico paquete sigue en camino.
                // No es "realizado" ni suma ganancia: no debe aparecer en el historial.
                Viaje v5 = CrearViaje(rutaAna, ramos, centro, hoy.AddDays(-2));
                v5.Asignaciones.Add(new Asignacion
                {
                    Envio = CrearEnvio(carla.Id, liniers, once, "Tablet", 6000m, 1,
                                       EstadoEnvio.EnCamino, hoy.AddDays(-4), hoy.AddDays(2)),
                    Estado = EstadoViaje.CONFIRMADO,
                    FechaAceptacion = hoy.AddDays(-3),
                    FechaRetiro = v5.FechaSalida.AddMinutes(20)
                });

                // ---- Viaje de Bruno (otro conductor) ----
                // Lleva un envio de Ana: aparece en el historial de Ana como "envio entregado"
                // pero el viaje NO debe aparecer en su historial (es de Bruno).
                Ruta rutaBruno = new()
                {
                    UsuarioId = bruno.Id,
                    DireccionOrigen = avellaneda,
                    DireccionDestino = palermo,
                    PesoMax = 15,
                    CapacidadMaxPaquetes = 3,
                    FechaInicio = hoy.AddMonths(-2),
                    HoraSalida = new TimeOnly(9, 0),
                    HoraLlegada = new TimeOnly(10, 0),
                    Estado = EstadoRuta.ACTIVA,
                    RutaDias = { new RutaDia { DiaSemana = DayOfWeek.Thursday } }
                };

                Viaje vBruno = CrearViaje(rutaBruno, avellaneda, palermo, hoy.AddDays(-14));
                Entregar(vBruno, ana.Id, avellaneda, palermo, "Repuesto", 2500m, 1);
                Entregar(vBruno, carla.Id, avellaneda, palermo, "Libro", 1800m, 1);

                await _viajes.AgregarAsync([v1, v2, v3, v4, v5, vBruno]);

                // ---- Envio de Ana que cancelo antes de que lo tomaran ----
                await _envios.CrearAsync(CrearEnvio(ana.Id, caballito, once, "Documentos", 3500m, 1,
                                                    EstadoEnvio.Cancelado, hoy.AddDays(-30), hoy.AddDays(-25)));

                _registro.LogInformation(
                    "Historial de prueba cargado para {Email} (4 viajes realizados, $38.000 de ganancia efectiva). " +
                    "Entra a /historial. Usuario: {Email} / {Contrasenia}",
                    SeedDeDesarrollo.EmailConductor, SeedDeDesarrollo.EmailConductor, SeedDeDesarrollo.ContraseniaDeTodos);
            }
            catch (DbUpdateException ex)
            {
                _registro.LogWarning(ex,
                    "No se pudieron guardar los datos del historial porque la base no coincide con el modelo actual. " +
                    "Corre Update-Database y vuelve a arrancar la app.");
            }
            catch (Exception ex)
            {
                _registro.LogWarning(ex, "No se pudo sembrar el historial de prueba.");
            }
        }

        private static Direccion Dir(string texto, decimal latitud, decimal longitud)
            => new() { Texto = texto, Latitud = latitud, Longitud = longitud };

        private static Viaje CrearViaje(Ruta ruta, Direccion origen, Direccion destino, DateTime dia)
            => new()
            {
                Ruta = ruta,
                DireccionOrigen = origen,
                DireccionDestino = destino,
                PesoMax = ruta.PesoMax,
                CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
                FechaSalida = dia.AddHours(8),
                FechaLlegada = dia.AddHours(9).AddMinutes(30),
                Estado = EstadoActividadViaje.ACTIVO
            };

        private static Envio CrearEnvio(
            int remitenteId, Direccion retiro, Direccion entrega, string descripcion,
            decimal monto, double pesoKg, EstadoEnvio estado, DateTime desde, DateTime hasta)
            => new()
            {
                UsuarioId = remitenteId,
                DireccionRetiro = retiro,
                DireccionEntrega = entrega,
                FechaDesde = desde,
                FechaHasta = hasta,
                Monto = monto,
                Estado = estado,
                Paquete = new Paquete
                {
                    PesoKg = pesoKg,
                    Tamanio = pesoKg <= 2 ? TamanioEnvio.Pequenio : pesoKg <= 6 ? TamanioEnvio.Mediano : TamanioEnvio.Grande,
                    Descripcion = descripcion
                }
            };

        // Agrega al viaje una asignacion ya completada: retirada y entregada durante el viaje.
        private static void Entregar(
            Viaje viaje, int remitenteId, Direccion retiro, Direccion entrega,
            string descripcion, decimal monto, double pesoKg)
        {
            Envio envio = CrearEnvio(remitenteId, retiro, entrega, descripcion, monto, pesoKg,
                                     EstadoEnvio.Entregado,
                                     viaje.FechaSalida.AddDays(-5), viaje.FechaSalida.AddDays(2));

            viaje.Asignaciones.Add(new Asignacion
            {
                Envio = envio,
                Estado = EstadoViaje.CONFIRMADO,
                FechaAceptacion = viaje.FechaSalida.AddDays(-3),
                FechaRetiro = viaje.FechaSalida.AddMinutes(15),
                FechaEntrega = viaje.FechaLlegada ?? viaje.FechaSalida.AddHours(1)
            });
        }
    }
}
