using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JitkaApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Nazev { get; set; } = "";
        public string Popis { get; set; } = "";
        [Range(1, 10000)]
        public decimal Cena { get; set; } = 0;
        public string Obrazek { get; set; } = "";
        public string Cesta { get; set; } = "";
     

    }
    [Table("kosikovepolozky")]
    public class KosikovaPolozka
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("Produkt")]
        [Column("produktid")]
        public int ProduktId { get; set; }  // ← klíč na Product
        [Column("pocet")]
        public int Pocet { get; set; } = 1;
        public Product Produkt { get; set; }  // ← navigační vlastnost

        [Column("uzivatelsessionid")]
        public string? UzivatelSessionId { get; set; } = "";
    }
    

    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
    }
}
