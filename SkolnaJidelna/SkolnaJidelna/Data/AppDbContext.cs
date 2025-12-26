using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using SkolniJidelna.Models;

namespace SkolniJidelna.Data
{
    // EF Core DbContext pro Oracle – na??t? connection string z appsettings.json, loguje SQL a mapuje entity/pohledy
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        // Bezparametrick? konstruktor – pro ru?n? vytvo?en? kontextu bez DI
        public AppDbContext() { }

        // Konstruktor s DI – umo??uje p?edat DbContextOptions a IConfiguration
        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        // DbSety – tabulky a pohledy
        public DbSet<Adresa> Adresa { get; set; }
        public DbSet<Alergie> Alergie { get; set; }
        public DbSet<StravnikAlergie> StravnikAlergie { get; set; }
        public DbSet<DietniOmezeni> DietniOmezeni { get; set; }
        public DbSet<Jidlo> Jidlo { get; set; }
        public DbSet<Log> Log { get; set; }
        public DbSet<Menu> Menu { get; set; }
        public DbSet<Objednavka> Objednavka { get; set; }
        public DbSet<StravnikOmezeni> StravnikOmezeni { get; set; }
        public DbSet<Platba> Platba { get; set; }
        public DbSet<Polozka> Polozka { get; set; }
        public DbSet<Pozice> Pozice { get; set; }
        public DbSet<Pracovnik> Pracovnik { get; set; }
        public DbSet<Slozka> Slozka { get; set; }
        public DbSet<SlozkaJidlo> SlozkaJidlo { get; set; }
        public DbSet<Stav> Stav { get; set; }
        public DbSet<Stravnik> Stravnik { get; set; }
        public DbSet<Student> Student { get; set; }
        public DbSet<Trida> Trida { get; set; }
        public DbSet<Soubor> Soubor { get; set; }

        public DbSet<VStravnikLogin> VStravnikLogin { get; set; }
        public DbSet<VStudTrida> VStudTrida { get; set; }
        public DbSet<VPracovnikPozice> VPracovnikPozice { get; set; }
        public DbSet<VJidlaSlozeni> VJidlaSlozeni { get; set; }
        public DbSet<VObjHistorieDetail> VObjHistorieDetail { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Pokud nen? kontext nakonfigurov?n p?es DI, na?ti connection string z appsettings.json a aktivuj logov?n? SQL
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("OracleDb");

                optionsBuilder
                    .UseOracle(connectionString)
                    .EnableSensitiveDataLogging()
                    .LogTo(msg =>
                    {
                        var p = Path.Combine(AppContext.BaseDirectory, "ef-sql.log");
                        File.AppendAllText(p, msg + Environment.NewLine);
                        System.Diagnostics.Debug.WriteLine(msg);
                    }, new[] { Microsoft.EntityFrameworkCore.DbLoggerCategory.Database.Command.Name });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konverze bool -> int (1/0) kv?li Oracle, a explicitn? mapov?n? tabulek, kl??? a pohled?
            var boolToInt = new ValueConverter<bool, int>(v => v ? 1 : 0, v => v == 1);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(bool)))
            {
                property.SetValueConverter(boolToInt);
            }

            base.OnModelCreating(modelBuilder);

            // Mapov?n? n?zv? tabulek a slo?en?ch kl???
            modelBuilder.Entity<Adresa>().ToTable("ADRESY");
            modelBuilder.Entity<StravnikAlergie>().ToTable("STRAVNICI_ALERGIE");
            modelBuilder.Entity<Jidlo>().ToTable("JIDLA");
            modelBuilder.Entity<Log>().ToTable("LOGY");
            modelBuilder.Entity<Objednavka>().ToTable("OBJEDNAVKY");
            modelBuilder.Entity<StravnikOmezeni>().ToTable("STRAVNICI_OMEZENI");
            modelBuilder.Entity<Platba>().ToTable("PLATBY");
            modelBuilder.Entity<Polozka>().ToTable("POLOZKY");
            modelBuilder.Entity<Pracovnik>().ToTable("PRACOVNICI");
            modelBuilder.Entity<Slozka>().ToTable("SLOZKY");
            modelBuilder.Entity<SlozkaJidlo>().ToTable("SLOZKY_JIDLA");
            modelBuilder.Entity<Stav>().ToTable("STAVY");
            modelBuilder.Entity<Stravnik>().ToTable("STRAVNICI");
            modelBuilder.Entity<Student>().ToTable("STUDENTI");
            modelBuilder.Entity<Trida>().ToTable("TRIDY");
            modelBuilder.Entity<Soubor>().ToTable("SOUBORY");

            modelBuilder.Entity<Polozka>().HasKey(p => new { p.IdJidlo, p.IdObjednavka });
            modelBuilder.Entity<SlozkaJidlo>().HasKey(sj => new { sj.IdJidlo, sj.IdSlozka });
            modelBuilder.Entity<StravnikAlergie>().HasKey(sa => new { sa.IdStravnik, sa.IdAlergie });
            modelBuilder.Entity<StravnikOmezeni>().HasKey(so => new { so.IdStravnik, so.IdOmezeni });
            modelBuilder.Entity<Stravnik>().Property(s => s.IdStravnik).HasColumnName("ID_STRAVNIK").ValueGeneratedOnAdd().HasDefaultValueSql("S_STR.NEXTVAL");

            // Mapov?n? pohled?
            modelBuilder.Entity<VStravnikLogin>().ToView("V_STRAVNICI_LOGIN").HasNoKey();
            modelBuilder.Entity<VStudTrida>().ToView("V_STUD_TRIDA").HasNoKey();
            modelBuilder.Entity<VPracovnikPozice>().ToView("V_PR_POZICE").HasNoKey();
            modelBuilder.Entity<VJidlaSlozeni>().ToView("V_JIDLA_SLOZENI").HasNoKey();
            modelBuilder.Entity<VObjHistorieDetail>().ToView("V_OBJEDNAVKY_HISTORIE_DETAIL").HasNoKey();
        }
    }
}