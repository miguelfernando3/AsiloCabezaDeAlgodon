
using System;

namespace AsilioCabezaAlgodon.Entidades
{
    public class Medicamento
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string Dosis { get; set; } = "";

        public string Duracion { get; set; } = "";

        // Precio original del medicamento
        public decimal Costo { get; set; } = 0;

        // Descuento de la fundación
        public decimal Descuento { get; set; } = 0;

        // Relación con la visita médica
        public int? VisitaMedicaId { get; set; }

        // Pendiente o Despachado
        public string Estado { get; set; } = "Pendiente";
    }
}
