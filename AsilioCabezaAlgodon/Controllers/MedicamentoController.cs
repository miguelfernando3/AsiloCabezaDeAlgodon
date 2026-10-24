
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

            // Obtener las visitas, citas y pacientes
            // para identificar a quién pertenece cada medicamento
            ViewBag.Visitas = await _context.VisitasMedicas
                .ToListAsync();

            ViewBag.Citas = await _context.CitasMedicas
                .ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes
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
            if (string.IsNullOrWhiteSpace(medicamento.Nombre))
            {
                ModelState.AddModelError(
                    "Nombre",
                    "Ingresa el nombre del medicamento.");
            }

            if (string.IsNullOrWhiteSpace(medicamento.Dosis))
            {
                ModelState.AddModelError(
                    "Dosis",
                    "Ingresa la dosis del medicamento.");
            }

            if (string.IsNullOrWhiteSpace(medicamento.Duracion))
            {
                ModelState.AddModelError(
                    "Duracion",
                    "Ingresa la duración del tratamiento.");
            }

            if (medicamento.Costo < 0)
            {
                ModelState.AddModelError(
                    "Costo",
                    "El costo no puede ser negativo.");
            }

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

            if (!ModelState.IsValid)
            {
                await CargarVisitas();

                return View(medicamento);
            }

            _context.Medicamentos.Add(medicamento);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Cargar visitas médicas con el nombre del paciente
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
