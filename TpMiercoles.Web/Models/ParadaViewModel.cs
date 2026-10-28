namespace TpMiercoles.Web.Models
{
    public class ParadaViewModel
    {
        public int Orden { get; set; }
        public DireccionViewModel Direccion { get; set; } = new();
    }
}
