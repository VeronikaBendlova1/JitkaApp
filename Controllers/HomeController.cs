using System.Diagnostics;
using JitkaApp.Data;
using JitkaApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace JitkaApp.Controllers
{
    public class HomeController : Controller
    {
          private readonly ILogger<HomeController> _logger;
          private readonly ApplicationDbContext _context;

           public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
           {
               _context = context;
               _logger = logger;
           }

           public IActionResult Index()
           {
               return View();
           }

         

           [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
           public IActionResult Error()
           {
               return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
           }

           public IActionResult Kontakt()
           {
               return View();
           }
           public IActionResult Galerie()
           {
             var produkty = _context.Produkty.ToList();
               return View(produkty);
           }
    }


}
