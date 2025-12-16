using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    public class OrderViewModel : BaseViewModel
    {
        private string _selectedTypMenu = "Vše";
        private ObservableCollection<MenuWithJidla> _menus = new();
        private ObservableCollection<CartItem> _cartItems = new();
        private double _total;
        private DateTime? _selectedDate = DateTime.Today;

        public string SelectedTypMenu { get => _selectedTypMenu; set { _selectedTypMenu = value; RaisePropertyChanged(); } }
        public ObservableCollection<MenuWithJidla> Menus { get => _menus; set { _menus = value; RaisePropertyChanged(); } }
        public ObservableCollection<CartItem> CartItems { get => _cartItems; set { _cartItems = value; UpdateTotal(); RaisePropertyChanged(); } }
        public DateTime? SelectedDate { get => _selectedDate; set { _selectedDate = value; RaisePropertyChanged(); } }
        public double Total { get => _total; private set { _total = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TotalFormatted)); } }
        public string TotalFormatted => $"Celkem: {Total:0.##} Kč";

        public class JidloItem
        {
            public int IdJidlo { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public string? Popis { get; set; }
            public double Cena { get; set; }
            public string? Poznamka { get; set; }
        }

        public class MenuWithJidla
        {
            public int IdMenu { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public string TypMenu { get; set; } = string.Empty;
            public ObservableCollection<JidloItem> Jidla { get; set; } = new();
        }

        public class CartItem : BaseViewModel
        {
            private int _mnozstvi;
            private double _cenaJednotkova;
            public int IdJidlo { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public double CenaJednotkova { get => _cenaJednotkova; set { _cenaJednotkova = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(CenaCelkem)); } }
            public int Mnozstvi { get => _mnozstvi; set { _mnozstvi = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(CenaCelkem)); } }
            public double CenaCelkem => Math.Round(CenaJednotkova * Mnozstvi, 2);
        }

        public void LoadMenus()
        {
            try
            {
                using var ctx = new AppDbContext();
                var menusQuery = ctx.Menu.AsNoTracking();
                var selected = (SelectedTypMenu ?? "").Trim();
                if (!string.Equals(selected, "Vše", StringComparison.OrdinalIgnoreCase))
                {
                    var typ = selected.ToUpperInvariant();
                    menusQuery = menusQuery.Where(m => m.TypMenu != null && m.TypMenu.ToUpper() == typ);
                }

                var list = menusQuery
                    .OrderBy(m => m.IdMenu)
                    .Select(m => new MenuWithJidla
                    {
                        IdMenu = m.IdMenu,
                        Nazev = m.Nazev,
                        TypMenu = m.TypMenu ?? string.Empty,
                        Jidla = new ObservableCollection<JidloItem>(
                            ctx.Jidlo
                                .AsNoTracking()
                                .Where(j => j.IdMenu == m.IdMenu)
                                .OrderBy(j => j.Nazev)
                                .Select(j => new JidloItem
                                {
                                    IdJidlo = j.IdJidlo,
                                    Nazev = j.Nazev,
                                    Popis = j.Popis,
                                    Cena = j.Cena,
                                    Poznamka = j.Poznamka
                                })
                                .ToList())
                    })
                    .ToList();

                Menus = new ObservableCollection<MenuWithJidla>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri nacitani menu: " + ex.Message);
                Menus = new ObservableCollection<MenuWithJidla>();
            }
        }

        public void AddToOrder(JidloItem item)
        {
            if (item == null) return;
            var existing = CartItems.FirstOrDefault(ci => ci.IdJidlo == item.IdJidlo);
            if (existing == null)
            {
                CartItems.Add(new CartItem
                {
                    IdJidlo = item.IdJidlo,
                    Nazev = item.Nazev,
                    CenaJednotkova = item.Cena,
                    Mnozstvi = 1
                });
            }
            else
            {
                existing.Mnozstvi += 1;
            }
            UpdateTotal();
        }

        public void RemoveOneFromOrder(int idJidlo)
        {
            var existing = CartItems.FirstOrDefault(ci => ci.IdJidlo == idJidlo);
            if (existing == null) return;
            if (existing.Mnozstvi > 1)
            {
                existing.Mnozstvi -= 1;
            }
            else
            {
                CartItems.Remove(existing);
            }
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            Total = CartItems.Sum(ci => ci.CenaCelkem);
        }

        public enum PaymentMethod
        {
            Card,
            Account,
            Cash
        }

        public void CreateOrder(string email, PaymentMethod method, string? note)
        {
            using var ctx = new AppDbContext();
            var stravnik = ctx.Stravnik.AsNoTracking().FirstOrDefault(s => s.Email == email);
            if (stravnik == null)
                throw new InvalidOperationException("Uživatel nenalezen");

            // For account payment, ensure sufficient balance
            if (method == PaymentMethod.Account)
            {
                var needed = Convert.ToDecimal(Total);
                var balance = Convert.ToDecimal(stravnik.Zustatek);
                if (balance < needed)
                    throw new InvalidOperationException("Nedostatečný zůstatek na účtu.");
            }

            var dbConn = ctx.Database.GetDbConnection();
            var wasClosed = dbConn.State == System.Data.ConnectionState.Closed;
            if (wasClosed) dbConn.Open();

            var conn = dbConn as OracleConnection;
            if (conn == null)
                throw new InvalidOperationException("OracleConnection není dostupné");

            using var tx = conn.BeginTransaction();
            try
            {
                // Insert order directly to avoid broken P_INSERT_OBJ OUT parameter for id_stav
                var stavId = (method == PaymentMethod.Cash) ? 4 : 1;
                var dateVal = SelectedDate ?? DateTime.Today;
                var noteVal = string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note;

                using var cmdIns = new OracleCommand("INSERT INTO objednavky (id_objednavka, datum, celkova_cena, poznamka, id_stravnik, id_stav) VALUES (S_OBJ.NEXTVAL, :d, 0, :p, :sId, :stav) RETURNING id_objednavka INTO :id", conn)
                {
                    CommandType = System.Data.CommandType.Text,
                    Transaction = tx
                };
                cmdIns.Parameters.Add(":d", OracleDbType.Date).Value = dateVal;
                cmdIns.Parameters.Add(":p", OracleDbType.Varchar2).Value = noteVal;
                cmdIns.Parameters.Add(":sId", OracleDbType.Int32).Value = stravnik.IdStravnik;
                cmdIns.Parameters.Add(":stav", OracleDbType.Int32).Value = stavId;
                var idParam = new OracleParameter(":id", OracleDbType.Int32, System.Data.ParameterDirection.ReturnValue);
                cmdIns.Parameters.Add(idParam);
                cmdIns.ExecuteNonQuery();
                var objednavkaId = Convert.ToInt32(idParam.Value.ToString());

                foreach (var ci in CartItems)
                {
                    using var cmdItem = new OracleCommand("P_INSERT_POLOZKA", conn)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure,
                        Transaction = tx
                    };
                    cmdItem.Parameters.Add("P_ID_OBJEDNAVKA", OracleDbType.Int32).Value = objednavkaId;
                    cmdItem.Parameters.Add("P_ID_JIDLO", OracleDbType.Int32).Value = ci.IdJidlo;
                    cmdItem.Parameters.Add("P_MNOZSTVI", OracleDbType.Int32).Value = ci.Mnozstvi;
                    cmdItem.ExecuteNonQuery();
                }

                using var cmdUpd = new OracleCommand("UPDATE objednavky SET celkova_cena = :c WHERE id_objednavka = :id", conn)
                {
                    CommandType = System.Data.CommandType.Text,
                    Transaction = tx
                };
                cmdUpd.Parameters.Add(":c", OracleDbType.Decimal).Value = Convert.ToDecimal(Total);
                cmdUpd.Parameters.Add(":id", OracleDbType.Int32).Value = objednavkaId;
                cmdUpd.ExecuteNonQuery();

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                if (wasClosed) dbConn.Close();
            }
        }
    }
}
