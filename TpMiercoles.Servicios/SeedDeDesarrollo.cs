using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface ISeedDeDesarrollo
    {
        Task SembrarAsync();
    }

    // Datos de ejemplo para desarrollo. Se cargan una sola vez, cuando la tabla
    // de viajes esta vacia, para poder probar las pantallas sin cargar nada a mano.
    // Son datos descartables: si la base se ensucia, se borra, se corre
    // Update-Database y al arrancar se vuelven a sembrar.
    public class SeedDeDesarrollo : ISeedDeDesarrollo
    {
        public const string EmailConductor = "conductor@ruteaya.test";
        public const string EmailOtroConductor = "otro@ruteaya.test";
        public const string EmailCliente = "cliente@ruteaya.test";
        public const string ContraseniaDeTodos = "Ruteaya123!";

        private readonly IViajeRepositorio _repositorio;
        private readonly IPasswordHasher<Usuario> _hasher;
        private readonly ILogger<SeedDeDesarrollo> _registro;

        public SeedDeDesarrollo(
            IViajeRepositorio repositorio,
            IPasswordHasher<Usuario> hasher,
            ILogger<SeedDeDesarrollo> registro)
        {
            _repositorio = repositorio;
            _hasher = hasher;
            _registro = registro;
        }

        public async Task SembrarAsync()
        {
            try
            {
                if (await _repositorio.ExisteAlgunoAsync())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _registro.LogWarning(ex,
                    "No se pudo leer la base para sembrar datos de prueba. " +
                    "Corre Update-Database y volve a arrancar la app.");
                return;
            }

            DateTime hoy = DateTime.Today;

            Usuario conductor = CrearUsuario("Ana", "Duarte", EmailConductor);
            Usuario otroConductor = CrearUsuario("Bruno", "Ferrari", EmailOtroConductor);
            Usuario cliente = CrearUsuario("Carla", "Mendez", EmailCliente);

            Direccion ramos = CrearDireccion("Av. de Mayo 1200, Ramos Mejia", -34.640000m, -58.563000m);
            Direccion ciudadela = CrearDireccion("Av. Rivadavia 100, Ciudadela", -34.636000m, -58.535000m);
            Direccion liniers = CrearDireccion("Av. Rivadavia 11500, Liniers", -34.642000m, -58.524000m);
            Direccion caballito = CrearDireccion("Av. Acoyte 300, Caballito", -34.618000m, -58.440000m);
            Direccion once = CrearDireccion("Av. Pueyrredon 200, Once", -34.609000m, -58.405000m);
            Direccion centro = CrearDireccion("Av. Corrientes 800, Microcentro", -34.603700m, -58.381600m);

            // Ruta del conductor de prueba: oeste -> centro, lunes, miercoles y viernes,
            // con dos paradas intermedias.
            Ruta rutaAlCentro = new()
            {
                Usuario = conductor,
                DireccionOrigen = ramos,
                DireccionDestino = centro,
                PesoMax = 25,
                CapacidadMaxPaquetes = 4,
                FechaInicio = hoy,
                FechaFin = hoy.AddMonths(3),
                RutaDias =
                {
                    new RutaDia { DiaSemana = DayOfWeek.Monday },
                    new RutaDia { DiaSemana = DayOfWeek.Wednesday },
                    new RutaDia { DiaSemana = DayOfWeek.Friday }
                },
                Paradas =
                {
                    new Parada { Direccion = ciudadela },
                    new Parada { Direccion = liniers }
                }
            };

            // Viaje ACTIVO con dos paquetes asignados: es el que sirve para ver
            // la pantalla de detalle completa.
            Viaje viajeConPaquetes = new()
            {
                Ruta = rutaAlCentro,
                DireccionOrigen = ramos,
                DireccionDestino = centro,
                PesoMax = 25,
                CapacidadMaxPaquetes = 4,
                FechaSalida = hoy.AddDays(1).AddHours(8),
                FechaLlegada = hoy.AddDays(1).AddHours(9).AddMinutes(30),
                Estado = EstadoActividadViaje.ACTIVO
            };

            // Los tres envios quedan en un paso distinto del transporte, para poder
            // probar la pantalla de Mis transportes con todos los estados y con
            // los filtros.
            Envio envioDeLibros = new()
            {
                Usuario = cliente,
                DireccionRetiro = liniers,
                DireccionEntrega = once,
                FechaDesde = hoy,
                FechaHasta = hoy.AddDays(3),
                Monto = 4500m,
                Estado = EstadoEnvio.ConductorAsignado,
                Paquete = new Paquete
                {
                    Descripcion = "Caja de libros",
                    PesoKg = 3.5,
                    Tamanio = TamanioEnvio.Mediano
                }
            };

            Envio envioDeRopa = new()
            {
                Usuario = cliente,
                DireccionRetiro = ciudadela,
                DireccionEntrega = centro,
                FechaDesde = hoy.AddDays(1),
                FechaHasta = hoy.AddDays(5),
                Monto = 2800m,
                Estado = EstadoEnvio.Retirado,
                Paquete = new Paquete
                {
                    Descripcion = "Bolso de ropa",
                    PesoKg = 1.2,
                    Tamanio = TamanioEnvio.Pequenio
                }
            };

            Envio envioDeHerramientas = new()
            {
                Usuario = cliente,
                DireccionRetiro = ciudadela,
                DireccionEntrega = once,
                FechaDesde = hoy.AddDays(-2),
                FechaHasta = hoy.AddDays(2),
                Monto = 7200m,
                Estado = EstadoEnvio.Entregado,
                Paquete = new Paquete
                {
                    Descripcion = "Caja de herramientas",
                    PesoKg = 11,
                    Tamanio = TamanioEnvio.Grande
                }
            };

            viajeConPaquetes.Asignaciones.Add(new Asignacion
            {
                Envio = envioDeLibros,
                Estado = EstadoViaje.CONFIRMADO,
                FechaAceptacion = DateTime.Now
            });

            viajeConPaquetes.Asignaciones.Add(new Asignacion
            {
                Envio = envioDeRopa,
                Estado = EstadoViaje.PENDIENTE,
                FechaAceptacion = DateTime.Now,
                FechaRetiro = DateTime.Now
            });

            viajeConPaquetes.Asignaciones.Add(new Asignacion
            {
                Envio = envioDeHerramientas,
                Estado = EstadoViaje.CONFIRMADO,
                FechaAceptacion = DateTime.Now.AddDays(-2),
                FechaRetiro = DateTime.Now.AddDays(-1),
                FechaEntrega = DateTime.Now.AddHours(-3)
            });

            // Viaje INACTIVO y sin paquetes: sirve para el estado vacio de la pantalla
            // y para el filtro Activos / Inactivos de la lista.
            Viaje viajeInactivo = new()
            {
                Ruta = rutaAlCentro,
                DireccionOrigen = ramos,
                DireccionDestino = centro,
                PesoMax = 25,
                CapacidadMaxPaquetes = 4,
                FechaSalida = hoy.AddDays(3).AddHours(8),
                FechaLlegada = hoy.AddDays(3).AddHours(9).AddMinutes(30),
                Estado = EstadoActividadViaje.INACTIVO
            };

            // Viaje de OTRO conductor: entrando a su detalle con el usuario de prueba
            // tiene que devolver 404.
            Ruta rutaAjena = new()
            {
                Usuario = otroConductor,
                DireccionOrigen = caballito,
                DireccionDestino = once,
                PesoMax = 10,
                CapacidadMaxPaquetes = 2,
                FechaInicio = hoy,
                FechaFin = null,
                RutaDias = { new RutaDia { DiaSemana = DayOfWeek.Saturday } }
            };

            Viaje viajeAjeno = new()
            {
                Ruta = rutaAjena,
                DireccionOrigen = caballito,
                DireccionDestino = once,
                PesoMax = 10,
                CapacidadMaxPaquetes = 2,
                FechaSalida = hoy.AddDays(2).AddHours(10),
                FechaLlegada = hoy.AddDays(2).AddHours(11),
                Estado = EstadoActividadViaje.ACTIVO
            };

            try
            {
                await _repositorio.AgregarAsync([viajeConPaquetes, viajeInactivo, viajeAjeno]);
            }
            catch (DbUpdateException ex)
            {
                _registro.LogWarning(ex,
                    "No se pudieron guardar los datos de prueba porque la base no coincide con el modelo actual. " +
                    "Corre Update-Database y vuelve a arrancar la app.");
                return;
            }

            _registro.LogInformation(
                "Datos de prueba cargados. Viaje con paquetes: /viajes/{ConPaquetes} | " +
                "Viaje inactivo: /viajes/{Inactivo} | Viaje de otro usuario (deberia dar 404): /viajes/{Ajeno}. " +
                "Usuario: {Email} / {Contrasenia}",
                viajeConPaquetes.Id, viajeInactivo.Id, viajeAjeno.Id,
                EmailConductor, ContraseniaDeTodos);
        }

        private Usuario CrearUsuario(string nombre, string apellido, string email)
        {
            Usuario usuario = new()
            {
                Nombre = nombre,
                Apellido = apellido,
                Email = email,
                CreadoEn = DateTime.Now
            };
            usuario.ContraseniaHash = _hasher.HashPassword(usuario, ContraseniaDeTodos);
            return usuario;
        }

        private static Direccion CrearDireccion(string texto, decimal latitud, decimal longitud)
            => new() { Texto = texto, Latitud = latitud, Longitud = longitud };
    }
}
