
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuarioController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Mostrar usuarios registrados
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users.ToListAsync();

            var lista = new List<AsilioCabezaAlgodon.Models.UsuarioListaViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);

                lista.Add(new AsilioCabezaAlgodon.Models.UsuarioListaViewModel
                {
                    Id = usuario.Id,
                    Correo = usuario.Email ?? "",
                    Rol = string.Join(", ", roles)
                });
            }

            return View(lista);
        }

        // Mostrar formulario de registro
        [HttpGet]
        public IActionResult Create()
        {
            CargarRoles();
            return View(new AsilioCabezaAlgodon.Models.RegistrarUsuarioViewModel());
        }

        // Registrar usuario y asignar rol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AsilioCabezaAlgodon.Models.RegistrarUsuarioViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                CargarRoles();
                return View(modelo);
            }

            bool existeRol = await _roleManager.RoleExistsAsync(modelo.Rol);

            if (!existeRol)
            {
                ModelState.AddModelError("Rol", "El cargo seleccionado no existe.");
                CargarRoles();
                return View(modelo);
            }

            var usuarioExistente = await _userManager.FindByEmailAsync(modelo.Correo);

            if (usuarioExistente != null)
            {
                ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                CargarRoles();
                return View(modelo);
            }

            var usuario = new IdentityUser
            {
                UserName = modelo.Correo,
                Email = modelo.Correo,
                EmailConfirmed = false
            };

            var resultado = await _userManager.CreateAsync(
                usuario, modelo.Contrasena);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                CargarRoles();
                return View(modelo);
            }

            var resultadoRol = await _userManager.AddToRoleAsync(
                usuario, modelo.Rol);

            if (!resultadoRol.Succeeded)
            {
                await _userManager.DeleteAsync(usuario);

                ModelState.AddModelError(
                    "",
                    "No se pudo asignar el cargo al usuario.");

                CargarRoles();
                return View(modelo);
            }

            return RedirectToAction("Index");
        }

        private void CargarRoles()
        {
            ViewBag.Roles = new List<string>
            {
                "Administrador",
                "Recepcionista",
                "Medico",
                "Enfermero",
                "Laboratorio",
                "Farmacia",
                "Fundacion"
            };
        }
    }
}
