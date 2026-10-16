
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo electrónico")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        public string Correo { get; set; } = "";

        [Required(ErrorMessage = "Ingrese su contraseña")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = "";

        public bool Recordarme { get; set; }
    }
}
