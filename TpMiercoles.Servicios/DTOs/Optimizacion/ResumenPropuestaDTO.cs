using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Optimizacion {
    // Lo que muestra la pantalla de confirmación (Pantalla 11) antes de reservar los envíos.
    // Se arma con los datos actuales de la base: si un envío ya no está disponible, lo indica.
    public sealed class ResumenPropuestaDTO {
        public int RutaId { get; init; }
        public int? ViajeId { get; init; }
        public DateTime FechaSalida { get; init; }
        public string Origen { get; init; } = string.Empty;
        public string Destino { get; init; } = string.Empty;

        // Problemas de la salida en sí (ruta inactiva, viaje inactivo, salida pasada, sin lugar).
        public IReadOnlyList<string> MotivosDeLaSalida { get; init; } = [];

        public IReadOnlyList<EnvioDePropuestaDTO> Envios { get; init; } = [];

        public bool TodosDisponibles =>
            MotivosDeLaSalida.Count == 0 && Envios.Count > 0 && Envios.All(envio => envio.Disponible);

        public decimal GananciaTotal => Envios.Sum(envio => envio.Monto);

        public double PesoTotalKg => Envios.Sum(envio => envio.PesoKg);

        public int CantidadPaquetes => Envios.Count;
    }

    public sealed class EnvioDePropuestaDTO {
        public int Id { get; init; }
        public string Descripcion { get; init; } = string.Empty;
        public decimal Monto { get; init; }
        public double PesoKg { get; init; }
        public string Retiro { get; init; } = string.Empty;
        public string Entrega { get; init; } = string.Empty;
        public bool Disponible { get; init; }

        // Por qué ya no se puede aceptar (solo si Disponible es false).
        public string? Motivo { get; init; }
    }
}