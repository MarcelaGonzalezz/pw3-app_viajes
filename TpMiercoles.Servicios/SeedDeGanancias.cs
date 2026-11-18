using Microsoft.Extensions.Logging;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface ISeedDeGanancias
    {
        Task SembrarAsync();
    }

    // Datos para ver la diferencia entre los criterios de la pantalla de ganancias.
    // Para cada salida de la ruta de Ana en las proximas semanas, Bruno publica cuatro
    // envios que solo sirven ese dia: uno caro y lejano, uno mediano y dos baratos de paso.
    // Como vencen con la salida, lo que un criterio no elige se pierde, y el total del mes
    // cambia segun el criterio. Si Bruno ya tiene envios vigentes, no hace nada.
    public class SeedDeGanancias : ISeedDeGanancias
    {
        private const int SemanasAPublicar = 4;

        private static readonly ModeloDeEnvio[] Modelos =
        [
            new("Silla de escritorio", 6, 11000m, TamanioEnvio.Grande,
                "Av. Rivadavia 16000, Haedo", -34.644000m, -58.594000m,
                "Av. Cabildo 2000, Belgrano", -34.562000m, -58.456000m),
            new("Monitor", 4, 5000m, TamanioEnvio.Mediano,
                "Av. Directorio 6000, Mataderos", -34.660000m, -58.502000m,
                "Av. Las Heras 2000, Recoleta", -34.588000m, -58.393000m),
            new("Sobre con documentos", 2, 3000m, TamanioEnvio.Pequenio,
                "Av. Rivadavia 7000, Flores", -34.628000m, -58.463000m,
                "Av. Callao 100, Congreso", -34.609000m, -58.392000m),
            new("Caja de zapatos", 3, 2500m, TamanioEnvio.Pequenio,
                "Av. Rivadavia 11500, Liniers", -34.642000m, -58.524000m,
                "Av. Corrientes 5000, Villa Crespo", -34.598000m, -58.438000m)
        ];

        private readonly IUsuarioRepositorio _usuarios;
        private readonly IRutaRepositorio _rutas;
        private readonly IEnvioRepositorio _envios;
        private readonly IViajeRepositorio _viajes;
        private readonly ILogger<SeedDeGanancias> _logger;

        public SeedDeGanancias(
            IUsuarioRepositorio usuarios,
            IRutaRepositorio rutas,
            IEnvioRepositorio envios,
            IViajeRepositorio viajes,
            ILogger<SeedDeGanancias> logger)
        {
            _usuarios = usuarios;
            _rutas = rutas;
            _envios = envios;
            _viajes = viajes;
            _logger = logger;
        }

        // Las rutas de Ana que se crearon antes de que existieran las columnas de horario
        // quedaron en 00:00 -> 00:00 (el valor por defecto de la base), y salen a la medianoche.
        // Les ponemos el horario que tienen en el seed. Solo toca rutas de la usuaria de prueba
        // con los dos horarios en cero, asi que una base nueva no se modifica.
        private async Task CorregirHorariosVaciosAsync(int conductorId)
        {
            List<Ruta> sinHorario = (await _rutas.ObtenerRutasDeUnUsuarioAsync(conductorId))
                .Where(r => r.HoraSalida == TimeOnly.MinValue && r.HoraLlegada == TimeOnly.MinValue)
                .ToList();

            if (sinHorario.Count == 0)
            {
                return;
            }

            foreach (Ruta ruta in sinHorario)
            {
                ruta.HoraSalida = new TimeOnly(8, 0);
                ruta.HoraLlegada = new TimeOnly(9, 30);
            }

            await _rutas.GuardarCambiosAsync();
            _logger.LogInformation("Seed de ganancias: se corrigio el horario de {Cantidad} rutas.", sinHorario.Count);
        }

        // Los viajes que ya se habian creado sobre esas rutas quedaron a las 00:00. Si no se
        // mueven al horario nuevo, dejan de coincidir con su salida: lo comprometido no se ve
        // en la estimacion y aceptar un envio crearia otro viaje para la misma salida.
        // Si ya existe un viaje en el horario nuevo, no se toca (no se duplica la salida).
        private async Task CorregirViajesAMedianocheAsync(int conductorId)
        {
            Dictionary<int, Ruta> rutasConHorario = (await _rutas.ObtenerRutasDeUnUsuarioAsync(conductorId))
                .Where(r => r.HoraSalida != TimeOnly.MinValue)
                .ToDictionary(r => r.Id);

            List<Viaje> aMedianoche = (await _viajes.ListarPorUsuarioAsync(conductorId, null))
                .Where(v => rutasConHorario.ContainsKey(v.RutaId) && v.FechaSalida.TimeOfDay == TimeSpan.Zero)
                .ToList();

            int movidos = 0;

            foreach (Viaje viaje in aMedianoche)
            {
                Ruta ruta = rutasConHorario[viaje.RutaId];
                DateTime nuevaSalida = viaje.FechaSalida.Date + ruta.HoraSalida.ToTimeSpan();

                TimeSpan duracion = ruta.HoraLlegada.ToTimeSpan() - ruta.HoraSalida.ToTimeSpan();
                if (duracion < TimeSpan.Zero)
                {
                    duracion += TimeSpan.FromDays(1);
                }

                if (await _viajes.ObtenerPorRutaYFechaAsync(viaje.RutaId, nuevaSalida) is not null)
                {
                    _logger.LogWarning(
                        "Seed de ganancias: el viaje {ViajeId} no se movio porque ya hay otro viaje a las {Salida}.",
                        viaje.Id, nuevaSalida);
                    continue;
                }

                Viaje? editable = await _viajes.ObtenerParaModificarAsync(viaje.Id);
                if (editable is null)
                {
                    continue;
                }

                editable.FechaSalida = nuevaSalida;
                editable.FechaLlegada = nuevaSalida + duracion;
                movidos++;
            }

            if (movidos > 0)
            {
                await _viajes.GuardarCambiosAsync();
                _logger.LogInformation("Seed de ganancias: se movieron {Cantidad} viajes al horario de su ruta.", movidos);
            }
        }

        public async Task SembrarAsync()
        {
            try
            {
                Usuario? ana = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailConductor);
                Usuario? bruno = await _usuarios.BuscarPorEmailAsync(SeedDeDesarrollo.EmailOtroConductor);

                if (ana is null || bruno is null)
                {
                    _logger.LogInformation("No se puede sembrar ganancias: no se encontraron usuarios de prueba.");
                    return;
                }

                await CorregirHorariosVaciosAsync(ana.Id);
                await CorregirViajesAMedianocheAsync(ana.Id);

                DateTime hoy = DateTime.Today;

                List<Envio> publicados =
                    await _envios.ListarPorUsuarioAsync(bruno.Id, EstadoEnvio.BuscandoConductor);
                if (publicados.Any(e => e.FechaHasta.Date >= hoy))
                {
                    return;
                }

                Ruta? ruta = (await _rutas.ObtenerRutasActivasDeUnUsuarioAsync(ana.Id))
                    .OrderBy(r => r.Id)
                    .FirstOrDefault();

                if (ruta is null)
                {
                    _logger.LogInformation("No se puede sembrar ganancias: Ana no tiene rutas activas.");
                    return;
                }

                List<DateTime> salidas =
                    CalendarioDeSalidas.Todas(ruta, DateTime.Now, hoy.AddDays(7 * SemanasAPublicar));

                foreach (DateTime salida in salidas)
                {
                    foreach (ModeloDeEnvio modelo in Modelos)
                    {
                        await Publicar(bruno, modelo, salida.Date);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al sembrar los envíos de ganancias.");
            }
        }

        private Task Publicar(Usuario remitente, ModeloDeEnvio modelo, DateTime dia)
        {
            return _envios.CrearAsync(new Envio
            {
                UsuarioId = remitente.Id,
                DireccionRetiro = new Direccion
                {
                    Texto = modelo.Retiro,
                    Latitud = modelo.LatitudRetiro,
                    Longitud = modelo.LongitudRetiro
                },
                DireccionEntrega = new Direccion
                {
                    Texto = modelo.Entrega,
                    Latitud = modelo.LatitudEntrega,
                    Longitud = modelo.LongitudEntrega
                },
                FechaDesde = dia,
                FechaHasta = dia,
                Monto = modelo.Monto,
                Estado = EstadoEnvio.BuscandoConductor,
                Paquete = new Paquete
                {
                    Descripcion = modelo.Descripcion,
                    PesoKg = modelo.PesoKg,
                    Tamanio = modelo.Tamanio
                }
            });
        }

        private sealed record ModeloDeEnvio(
            string Descripcion,
            double PesoKg,
            decimal Monto,
            TamanioEnvio Tamanio,
            string Retiro,
            decimal LatitudRetiro,
            decimal LongitudRetiro,
            string Entrega,
            decimal LatitudEntrega,
            decimal LongitudEntrega);
    }
}
