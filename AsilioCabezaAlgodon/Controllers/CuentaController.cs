
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AsilioCabezaAlgodon.Models;

namespace AsilioCabezaAlgodon.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public CuentaController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string[] rolesPermitidos =
            {
                "Administrador",
                "Recepcionista",
                "Medico",
                "Enfermero",
                "Laboratorio",
                "Farmacia",
                "Fundacion"
            };

            if (!rolesPermitidos.Contains(modelo.RolSeleccionado))
            {
                ModelState.AddModelError(
                    "RolSeleccionado",
                    "Seleccione un cargo válido.");

                return View(modelo);
            }

            var usuario = await _userManager.FindByEmailAsync(modelo.Correo);

            if (usuario != null)
            {
                var resultado = await _signInManager.CheckPasswordSignInAsync(
                    usuario,
                    modelo.Contrasena,
                    lockoutOnFailure: true);

                if (resultado.Succeeded)
                {
                    bool tieneRol = await _userManager.IsInRoleAsync(
                        usuario,
                        modelo.RolSeleccionado);

                    if (!tieneRol)
                    {
                        ModelState.AddModelError(
                            "",
                            "El usuario no tiene permiso para ingresar con ese cargo.");

                        return View(modelo);
                    }

                    await _signInManager.SignInAsync(
                        usuario,
                        modelo.Recordarme);

                    return RedirectToAction("Index", "Home");
                }

                if (resultado.IsLockedOut)
                {
                    ModelState.AddModelError(
                        "",
                        "Cuenta bloqueada temporalmente por varios intentos fallidos.");

                    return View(modelo);
                }
            }

            ModelState.AddModelError(
                "",
                "Correo o contraseña incorrectos.");

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Cuenta");
        }

        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
