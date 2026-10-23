
using System;

namespace AsilioCabezaAlgodon.Entidades
{
    public class Medicamento
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string Dosis { get; set; } = "";

        public string Duracion { get; set; } = "";

        public decimal Costo { get; set; }

        // Relación con la visita médica
        public int? VisitaMedicaId { get; set; }
    }
}
