using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Models;

namespace SkolniJidelna.Data
{
    public class AppDbContext : DbContext
    {

        private readonly IConfiguration _configuration;

        // Bezparametrický konstruktor pro scénáøe, kdy DbContext vytváøíme ruènì bez DI.
        public AppDbContext()
        {
        }

        // Konstruktor používaný pøi DI – umožòuje pøedat nastavené DbContextOptions a IConfiguration.
        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

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
            // Pokud kontext nebyl nakonfigurován DI, naèti connection string z appsettings.json
            // a zapni logování SQL do ef-sql.log pro ladìní.
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
            // Pøevede všechny bool property na int (1/0) kvùli Oracle a nastaví mapování tabulek, klíèù a pohledù.
            var boolToInt = new ValueConverter<bool, int>(
               v => v ? 1 : 0,
               v => v == 1);

            foreach (var property in modelBuilder.Model
                         .GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(bool)))
            {
                property.SetValueConverter(boolToInt);
            }

            base.OnModelCreating(modelBuilder);

            // Explicitní názvy tabulek, klíèe složených tabulek a mapování pohledù.
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

            modelBuilder.Entity<Polozka>()
                .HasKey(p => new { p.IdJidlo, p.IdObjednavka });

            modelBuilder.Entity<SlozkaJidlo>()
                .HasKey(sj => new { sj.IdJidlo, sj.IdSlozka });

            modelBuilder.Entity<StravnikAlergie>()
                .HasKey(sa => new { sa.IdStravnik, sa.IdAlergie });

            modelBuilder.Entity<StravnikOmezeni>()
                .HasKey(so => new { so.IdStravnik, so.IdOmezeni });
            modelBuilder.Entity<Stravnik>()
                .Property(s => s.IdStravnik)
                .HasColumnName("ID_STRAVNIK")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("S_STR.NEXTVAL");

            modelBuilder.Entity<VStravnikLogin>()
                .ToView("V_STRAVNICI_LOGIN")
                .HasNoKey();

            modelBuilder.Entity<VStudTrida>()
                .ToView("V_STUD_TRIDA")
                .HasNoKey();

            modelBuilder.Entity<VPracovnikPozice>()
                .ToView("V_PR_POZICE")
                .HasNoKey();

            modelBuilder.Entity<VJidlaSlozeni>()
                .ToView("V_JIDLA_SLOZENI")
                .HasNoKey();

            modelBuilder.Entity<VObjHistorieDetail>()
                .ToView("V_OBJEDNAVKY_HISTORIE_DETAIL")
                .HasNoKey();
        }
    }
}