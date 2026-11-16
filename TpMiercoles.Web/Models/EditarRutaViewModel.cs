namespace TpMiercoles.Web.Models {
    // Mismos campos y validaciones que al crear una ruta, mas el Id de la ruta que se edita.
    public class EditarRutaViewModel : CrearRutaViewModel {
        public int Id { get; set; }
    }
}
