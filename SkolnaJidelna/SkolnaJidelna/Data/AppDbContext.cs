using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkolnaJidelna.Models;

namespace SkolnaJidelna.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Adresa> Adresa { get; set; }
        public DbSet<Alergie> Alergie { get; set; }
        public DbSet<AlergieStravnik> AlergieStravnik { get; set; }
        public DbSet<DietniOmezeni> DietniOmezeni { get; set; }
        public DbSet<Jidlo> Jidlo { get; set; }
        public DbSet<Log> Log { get; set; }
        public DbSet<Menu> Menu { get; set; }
        public DbSet<Objednavka> Objednavka { get; set; }
        public DbSet<OmezeniStravnik> OmezeniStravnik { get; set; }
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
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Adresa>().ToTable("ADRESY");
            modelBuilder.Entity<AlergieStravnik>().ToTable("ALERGIE_STRAVNICI");
            modelBuilder.Entity<Jidlo>().ToTable("JIDLA");
            modelBuilder.Entity<Log>().ToTable("LOGY");
            modelBuilder.Entity<Objednavka>().ToTable("OBJEDNAVKY");
            modelBuilder.Entity<OmezeniStravnik>().ToTable("OMEZENI_STRAVNICI");
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