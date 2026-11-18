namespace TpMiercoles.Web.Models
{
    // Todos los paquetes que el conductor acepto llevar, incluidos los ya entregados.
    public class MisTransportesViewModel
    {
        // "encurso" (por defecto), "entregados" o "todos".
        public string Filtro { get; set; } = "encurso";
        public int TotalEnCurso { get; set; }
        public int TotalEntregados { get; set; }
        public List<TransporteListadoViewModel> Transportes { get; set; } = [];
    }
}
