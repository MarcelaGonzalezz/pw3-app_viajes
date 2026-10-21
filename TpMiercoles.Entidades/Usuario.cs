using Microsoft.EntityFrameworkCore;

namespace TpMiercoles.Entidades
{
    [Index(nameof(Email), IsUnique = true)]
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
       
        public string Email { get; set; }
        public string ContraseniaHash { get; set; }
        public DateTime CreadoEn { get; set; } = DateTime.Now;
        public List<Envio> Envios { get; set; } = [];

    }
}
