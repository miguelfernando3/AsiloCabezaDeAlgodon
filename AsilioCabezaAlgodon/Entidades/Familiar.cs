
using System;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Entidades
{
    public class Familiar
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Seleccione un paciente")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione un paciente válido")]
        public int? PacienteId { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Ingrese el apellido")]
        public string Apellido { get; set; } = "";

        public string Telefono { get; set; } = "";

        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        public string Correo { get; set; } = "";

        [Required(ErrorMessage = "Ingrese el parentesco")]
        public string Parentesco { get; set; } = "";

        [Display(Name = "Autorizar notificaciones médicas")]
        public bool RecibeNotificaciones { get; set; } = false;
    }
}