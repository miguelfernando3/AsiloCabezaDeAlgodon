
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Fundacion")]
    public class MedicoController : Controller
    {
        private readonly AsiloContext _context;

        public MedicoController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar médicos registrados
        public async Task<IActionResult> Index()
        {
            var medicos = await _context.Medicos
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return View(medicos);
        }

        // Mostrar formulario de registro
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Guardar médico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medico medico)
        {
            if (!ModelState.IsValid)
            {
                return View(medico);
            }

            _context.Medicos.Add(medico);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
