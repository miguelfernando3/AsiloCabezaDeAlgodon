
using System;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Entidades
{
    public class Pago
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione un paciente.")]
        public int PacienteId { get; set; }

        // Opcional: pago relacionado con una cita médica
        public int? CitaMedicaId { get; set; }

        [Range(typeof(decimal), "0.01", "999999999.99",
            ErrorMessage = "El monto debe ser mayor que cero.")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Ingrese el concepto del pago.")]
        [StringLength(200)]
        public string Concepto { get; set; } = "";

        // Identifica si el pago es una mensualidad
        public bool EsMensualidad { get; set; } = false;

        // Mes correspondiente a la mensualidad
        [Range(1, 12, ErrorMessage = "Seleccione un mes válido.")]
        public int? Mes { get; set; }

        // Año correspondiente a la mensualidad
        [Range(2020, 2100, ErrorMessage = "Ingrese un año válido.")]
        public int? Anio { get; set; }
    }
}
