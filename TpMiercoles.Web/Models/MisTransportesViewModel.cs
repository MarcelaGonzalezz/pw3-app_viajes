namespace TpMiercoles.Web.Models
{
    // Todos los paquetes que el conductor acepto llevar, incluidos los ya entregados.
    public class MisTransportesViewModel
    {
        public List<TransporteListadoViewModel> Transportes { get; set; } = [];
    }
}
