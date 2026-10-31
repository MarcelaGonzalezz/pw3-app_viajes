namespace TpMiercoles.Web.Models
{
    public class ObtenerRutasViewModel
    {
        public string NombreUsuario { get; set; }
        public List<RutaViewModel> Rutas { get; set; } = [];
    }
}
