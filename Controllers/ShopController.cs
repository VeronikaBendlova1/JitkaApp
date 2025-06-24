using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JitkaApp.Models;
using System.Net;
using System.Net.Mail;
using System.Text;
using JitkaApp.Data;
using Microsoft.AspNetCore.Authorization;


namespace JitkaApp.Controllers
{
    public class ShopController : Controller
    {
        
        private readonly EmailSettings _emailSettings;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ShopController( EmailSettings emailSettings, ApplicationDbContext context, IConfiguration configuration)
        {
            
            _emailSettings = emailSettings;
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            


            return View();
        }

        public IActionResult Obchod()
        {
            var produkty = _context.Produkty.ToList();
            return View(produkty);
        }

        public IActionResult PridatDoKosiku(int id)
        {
            if (HttpContext.Session.GetString("SessionId") == null)
            {
                HttpContext.Session.SetString("SessionId", Guid.NewGuid().ToString());
            }

            var sessionId = HttpContext.Session.GetString("SessionId");

            var produkt = _context.Produkty.FirstOrDefault(p => p.Id == id);

            if (produkt == null)
            {
                return NotFound();
            }
            
                // Zkontroluj, jestli už je položka v košíku pro danou session
                var jePolozkaVkosiku = _context.KosikovePolozky.FirstOrDefault(p => p.ProduktId == id && p.UzivatelSessionId == sessionId);

                if (jePolozkaVkosiku != null)
                {

                    // Navýšíme počet, pokud už existuje
                    jePolozkaVkosiku.Pocet++;
                

            }
                else
                {
                    jePolozkaVkosiku = new KosikovaPolozka
                    {
                        ProduktId = produkt.Id,
                        UzivatelSessionId = sessionId

                    };

                TempData["PridatDoKosiku"] = $"Úspěšně přidáno do košíku: {produkt.Nazev}";

                _context.KosikovePolozky.Add(jePolozkaVkosiku);
              
            }

            
            
            
            _context.SaveChanges();

            return RedirectToAction("Obchod");
        }

        public IActionResult Kosik()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");

            var polozky = _context.KosikovePolozky
                .Include(p => p.Produkt)
                .Where(p => p.UzivatelSessionId == sessionId)
                .ToList();

            ViewBag.Celkem = polozky.Sum(p => p.Pocet * p.Produkt.Cena);
            return View(polozky);
        }

        public IActionResult VycistitKosik()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");

            var polozky = _context.KosikovePolozky
                .Where(p => p.UzivatelSessionId == sessionId)
                .ToList();

            _context.KosikovePolozky.RemoveRange(polozky);
            _context.SaveChanges();

            return RedirectToAction("Kosik");
        }

        public IActionResult OdebratJednuPolozku(int id)
        {
            var sessionId = HttpContext.Session.GetString("SessionId");

            var polozka = _context.KosikovePolozky
                .FirstOrDefault(p => p.Id == id && p.UzivatelSessionId == sessionId);

            if (polozka != null)
            {
                polozka.Pocet--;
                

                if (polozka.Pocet <= 0)
                {
                    _context.KosikovePolozky.Remove(polozka);
                }
            }

            _context.SaveChanges();

            return RedirectToAction("Kosik");
        }

        [HttpPost]
        public IActionResult OdeslatObjednavku(string jmeno, string prijmeni, string ulice, string mesto, string psc, string email, string poznamky)
        {
            try
            {
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (string.IsNullOrEmpty(sessionId))
                {
                    ViewBag.Zprava = "Košík je prázdný.";
                    return View("Kosik");
                }

                var polozky = _context.KosikovePolozky
                    .Include(p => p.Produkt)
                    .Where(p => p.UzivatelSessionId == sessionId)
                    .ToList();

                if (!polozky.Any())
                {
                    ViewBag.Zprava = "Košík je prázdný.";
                    return View("Kosik");
                }

                var sb = new StringBuilder();
                sb.AppendLine($"OBJEDNÁVKA OD: {jmeno} {prijmeni}");
                sb.AppendLine($"Adresa: {ulice}, {mesto}, {psc}");
                sb.AppendLine($"E-mail: {email}");
                sb.AppendLine($"Poznámky: {poznamky}");
                sb.AppendLine("\n--- OBSAH KOŠÍKU ---");

                decimal celkovaCena = 0;
                foreach (var p in polozky)
                {
                    sb.AppendLine($"{p.Produkt.Nazev} - {p.Produkt.Cena} Kč");
                    celkovaCena += p.Produkt.Cena;
                }

                sb.AppendLine($"--- CELKEM: {celkovaCena} Kč ---");

                var smtp = new SmtpClient(_emailSettings.SmtpServer)
                {
                    Port = _emailSettings.SmtpPort,
                    Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword),
                    EnableSsl = true
                };

                var message = new MailMessage(
                    from: _emailSettings.SmtpUser,
                    to: _emailSettings.SmtpUser,
                    subject: "Nová objednávka",
                    body: sb.ToString()
                );
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community; // kvůli licenci, aby to neházelo chybu

                var pdf = FakturaGenerator.VytvorFakturu(
                cisloFaktury: "2025-001", // třeba generovat dynamicky
                zakaznikJmeno: $"{jmeno} {prijmeni}",
                zakaznikAdresa: $"{ulice}, {mesto}, {psc}",
                polozky: polozky.Select(p => (p.Produkt.Nazev, p.Pocet, p.Produkt.Cena)).ToList()
                );

                Directory.CreateDirectory("faktury"); // zajistí, že složka existuje
                System.IO.File.WriteAllBytes("faktury/zaloha-001.pdf", pdf);

                Console.WriteLine("PDF byl uložen.");
                // Připojit PDF jako přílohu
                var attachment = new Attachment(new MemoryStream(pdf), "zaloha.pdf", "application/pdf");
                message.Attachments.Add(attachment);


                smtp.Send(message);

                ViewBag.Zprava = "Objednávka byla úspěšně odeslána!";
                ViewBag.TypZpravy = "success";
                _context.KosikovePolozky.RemoveRange(polozky);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Zprava = "Chyba při odesílání: " + ex.Message;
                ViewBag.TypZpravy = "danger"; // warning nebo info (modrá)
            }

            return View("Kosik");
        }




    }
}
