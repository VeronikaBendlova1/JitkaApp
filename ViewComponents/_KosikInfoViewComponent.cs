using Microsoft.AspNetCore.Mvc;
using JitkaApp.Data;

public class KosikInfoViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public KosikInfoViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public IViewComponentResult Invoke()
    {
        var sessionId = HttpContext.Session.GetString("SessionId");
        int pocet = 0;

        if (!string.IsNullOrEmpty(sessionId))
        {
            pocet = _context.KosikovePolozky
                .Where(p => p.UzivatelSessionId == sessionId)
                .Sum(p => p.Pocet);
        }

        return View(pocet); // Předáváme počet do view
    }
}
