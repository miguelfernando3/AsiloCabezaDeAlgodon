
using System;

namespace AsilioCabezaAlgodon.Entidades
{
    public class ExamenLaboratorio
    {
        public int Id { get; set; }

        public int VisitaMedicaId { get; set; }

        public string NombreExamen { get; set; } = "";

        public string Resultado { get; set; } = "";

        public DateTime Fecha { get; set; }

        // Precio original del examen
        public decimal Costo { get; set; } = 0;

        // Descuento proporcionado por la fundación
        public decimal Descuento { get; set; } = 0;
    }
}
