using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface ISeedDeOportunidades
    {
        Task SembrarAsync();
    }

    // datos para probar oportunidades:
    // un viaje futuro de Ana sobre su ruta Ramos -> Centro y envios de Carla, algunos compatibles
    // y otros que se tienen que descartar (muy pesado, fuera de fecha). Si carla ya tiene envios
    // vigentes buscando conductor, no hace nada.
    public class SeedDeOportunidades : ISeedDeOportunidades
    {
        private readonly IUsuarioRepositorio _usuarios;
        private readonly IRutaRepositorio _rutas;
        private readonly IViajeRepositorio _viajes;
        private readonly IEnvioRepositorio _envios;
        private readonly ILogger<SeedDeOportunidades> _logger;

        public SeedDeOportunidades(
            IUsuarioRepositorio usuarios,
            IRutaRepositorio rutas,
            IViajeRepositorio viajes,
            IEnvioRepositorio envios,
            ILogger<SeedDeOportunidades> logger)
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
                    _logger.LogInformation("No se puede sembrar oportunidades: no se encontraron usuarios de prueba.");
                    return;
                }

                DateTime hoy = DateTime.Today;

                // Carla tiene vigente alguno de los envios que crea este seed.
                List<Envio> publicados = await _envios.ListarPorUsuarioAsync(carla.Id, EstadoEnvio.BuscandoConductor);
                if (publicados.Any(e => e.FechaHasta.Date >= hoy && e.Paquete.Descripcion == "Caja de libros"))
                {
                    return;
                }

                Ruta? ruta = (await _rutas.ObtenerRutasActivasDeUnUsuarioAsync(ana.Id))
                    .OrderBy(r => r.Id)
                    .FirstOrDefault();

                if (ruta is null)
                {
                    _logger.LogInformation("No se puede sembrar oportunidades: Ana no tiene rutas activas.");
                    return;
                }

                // proximo lunes, miercoles o viernes (los dias de la ruta del seed)
                DateTime dia = hoy.AddDays(1);
                while (dia.DayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday))
                {
                    dia = dia.AddDays(1);
                }

                await _viajes.AgregarAsync([new Viaje
                {
                    RutaId = ruta.Id,
                    DireccionOrigenId = ruta.DireccionOrigenId,
                    DireccionDestinoId = ruta.DireccionDestinoId,
                    PesoMax = ruta.PesoMax,
                    CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
                    FechaSalida = dia.AddHours(8),
                    FechaLlegada = dia.AddHours(9).AddMinutes(30),
                    Estado = EstadoActividadViaje.ACTIVO
                }]);

                DateTime desde = hoy;
                DateTime hasta = hoy.AddDays(14);

                // Compatibles
                await Publicar(carla, "Caja de libros", 3, 5000m, TamanioEnvio.Mediano,
                    Dir("Av. Rivadavia 100, Ciudadela", -34.636000m, -58.535000m),
                    Dir("Av. Acoyte 300, Caballito", -34.618000m, -58.440000m), desde, hasta);
                await Publicar(carla, "Notebook", 2.5, 6500m, TamanioEnvio.Pequenio,
                    Dir("Av. Rivadavia 11500, Liniers", -34.642000m, -58.524000m),
                    Dir("Av. Pueyrredon 200, Once", -34.609000m, -58.405000m), desde, hasta);
                await Publicar(carla, "Plantas", 6, 8000m, TamanioEnvio.Grande,
                    Dir("Av. Rivadavia 16000, Haedo", -34.644000m, -58.594000m),
                    Dir("Av. Santa Fe 3200, Palermo", -34.588000m, -58.410000m), desde, hasta);
                // Compatible pero con mucho desvio: sirve para probar el filtro de desvio maximo.
                await Publicar(carla, "Bicicleta", 9, 12000m, TamanioEnvio.Grande,
                    Dir("Av. Mitre 500, Avellaneda", -34.662000m, -58.365000m),
                    Dir("Calle 7 800, La Plata", -34.921400m, -57.954500m), desde, hasta);
                // Descartados: muy pesado y fuera de fecha.
                await Publicar(carla, "Heladera chica", 40, 15000m, TamanioEnvio.Grande,
                    Dir("Av. Acoyte 300, Caballito", -34.618000m, -58.440000m),
                    Dir("Av. Corrientes 800, Microcentro", -34.603700m, -58.381600m), desde, hasta);
                await Publicar(carla, "Documentos", 0.5, 3000m, TamanioEnvio.Pequenio,
                    Dir("Av. Pueyrredon 200, Once", -34.609000m, -58.405000m),
                    Dir("Av. Corrientes 800, Microcentro", -34.603700m, -58.381600m), hoy.AddDays(20), hoy.AddDays(25));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al sembrar oportunidades de prueba.");
            }
        }

        private Task Publicar(Usuario remitente, string descripcion, double pesoKg, decimal monto, TamanioEnvio tamanio, 
            Direccion retiro, Direccion entrega, DateTime desde, DateTime hasta)
        {
            return _envios.CrearAsync(new Envio
            {
                UsuarioId = remitente.Id,
                DireccionRetiro = retiro,
                DireccionEntrega = entrega,
                FechaDesde = desde,
                FechaHasta = hasta,
                Monto = monto,
                Estado = EstadoEnvio.BuscandoConductor,
                Paquete = new Paquete { PesoKg = pesoKg, Tamanio = tamanio, Descripcion = descripcion }
            });
        }

        private static Direccion Dir(string texto, decimal lat, decimal lon)
            => new() { Texto = texto, Latitud = lat, Longitud = lon };
    }
}
