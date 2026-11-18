using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class MisViajesViewModel
    {
        // null = filtro "Todos"
        public EstadoActividadViaje? Filtro { get; set; }
        public List<ViajeListadoViewModel> Viajes { get; set; } = [];

        public int TotalActivos { get; set; }
        public int TotalInactivos { get; set; }

        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; }
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
    }
}
