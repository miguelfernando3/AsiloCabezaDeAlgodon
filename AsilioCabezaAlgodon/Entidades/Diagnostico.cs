namespace AsilioCabezaAlgodon.Entidades
{
    public class Diagnostico
    {
        public int Id { get; set; }
        public int VisitaMedicaId { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
    }
}