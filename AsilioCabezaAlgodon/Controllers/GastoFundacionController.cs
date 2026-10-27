
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Fundacion")]
    public class GastoFundacionController : Controller
    {
        private readonly AsiloContext _context;

        public GastoFundacionController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar los gastos registrados
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var gastos = await _context.GastosFundacion
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            return View(gastos);
        }

        // Mostrar formulario para registrar un gasto
        [HttpGet]
        public IActionResult Create()
        {
            return View(new GastoFundacion());
        }

        // Guardar el gasto en la base de datos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GastoFundacion gasto)
        {
            if (!ModelState.IsValid)
            {
                return View(gasto);
            }

            _context.GastosFundacion.Add(gasto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
