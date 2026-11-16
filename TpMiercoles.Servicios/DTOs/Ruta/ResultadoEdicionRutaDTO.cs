namespace TpMiercoles.Servicios.DTOs.Ruta
{
    // Resultado de editar una ruta: la edicion se guardo, pero puede haber avisos para el conductor.
    public class ResultadoEdicionRutaDTO
    {
        public List<string> Avisos { get; set; } = [];
    }
}
