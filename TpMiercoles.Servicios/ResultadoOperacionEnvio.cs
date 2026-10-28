namespace TpMiercoles.Servicios
{
    public class ResultadoOperacionEnvio
    {
        public bool NoEncontrado { get; private init; }
        public string? Error { get; private init; }
        public bool EsExitoso => !NoEncontrado && Error == null;

        public static ResultadoOperacionEnvio NoExiste()
            => new() { NoEncontrado = true };

        public static ResultadoOperacionEnvio Invalido(string error)
            => new() { Error = error };

        public static ResultadoOperacionEnvio Exitoso()
            => new();
    }
}
