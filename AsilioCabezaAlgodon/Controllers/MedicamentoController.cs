
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Medico,Farmacia,Fundacion")]
    public class MedicamentoController : Controller
    {
        private readonly AsiloContext _context;

        public MedicamentoController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar medicamentos registrados
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var medicamentos = await _context.Medicamentos
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return View(medicamentos);
        }

        // Mostrar formulario para registrar medicamentos
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico,Farmacia")]
        public async Task<IActionResult> Create()
        {
            await CargarVisitas();

            return View();
        }

        // Guardar medicamento
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Medico,Farmacia")]
        public async Task<IActionResult> Create(Medicamento medicamento)
        {
            // Validar el costo
            if (medicamento.Costo < 0)
            {
                ModelState.AddModelError(
                    "Costo",
                    "El costo no puede ser negativo.");
            }

            // Verificar que la visita médica exista
            if (medicamento.VisitaMedicaId.HasValue)
            {
                bool existeVisita = await _context.VisitasMedicas
                    .AnyAsync(v =>
                        v.Id == medicamento.VisitaMedicaId.Value);

                if (!existeVisita)
                {
                    ModelState.AddModelError(
                        "VisitaMedicaId",
                        "La visita médica no existe.");
                }
            }

            // Si hay errores, regresar al formulario
            if (!ModelState.IsValid)
            {
                await CargarVisitas();

                return View(medicamento);
            }

            // Guardar medicamento en SQL Server
            _context.Medicamentos.Add(medicamento);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Cargar las visitas médicas con el nombre del paciente
        private async Task CargarVisitas()
        {
            var datos = await _context.VisitasMedicas
                .Join(
                    _context.CitasMedicas,
                    visita => visita.CitaMedicaId,
                    cita => cita.Id,
                    (visita, cita) => new
                    {
                        visita.Id,
                        cita.PacienteId,
                        visita.Fecha
                    })
                .Join(
                    _context.Pacientes,
                    dato => dato.PacienteId,
                    paciente => paciente.Id,
                    (dato, paciente) => new
                    {
                        dato.Id,
                        paciente.Nombre,
                        paciente.Apellido,
                        dato.Fecha
                    })
                .OrderByDescending(dato => dato.Fecha)
                .ToListAsync();

            var visitas = datos.Select(dato => new
            {
                dato.Id,
                Descripcion = dato.Nombre + " " +
                              dato.Apellido + " - " +
                              dato.Fecha.ToString("dd/MM/yyyy")
            }).ToList();

            ViewBag.ListaVisitas = new SelectList(
                visitas,
                "Id",
                "Descripcion");
        }
    }
}
