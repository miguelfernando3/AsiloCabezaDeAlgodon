
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Medico,Enfermero")]
    public class HistorialMedicoController : Controller
    {
        private readonly AsiloContext _context;

        public HistorialMedicoController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar formulario para registrar o editar el historial
        [HttpGet]
        public async Task<IActionResult> Editar(int pacienteId)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == pacienteId);

            if (paciente == null)
            {
                return NotFound();
            }

            var historial = await _context.HistorialesMedicos
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

            if (historial == null)
            {
                historial = new HistorialMedico
                {
                    PacienteId = pacienteId
                };
            }

            ViewBag.NombrePaciente = paciente.Nombre + " " + paciente.Apellido;

            return View(historial);
        }

        // Guardar los datos del historial médico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(HistorialMedico datos)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == datos.PacienteId);

            if (paciente == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.NombrePaciente = paciente.Nombre + " " + paciente.Apellido;
                return View(datos);
            }

            var historial = await _context.HistorialesMedicos
                .FirstOrDefaultAsync(h => h.PacienteId == datos.PacienteId);

            if (historial == null)
            {
                historial = new HistorialMedico
                {
                    PacienteId = datos.PacienteId,
                    FechaRegistro = DateTime.Now
                };

                _context.HistorialesMedicos.Add(historial);
            }

            historial.MotivoIngreso = datos.MotivoIngreso;
            historial.AntecedentesMedicos = datos.AntecedentesMedicos;
            historial.EnfermedadesCronicas = datos.EnfermedadesCronicas;
            historial.Alergias = datos.Alergias;
            historial.Psicopatologias = datos.Psicopatologias;
            historial.MedicamentosHabituales = datos.MedicamentosHabituales;
            historial.Observaciones = datos.Observaciones;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Historial médico guardado correctamente.";

            return RedirectToAction(
                "Historial",
                "Paciente",
                new { id = datos.PacienteId });
        }
    }
}
