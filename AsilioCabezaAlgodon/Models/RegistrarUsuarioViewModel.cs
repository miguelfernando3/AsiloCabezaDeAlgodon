
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Models
{
    public class RegistrarUsuarioViewModel
    {
        [Required(ErrorMessage = "Ingrese el correo electrónico.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Correo { get; set; } = "";

        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = "";

        [Required(ErrorMessage = "Seleccione un cargo.")]
        public string Rol { get; set; } = "";
    }
}
