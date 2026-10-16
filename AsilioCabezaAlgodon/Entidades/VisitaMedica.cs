namespace AsilioCabezaAlgodon.Entidades
{
    public class VisitaMedica
    {
        public int Id { get; set; }
        public int CitaMedicaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public string Diagnostico { get; set; }
        public string Observaciones { get; set; }
    }
}