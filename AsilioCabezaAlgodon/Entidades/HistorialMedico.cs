
using System;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Entidades
{
    public class HistorialMedico
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione un paciente.")]
        public int PacienteId { get; set; }

        // Motivo por el que ingresó al asilo
        [StringLength(1000)]
        public string MotivoIngreso { get; set; } = "";

        // Enfermedades que ha padecido anteriormente
        [StringLength(2000)]
        public string AntecedentesMedicos { get; set; } = "";

        // Enfermedades que padece actualmente
        [StringLength(1000)]
        public string EnfermedadesCronicas { get; set; } = "";

        // Alergias conocidas
        [StringLength(1000)]
        public string Alergias { get; set; } = "";

        // Psicopatologías que presenta el paciente
        [StringLength(1000)]
        public string Psicopatologias { get; set; } = "";

        // Medicamentos que utiliza habitualmente
        [StringLength(2000)]
        public string MedicamentosHabituales { get; set; } = "";

        // Información adicional del paciente
        [StringLength(2000)]
        public string Observaciones { get; set; } = "";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
