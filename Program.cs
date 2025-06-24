using JitkaApp.Data;
using JitkaApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));





builder.Services.AddHttpContextAccessor();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>() // ← důležité pro role
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<EmailSettings>>().Value);

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});






var app = builder.Build();

// Middleware pro vývojové/provozní prostředí
if (app.Environment.IsDevelopment())
{
   // app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseSession(); // musí být PŘED UseEndpoints nebo UseRouting






app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// admin
// admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string roleName = "Admin";
    string adminEmail = "brushbabycoloring@gmail.com"; // ← nahraď svým e-mailem

    // ✅ Přidáno: retry mechanismus + ošetření výpadků DB při startu
    int pokusy = 0;
    const int maxPokusy = 3;
    bool hotovo = false;

    while (!hotovo && pokusy < maxPokusy)
    {
        try
        {
            pokusy++;

            // 1. Vytvoří roli "Admin", pokud ještě neexistuje
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 2. Najdi uživatele a přiřaď mu roli Admin
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
                await Task.Delay(2000); // čekej 2 sekundy před dalším pokusem
            }
            else
            {
                Console.WriteLine("❌ Nepodařilo se nastavit roli Admin po několika pokusech.");
            }
        }
    }
}







app.Run();
