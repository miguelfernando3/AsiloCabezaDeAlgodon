namespace AsilioCabezaAlgodon.Entidades
{
    public class Pago
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; }
    }
}