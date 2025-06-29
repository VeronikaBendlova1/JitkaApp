using JitkaApp.Data;
using JitkaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JitkaApp.Controllers
{
    public class AdminController : Controller
    {
       private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")] //Admin/PridatProdukt
        public async Task<IActionResult> PridatProduktAsync()
        {
            if (_signInManager.IsSignedIn(User))
            {
                var user = await _userManager.GetUserAsync(User);
                ViewBag.LoggedUser = user?.Email ?? "neznámý";
            }
            else
            {
                ViewBag.LoggedUser = "Nepřihlášen";
            }

            return View(); // zobrazí formulář
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult PridatProdukt(Product produkt)
        {
            produkt.Id = _context.Produkty.Max(x => x.Id) + 1;

            if (ModelState.IsValid)
            {

                _context.Add(produkt);
                _context.SaveChanges();
                ViewBag.PridanyProdukt = $"{produkt.Nazev} byl úspěšně přidán do databáze";
                return RedirectToAction("PridatProdukt");
                
            }
            return View();
        }
    }
}
