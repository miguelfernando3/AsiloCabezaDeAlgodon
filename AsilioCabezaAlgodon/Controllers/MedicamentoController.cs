
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

        // Mostrar medicamentos recetados y despachados
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var medicamentos = await _context.Medicamentos
                .AsNoTracking()
                .OrderBy(m => m.Estado == "Despachado")
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            ViewBag.Visitas = await _context.VisitasMedicas
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Citas = await _context.CitasMedicas
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes
                .AsNoTracking()
                .ToListAsync();

            return View(medicamentos);
        }

        // Mostrar formulario para recetar medicamento
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create()
        {
            await CargarVisitas();

            return View(new Medicamento
            {
                Estado = "Pendiente",
                Costo = 0,
                Descuento = 0
            });
        }

        // Guardar receta médica
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create(Medicamento medicamento)
        {
            medicamento.Id = 0;
            medicamento.Estado = "Pendiente";

            ModelState.Remove(nameof(Medicamento.Id));
            ModelState.Remove(nameof(Medicamento.Estado));

            if (string.IsNullOrWhiteSpace(medicamento.Nombre))
            {
                ModelState.AddModelError(
                    nameof(medicamento.Nombre),
                    "Ingrese el nombre del medicamento.");
            }
            else if (medicamento.Nombre.Trim().Length > 200)
            {
                ModelState.AddModelError(
                    nameof(medicamento.Nombre),
                    "El nombre no puede superar 200 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(medicamento.Dosis))
            {
                ModelState.AddModelError(
                    nameof(medicamento.Dosis),
                    "Ingrese la dosis del medicamento.");
            }
            else if (medicamento.Dosis.Trim().Length > 200)
            {
                ModelState.AddModelError(
                    nameof(medicamento.Dosis),
                    "La dosis no puede superar 200 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(medicamento.Duracion))
            {
                ModelState.AddModelError(
                    nameof(medicamento.Duracion),
                    "Ingrese la duración del tratamiento.");
            }
            else if (medicamento.Duracion.Trim().Length > 200)
            {
                ModelState.AddModelError(
                    nameof(medicamento.Duracion),
                    "La duración no puede superar 200 caracteres.");
            }

            // Validar costo
            if (medicamento.Costo < 0)
            {
                ModelState.AddModelError(
                    nameof(medicamento.Costo),
                    "El costo no puede ser negativo.");
            }

            // Validar descuento de la fundación
            if (medicamento.Descuento < 0)
            {
                ModelState.AddModelError(
                    nameof(medicamento.Descuento),
                    "El descuento no puede ser negativo.");
            }

            if (medicamento.Descuento > medicamento.Costo)
            {
                ModelState.AddModelError(
                    nameof(medicamento.Descuento),
                    "El descuento no puede superar el costo del medicamento.");
            }

            // Validar visita médica
            if (!medicamento.VisitaMedicaId.HasValue ||
                medicamento.VisitaMedicaId.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(medicamento.VisitaMedicaId),
                    "Seleccione una visita médica.");
            }
            else
            {
                bool existeVisita = await _context.VisitasMedicas
                    .AnyAsync(v =>
                        v.Id == medicamento.VisitaMedicaId.Value);

                if (!existeVisita)
                {
                    ModelState.AddModelError(
                        nameof(medicamento.VisitaMedicaId),
                        "La visita médica seleccionada no existe.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarVisitas();
                return View(medicamento);
            }

            medicamento.Nombre = medicamento.Nombre.Trim();
            medicamento.Dosis = medicamento.Dosis.Trim();
            medicamento.Duracion = medicamento.Duracion.Trim();

            _context.Medicamentos.Add(medicamento);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Medicamento recetado correctamente. Pendiente de despacho.";

            return RedirectToAction(nameof(Index));
        }

        // Confirmar despacho de un medicamento
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Farmacia")]
        public async Task<IActionResult> Despachar(int id)
        {
            if (id <= 0)
                return BadRequest();

            // Evitar despachar dos veces
            int actualizados = await _context.Medicamentos
                .Where(m => m.Id == id &&
                            m.Estado == "Pendiente" &&
                            m.VisitaMedicaId != null)
                .ExecuteUpdateAsync(cambios => cambios
                    .SetProperty(
                        m => m.Estado,
                        "Despachado"));

            if (actualizados == 0)
            {
                TempData["Aviso"] =
                    "No se pudo despachar el medicamento. " +
                    "Verifique que esté pendiente y tenga una visita médica.";
            }
            else
            {
                TempData["Mensaje"] =
                    "Medicamento despachado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Cargar visitas médicas y pacientes
        private async Task CargarVisitas()
        {
            var datos = await _context.VisitasMedicas
                .AsNoTracking()
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
                Descripcion =
                    dato.Nombre + " " + dato.Apellido +
                    " - " + dato.Fecha.ToString("dd/MM/yyyy")
            }).ToList();

            ViewBag.ListaVisitas = new SelectList(
                visitas,
                "Id",
                "Descripcion");
        }
    }
}
