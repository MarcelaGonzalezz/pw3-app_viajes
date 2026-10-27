namespace TpMiercoles.Servicios
{
    // Resultado de una operacion sobre un viaje. Separa "no existe o no es tuyo"
    // (que el controller traduce a 404) de "los datos no pasan las validaciones"
    // (que vuelve al formulario con los errores).
    public class ResultadoOperacionViaje
    {
        public bool NoEncontrado { get; private init; }
        public List<string> Errores { get; private init; } = [];

        // Cantidad de paquetes que tenia el viaje al momento de la operacion.
        // La usa la desactivacion para avisar cuantos envios quedan comprometidos.
        public int PaquetesAfectados { get; private init; }

        public bool EsExitoso => !NoEncontrado && Errores.Count == 0;

        public static ResultadoOperacionViaje NoExiste()
            => new() { NoEncontrado = true };

        public static ResultadoOperacionViaje Invalido(List<string> errores)
            => new() { Errores = errores };

        public static ResultadoOperacionViaje Exitoso(int paquetesAfectados = 0)
            => new() { PaquetesAfectados = paquetesAfectados };
    }
}
