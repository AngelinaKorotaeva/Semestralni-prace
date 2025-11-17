using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Models;

namespace SkolniJidelna.Data
{
    public class AppDbContext : DbContext
    {

        private readonly IConfiguration _configuration;

        public AppDbContext()
        {
        }

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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("OracleDb");
                optionsBuilder.UseOracle(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Adresa>().ToTable("ADRESY");
            modelBuilder.Entity<StravnikAlergie>().ToTable("ALERGIE_STRAVNICI");
            modelBuilder.Entity<Jidlo>().ToTable("JIDLA");
            modelBuilder.Entity<Log>().ToTable("LOGY");
            modelBuilder.Entity<Objednavka>().ToTable("OBJEDNAVKY");
            modelBuilder.Entity<StravnikOmezeni>().ToTable("OMEZENI_STRAVNICI");
            modelBuilder.Entity<Platba>().ToTable("PLATBY");
            modelBuilder.Entity<Polozka>().ToTable("POLOZKY");
            modelBuilder.Entity<Pracovnik>().ToTable("PRACOVNICI");
            modelBuilder.Entity<Slozka>().ToTable("SLOZKY");
            modelBuilder.Entity<SlozkaJidlo>().ToTable("SLOZKY_JIDLA");
            modelBuilder.Entity<Stav>().ToTable("STAVY");
            modelBuilder.Entity<Stravnik>().ToTable("STRAVNICI");
            modelBuilder.Entity<Student>().ToTable("STUDENTI");
            modelBuilder.Entity<Trida>().ToTable("TRIDY");
        }
    }
}