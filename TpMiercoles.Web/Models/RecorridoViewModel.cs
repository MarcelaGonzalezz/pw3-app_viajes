namespace TpMiercoles.Web.Models {
    public class RecorridoViewModel {
        public DireccionViewModel Origen { get; set; } = new();
        public DireccionViewModel Destino { get; set; } = new();
        public List<ParadaViewModel> Paradas { get; set; } = [];
    }
}
