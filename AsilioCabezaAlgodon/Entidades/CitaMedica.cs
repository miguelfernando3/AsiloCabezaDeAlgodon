
namespace AsilioCabezaAlgodon.Entidades
{
    public class CitaMedica
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }

        public int MedicoId { get; set; }

        public DateTime Fecha { get; set; }

        public string Motivo { get; set; } = "";

        public string Estado { get; set; } = "";

        // Control de costos de la consulta
        public decimal CostoConsulta { get; set; } = 0;

        // Descuento otorgado por la fundación
        public decimal Descuento { get; set; } = 0;

        // Cantidad abonada a la consulta
        public decimal MontoPagado { get; set; } = 0;

        // Pendiente, Parcial o Pagado
        public string EstadoPago { get; set; } = "Pendiente";
    }
}
