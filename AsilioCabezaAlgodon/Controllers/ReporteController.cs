
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReporteController : Controller
    {
        private readonly AsiloContext _context;

        public ReporteController(AsiloContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Cantidad de pacientes registrados
            ViewBag.TotalPacientes =
                await _context.Pacientes.CountAsync();

            // Cantidad de citas médicas
            ViewBag.TotalCitas =
                await _context.CitasMedicas.CountAsync();

            // Cantidad de visitas médicas
            ViewBag.TotalVisitas =
                await _context.VisitasMedicas.CountAsync();

            // Cantidad de familiares
            ViewBag.TotalFamiliares =
                await _context.Familiares.CountAsync();

            // Total de pagos registrados
            ViewBag.TotalPagos =
                await _context.Pagos.SumAsync(p => (decimal?)p.Monto)
                ?? 0m;

            // Total de donaciones registradas
            ViewBag.TotalDonaciones =
                await _context.Donaciones.SumAsync(d => (decimal?)d.Monto)
                ?? 0m;

            return View();
        }
    }
}
