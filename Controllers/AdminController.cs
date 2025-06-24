using JitkaApp.Data;
using JitkaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JitkaApp.Controllers
{
    public class AdminController : Controller
    {
       private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")] //Admin/PridatProdukt
        public IActionResult PridatProdukt()
        {
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
