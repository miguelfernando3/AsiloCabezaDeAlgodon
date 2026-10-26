
using System;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Entidades
{
    public class SolicitudMedica
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione un paciente.")]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "Ingrese el motivo de la solicitud.")]
        [StringLength(500)]
        public string Motivo { get; set; } = "";

        [Required(ErrorMessage = "Ingrese la especialidad solicitada.")]
        [StringLength(100)]
        public string EspecialidadSolicitada { get; set; } = "";

        [Required(ErrorMessage = "Ingrese el nombre del enfermero acompañante.")]
        [StringLength(150)]
        public string EnfermeroAcompanante { get; set; } = "";

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        // Pendiente, Programada o Cancelada
        public string Estado { get; set; } = "Pendiente";

        // Se asignará cuando la fundación programe la cita
        public int? CitaMedicaId { get; set; }
    }
}
