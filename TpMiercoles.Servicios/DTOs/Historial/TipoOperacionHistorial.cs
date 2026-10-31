namespace TpMiercoles.Servicios.DTOs.Historial
{
    // Tipo de fila del historial. Tambien se usa como filtro de la pantalla.
    public enum TipoOperacionHistorial
    {
        // Viaje que hice como conductor (suma ganancia).
        ViajeRealizado = 0,

        // Envio que publique y llego a destino.
        EnvioEntregado = 1,

        // Envio que publique y cancele.
        EnvioCancelado = 2
    }
}
