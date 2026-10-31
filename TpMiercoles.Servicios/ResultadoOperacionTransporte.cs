using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios
{
    // Separa "no existe o no es tuyo" (404) de "la transicion no corresponde"
    // (409), que son dos fallas distintas y la Web API las tiene que contestar
    // con codigos distintos.
    public class ResultadoOperacionTransporte
    {
        public bool NoEncontrado { get; private init; }
        public string? Conflicto { get; private init; }
        public TransporteListadoDTO? Transporte { get; private init; }

        public bool EsExitoso => !NoEncontrado && Conflicto is null;

        public static ResultadoOperacionTransporte NoExiste()
            => new() { NoEncontrado = true };

        public static ResultadoOperacionTransporte EnConflicto(string motivo)
            => new() { Conflicto = motivo };

        public static ResultadoOperacionTransporte Exitoso(TransporteListadoDTO transporte)
            => new() { Transporte = transporte };
    }
}
