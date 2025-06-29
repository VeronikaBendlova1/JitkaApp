// Načtení vlastních tříd (Data, Models), identity a EF Core
using JitkaApp.Data;
using JitkaApp.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IO;

// Vytvoření builderu aplikace – základ pro konfiguraci a DI
var builder = WebApplication.CreateBuilder(args);

// Načtení connection stringu z appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Registrace EF Core s PostgreSQL (Npgsql)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Umožňuje získat HttpContext odkudkoli v aplikaci
builder.Services.AddHttpContextAccessor();

// Konfigurace identity (přihlášení, role, uživatelé)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>() // Umožňuje práci s rolemi (např. Admin)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Načtení nastavení e-mailu z appsettings.json do modelu EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<EmailSettings>>().Value);

// MVC: Registrace kontrolerů a razor viewů
builder.Services.AddControllersWithViews();

// Paměťová cache + konfigurace Session (trvání, cookies)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Konfigurace cookies pro Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.AddRazorPages();
// Vytvoření instance aplikace
var app = builder.Build();

// Vývojové/provozní prostředí
if (app.Environment.IsDevelopment())
{
    // app.UseMigrationsEndPoint(); // můžeš aktivovat pro migrace ve vývoji
}
else
{
    app.UseExceptionHandler("/Home/Error"); // uživatelsky přívětivá chybová stránka
    app.UseHsts(); // bezpečnostní hlavička (HTTPS)
}

// Přesměrování na HTTPS
app.UseHttpsRedirection();

// Zajišťuje, že se budou statické soubory (wwwroot) obsluhovat
app.UseStaticFiles();

// Aktivace session (musí být před Authentication!)
app.UseSession();

// Konfigurace směrování požadavků
app.UseRouting();

// ✅ Aktivace Identity – AUTENTIZACE (musí být před Authorization)
app.UseAuthentication();

// Autorizace (např. omezení přístupu dle role)
app.UseAuthorization();

//test admin
app.Use(async (context, next) =>
{
    var user = context.User;
    Console.WriteLine($"IsAuthenticated: {user.Identity.IsAuthenticated}, Name: {user.Identity.Name}");
    await next.Invoke();
});

// Mapování kontrolerů (např. HomeController -> /Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapování razor pages (/Identity atd.)
app.MapRazorPages();

// --- Opraveno: odstraněno `.WithStaticAssets()` z neexistujících metod ---
// Pokud máš vlastní extension metodu `WithStaticAssets`, zkontroluj, že je správně definována a naimportována.

// --- Automatická role Admin ---

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string roleName = "Admin";
    string adminEmail = "brushbabycoloring@gmail.com";

    // Pokus o vytvoření role a přiřazení role uživateli
    int pokusy = 0;
    const int maxPokusy = 3;
    bool hotovo = false;

    while (!hotovo && pokusy < maxPokusy)
    {
        try
        {
            pokusy++;

            // Pokud role neexistuje, vytvoří ji
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Najde uživatele a přiřadí mu roli Admin
            var user = await userManager.FindByEmailAsync(adminEmail);
            if (user != null && !await userManager.IsInRoleAsync(user, roleName))
            {
                await userManager.AddToRoleAsync(user, roleName);
            }

            hotovo = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❗ Chyba při nastavování role Admin (pokus {pokusy}): {ex.Message}");
            if (pokusy < maxPokusy)
            {
                await Task.Delay(2000); // Počkej 2 sekundy a zkus to znovu
            }
            else
            {
                Console.WriteLine("❌ Nepodařilo se nastavit roli Admin po několika pokusech.");
            }
        }
    }
}

// Spuštění aplikace
app.Run();
