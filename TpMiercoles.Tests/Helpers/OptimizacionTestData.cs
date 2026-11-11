using TpMiercoles.Entidades;
using TpMiercoles.Servicios.Optimizacion;

namespace TpMiercoles.Tests.Helpers;

internal static class OptimizacionTestData
{
    internal static readonly DateTime Fecha = new(2026, 11, 2, 8, 0, 0);

    internal static Direccion Direccion(decimal longitud) => new()
    {
        Texto = $"Punto {longitud}", Latitud = 0, Longitud = longitud
    };

    internal static Ruta Ruta(double peso = 10, int cantidad = 3) => new()
    {
        Id = 1, UsuarioId = 7, Estado = EstadoRuta.ACTIVA,
        FechaInicio = new DateTime(2026, 11, 1),
        HoraSalida = new TimeOnly(8, 0), HoraLlegada = new TimeOnly(9, 0),
        RutaDias = [new RutaDia { DiaSemana = DayOfWeek.Monday }],
        DireccionOrigen = Direccion(0), DireccionDestino = Direccion(10),
        PesoMax = peso, CapacidadMaxPaquetes = cantidad
    };

    internal static Viaje Viaje(Ruta ruta) => new()
    {
        Id = 20, RutaId = ruta.Id, Ruta = ruta,
        DireccionOrigen = ruta.DireccionOrigen, DireccionDestino = ruta.DireccionDestino,
        FechaSalida = Fecha, FechaLlegada = Fecha.AddHours(1),
        Estado = EstadoActividadViaje.ACTIVO,
        PesoMax = ruta.PesoMax, CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
        Paradas = ruta.Paradas.ToList()
    };

    internal static SalidaOptimizacion Salida(Ruta? ruta = null, Viaje? viaje = null)
    {
        ruta ??= viaje?.Ruta ?? Ruta();
        return new SalidaOptimizacion
        {
            Ruta = ruta, Viaje = viaje,
            FechaSalidaProgramada = viaje?.FechaSalida ?? Fecha,
            MomentoEvaluacion = viaje?.FechaSalida ?? Fecha
        };
    }

    internal static Envio Envio(int id = 1, double peso = 2, decimal pago = 1000) => new()
    {
        Id = id, UsuarioId = 99, Estado = EstadoEnvio.BuscandoConductor,
        FechaDesde = Fecha.Date, FechaHasta = Fecha.Date.AddDays(1), Monto = pago,
        Paquete = new Paquete { PesoKg = peso, Descripcion = $"Paquete {id}", Tamanio = TamanioEnvio.Mediano },
        // Coordenadas únicas para identificar cada paquete en el proveedor ficticio.
        DireccionRetiro = Direccion(id * 0.01m),
        DireccionEntrega = Direccion(id * 0.01m + 0.001m)
    };

    internal static Asignacion Asignar(Viaje viaje, Envio envio,
        EstadoViaje estado = EstadoViaje.CONFIRMADO)
    {
        var asignacion = new Asignacion
        {
            EnvioId = envio.Id, Envio = envio, ViajeId = viaje.Id, Viaje = viaje, Estado = estado
        };
        viaje.Asignaciones.Add(asignacion);
        return asignacion;
    }
}
