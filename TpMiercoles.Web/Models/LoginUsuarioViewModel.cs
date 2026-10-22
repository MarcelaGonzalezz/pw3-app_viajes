using System.ComponentModel.DataAnnotations;

namespace TpMiercoles.Web.Models
{
    public class LoginUsuarioViewModel
    {
        [Required(ErrorMessage = "El Email es obligatorio")]
        [EmailAddress(ErrorMessage = "Por favor ingresa un Email válido")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasenia { get; set; } = string.Empty;
        public bool Recordarme { get; set; }
    }
}
