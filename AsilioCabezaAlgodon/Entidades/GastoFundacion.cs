
using System;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Entidades
{
    public class GastoFundacion
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingrese el concepto del gasto")]
        [StringLength(200)]
        public string Concepto { get; set; } = "";

        [Range(typeof(decimal), "0.01", "999999999.99",
            ErrorMessage = "El monto debe ser mayor que cero")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "Ingrese la fecha")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [StringLength(300)]
        public string Descripcion { get; set; } = "";
    }
}
