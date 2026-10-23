
using System;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Entidades
{
    public class Donacion
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del donante")]
        [StringLength(150)]
        public string Donante { get; set; } = "";

        [Range(typeof(decimal), "0.01", "999999999.99",
            ErrorMessage = "El monto debe ser mayor que cero")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Ingrese una descripción")]
        [StringLength(300)]
        public string Descripcion { get; set; } = "";
    }
}
