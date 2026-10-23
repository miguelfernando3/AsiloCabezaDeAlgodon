
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Models
{
    public class RegistrarVisitaViewModel
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Selecciona una cita médica.")]
        public int CitaMedicaId { get; set; }

        [Required(ErrorMessage = "Ingresa la fecha de la visita.")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Ingresa el motivo de la visita.")]
        [StringLength(500)]
        public string Motivo { get; set; } = "";

        [Required(ErrorMessage = "Ingresa el diagnóstico.")]
        [StringLength(1000)]
        public string Diagnostico { get; set; } = "";

        [StringLength(2000)]
        public string Observaciones { get; set; } = "";
    }
}
