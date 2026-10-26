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

        // Costo del examen de laboratorio
        public decimal Costo { get; set; } = 0;
    }
}