using JitkaApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JitkaApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)  {}
        public DbSet<Product> Produkty { get; set; }

        public DbSet<KosikovaPolozka> KosikovePolozky { get; set; }


    }
}
