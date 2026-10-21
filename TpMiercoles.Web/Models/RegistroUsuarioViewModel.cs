using System.ComponentModel.DataAnnotations;

namespace TpMiercoles.Web.Models
{
    public class RegistroUsuarioViewModel
    {
        [Required(ErrorMessage = "Por favor, ingresa tu nombre")]
        public string Nombre { get; set; }
        [Required(ErrorMessage = "Por favor, ingresa tu apellido")]
        public string Apellido { get; set; }
        [Required(ErrorMessage = "El Email es obligatorio")]
        [EmailAddress(ErrorMessage = "Por favor ingresa un Email válido")]
        public string Email { get; set; }
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]

        public string Contrasenia { get; set; }
        [Required(ErrorMessage = "Las contraseñas deben coincidir")]
        [Compare("Contrasenia", ErrorMessage = "Las contraseñas no coinciden")]
        public string RepetirContrasenia { get; set; }
    }
}
