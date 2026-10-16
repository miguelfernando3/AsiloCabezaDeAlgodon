
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

            var usuario = await _userManager.FindByEmailAsync(modelo.Correo);

            if (usuario != null)
            {
                var resultado = await _signInManager.PasswordSignInAsync(
                    usuario,
                    modelo.Contrasena,
                    modelo.Recordarme,
                    lockoutOnFailure: true);

                if (resultado.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (resultado.IsLockedOut)
                {
                    ModelState.AddModelError("",
                        "Cuenta bloqueada temporalmente por varios intentos fallidos.");
                    return View(modelo);
                }
            }

            ModelState.AddModelError("",
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
