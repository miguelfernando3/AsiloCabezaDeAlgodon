
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Fundacion")]
    public class DonacionController : Controller
    {
        private readonly AsiloContext _context;

        public DonacionController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar donaciones registradas
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var donaciones = await _context.Donaciones
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            return View(donaciones);
        }

        // Mostrar formulario de registro
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Donacion());
        }

        // Guardar una donacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Donacion donacion)
        {
            if (!ModelState.IsValid)
            {
                return View(donacion);
            }

            _context.Donaciones.Add(donacion);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
