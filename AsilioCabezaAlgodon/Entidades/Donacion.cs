namespace AsilioCabezaAlgodon.Entidades
{
    public class Donacion
    {
        public int Id { get; set; }
        public string Donante { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
    }
}