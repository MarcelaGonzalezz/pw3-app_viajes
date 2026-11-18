namespace TpMiercoles.Servicios.Rutas {
    // Servicio de rutas que estima sin consultar a nadie (linea recta x factor de calle): es gratis
    // y no tiene limite de pedidos. El optimizador lo usa para comparar todas las combinaciones
    // y le pide a OpenRouteService solo las finalistas. Lo implementa MockServicioRutas.
    public interface IEstimadorDeRutas : IServicioRutas {
    }
}
