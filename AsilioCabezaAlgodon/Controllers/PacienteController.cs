using Microsoft.AspNetCore.Mvc;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    public class PacienteController : Controller
    {
        private readonly AsiloContext _context;

        public PacienteController(AsiloContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var pacientes = _context.Pacientes.ToList();

            return View(pacientes);
        }

        public IActionResult Create()
        
        {
        return View();
        }

[HttpPost]
public IActionResult Create(Paciente paciente)
{
    _context.Pacientes.Add(paciente);
    _context.SaveChanges();

    return RedirectToAction("Index");
}
    }
}