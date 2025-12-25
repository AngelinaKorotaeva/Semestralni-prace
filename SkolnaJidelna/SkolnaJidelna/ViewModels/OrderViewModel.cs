using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SkolniJidelna.ViewModels
{
    public class OrderViewModel : BaseViewModel
    {
        private string _selectedTypMenu = "Vše";
        private ObservableCollection<MenuWithJidla> _menus = new();
        private ObservableCollection<CartItem> _cartItems = new();
        private double _total;
        private DateTime? _selectedDate = DateTime.Today;
        private HashSet<string> _userAllergicProducts = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _userDietKeywords = new(StringComparer.OrdinalIgnoreCase);

        public string SelectedTypMenu { get => _selectedTypMenu; set { _selectedTypMenu = value; RaisePropertyChanged(); } }
        public ObservableCollection<MenuWithJidla> Menus { get => _menus; set { _menus = value; RaisePropertyChanged(); } }
        public ObservableCollection<CartItem> CartItems { get => _cartItems; set { _cartItems = value; UpdateTotal(); RaisePropertyChanged(); } }
        public DateTime? SelectedDate { get => _selectedDate; set { _selectedDate = value; RaisePropertyChanged(); } }
        public double Total { get => _total; private set { _total = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TotalFormatted)); } }
        public string TotalFormatted => $"Celkem: {Total:0.##} Kč";

        public class JidloItem : BaseViewModel
        {
            private bool _isAllergic;
            private string? _matchedAllergens;
            private bool _isDietConflict;
            private string? _matchedDietKeywords;
            public int IdJidlo { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public string? Popis { get; set; }
            public double Cena { get; set; }
            public string? Poznamka { get; set; }
            public bool IsAllergic { get => _isAllergic; set { _isAllergic = value; RaisePropertyChanged(); } }
            public string? MatchedAllergens { get => _matchedAllergens; set { _matchedAllergens = value; RaisePropertyChanged(); } }
            public bool IsDietConflict { get => _isDietConflict; set { _isDietConflict = value; RaisePropertyChanged(); } }
            public string? MatchedDietKeywords { get => _matchedDietKeywords; set { _matchedDietKeywords = value; RaisePropertyChanged(); } }
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

        // Načte menu bez filtru (zachována kompatibilita s původním voláním).
        public void LoadMenus()
        {
            LoadMenus(null);
        }

        // Načte menu s volitelným emailem pro označení alergenních/dietních položek uživatele.
        public void LoadMenus(string? email)
        {
            try
            {
                using var ctx = new AppDbContext();

                // Načti alergie a dietní omezení uživatele (pokud je email vyplněn).
                _userAllergicProducts.Clear();
                _userDietKeywords.Clear();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        var stravnik = ctx.Stravnik.AsNoTracking().FirstOrDefault(s => s.Email == email);
                        if (stravnik != null)
                        {
                            // Alergie -> produkty
                            var products = ctx.StravnikAlergie
                                .AsNoTracking()
                                .Where(sa => sa.IdStravnik == stravnik.IdStravnik)
                                .Join(ctx.Alergie, sa => sa.IdAlergie, a => a.IdAlergie, (sa, a) => a.Produkt)
                                .Where(p => p != null)
                                .Select(p => p!)
                                .ToList();
                            foreach (var p in products)
                            {
                                _userAllergicProducts.Add(Normalize(p));
                            }

                            // Dietní omezení -> klíčová slova do složek
                            var diets = ctx.StravnikOmezeni
                                .AsNoTracking()
                                .Where(so => so.IdStravnik == stravnik.IdStravnik)
                                .Join(ctx.DietniOmezeni, so => so.IdOmezeni, d => d.IdOmezeni, (so, d) => d.Nazev)
                                .Where(n => n != null)
                                .Select(n => n!)
                                .ToList();
                            var keywords = BuildDietKeywordsSet(diets);
                            foreach (var k in keywords)
                                _userDietKeywords.Add(k);
                        }
                    }
                    catch { }
                }

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

                // Označ položky, které kolidují s alergiemi/diety uživatele.
                if (_userAllergicProducts.Count > 0 || _userDietKeywords.Count > 0)
                {
                    foreach (var menu in list)
                    {
                        foreach (var item in menu.Jidla)
                        {
                            try
                            {
                                var slozkyRaw = ctx.SlozkaJidlo
                                    .AsNoTracking()
                                    .Where(sj => sj.IdJidlo == item.IdJidlo)
                                    .Join(ctx.Slozka, sj => sj.IdSlozka, s => s.IdSlozka, (sj, s) => s.Nazev)
                                    .ToList();

                                // Alergie
                                var matchedAllergens = new List<string>();
                                if (_userAllergicProducts.Count > 0)
                                {
                                    foreach (var ing in slozkyRaw)
                                    {
                                        var n = Normalize(ing);
                                        if (_userAllergicProducts.Contains(n) || _userAllergicProducts.Any(p => n.Contains(p) || p.Contains(n)))
                                        {
                                            matchedAllergens.Add(ing);
                                        }
                                    }
                                }
                                var matchedAllergensDistinct = matchedAllergens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                                item.IsAllergic = matchedAllergensDistinct.Count > 0;
                                item.MatchedAllergens = item.IsAllergic ? string.Join(", ", matchedAllergensDistinct) : null;
                                if (item.IsAllergic)
                                {
                                    item.Poznamka = string.IsNullOrWhiteSpace(item.Poznamka) ? "[ALERGIE]" : item.Poznamka + " [ALERGIE]";
                                }

                                // Dietní omezení
                                var matchedDiet = new List<string>();
                                if (_userDietKeywords.Count > 0)
                                {
                                    foreach (var ing in slozkyRaw)
                                    {
                                        var n = Normalize(ing);
                                        if (_userDietKeywords.Any(k => n.Contains(k) || k.Contains(n)))
                                        {
                                            matchedDiet.Add(ing);
                                        }
                                    }
                                }
                                var matchedDietDistinct = matchedDiet.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                                item.IsDietConflict = matchedDietDistinct.Count > 0;
                                item.MatchedDietKeywords = item.IsDietConflict ? string.Join(", ", matchedDietDistinct) : null;
                            }
                            catch
                            {
                                item.IsAllergic = false;
                                item.MatchedAllergens = null;
                                item.IsDietConflict = false;
                                item.MatchedDietKeywords = null;
                            }
                        }
                    }
                }

                Menus = new ObservableCollection<MenuWithJidla>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri nacitani menu: " + ex.Message);
                Menus = new ObservableCollection<MenuWithJidla>();
            }
        }

        // Postaví množinu klíčových slov pro dietní omezení (porovnává se se složkami jídel).
        private static HashSet<string> BuildDietKeywordsSet(IEnumerable<string> dietNames)
        {
            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Bezlepková dieta", new [] { "lepek" } },
                { "Bezlaktózová dieta", new [] { "laktoza", "mléko", "mleko", "smetana", "tvaroh", "sýr", "maslo" } },
                { "Vegetariánská dieta", new [] { "maso", "šunka", "sunka", "slanina", "kuřecí", "kureci", "hovězí", "hovezi", "vepřové", "veprove", "ryba", "ryby" } },
                { "Veganská dieta", new [] { "maso", "vejce", "mléko", "mleko", "sýr", "tvaroh", "smetana", "máslo", "maslo", "med" } },
                { "Bez ryb", new [] { "ryba", "ryby", "losos", "tuňák", "tunak" } },
                { "Bez vajec", new [] { "vejce" } },
                { "Bez ořechů", new [] { "ořechy", "orechy", "mandle", "arašídy", "arasidy", "lískové", "liskove", "vlašské", "vlasske", "sezam" } },
                { "Bez sóji", new [] { "sója", "soja" } },
                { "Bez cukru", new [] { "cukr" } },
                { "Bez soli", new [] { "sůl", "sul" } },
                { "Bez vepřového masa", new [] { "vepřové", "veprove", "slanina", "šunka", "sunka" } },
                { "Pescetariánská dieta", new [] { "maso", "kuřecí", "kureci", "hovězí", "hovezi", "vepřové", "veprove" } },
            };

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dn in dietNames)
            {
                if (string.IsNullOrWhiteSpace(dn)) continue;
                if (map.TryGetValue(dn.Trim(), out var kws))
                {
                    foreach (var k in kws)
                        set.Add(Normalize(k));
                }
            }
            return set;
        }

        // Normalizuje text (bez diakritiky, malé znaky, pouze písmena/čísla) pro robustní porovnání.
        private static string Normalize(string s)
        {
            var t = (s ?? string.Empty).Trim().ToLowerInvariant();
            t = t.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(t.Length);
            foreach (var c in t)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark && (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Přidá položku do košíku (zvýší množství, pokud už tam je) a přepočítá cenu.
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

        // Odebere jednu jednotku položky z košíku (nebo celou položku, pokud množství klesne na 0) a přepočítá cenu.
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

        // Přepočítá celkovou cenu košíku.
        private void UpdateTotal()
        {
            Total = CartItems.Sum(ci => ci.CenaCelkem);
        }

        // Vytvoří objednávku pro daný email, způsob platby a poznámku; kontroluje víkend a zůstatek na účtu.
        public void CreateOrder(string email, PaymentMethod method, string? note)
        {
            var dateVal = SelectedDate ?? DateTime.Today;
            if (dateVal.DayOfWeek == DayOfWeek.Saturday || dateVal.DayOfWeek == DayOfWeek.Sunday)
                throw new InvalidOperationException("Objednávku nelze vytvořit na víkend (sobota/neděle). Zvolte pracovní den.");

            using var ctx = new AppDbContext();
            var stravnik = ctx.Stravnik.AsNoTracking().FirstOrDefault(s => s.Email == email);
            if (stravnik == null)
                throw new InvalidOperationException("Uživatel nenalezen");

            // open connection once (used for balance check + whole transaction)
            var dbConn = ctx.Database.GetDbConnection();
            var wasClosed = dbConn.State == System.Data.ConnectionState.Closed;
            if (wasClosed) dbConn.Open();

            var conn = dbConn as OracleConnection;
            if (conn == null)
                throw new InvalidOperationException("OracleConnection není dostupné");

            // Validate balance using DB function F_ZUSTATEK for Account method
            if (method == PaymentMethod.Account)
            {
                using var cmdCheck = new OracleCommand("SELECT F_ZUSTATEK(:p_sid, :p_amt) FROM dual", conn)
                {
                    CommandType = System.Data.CommandType.Text
                };
                cmdCheck.Parameters.Add(":p_sid", OracleDbType.Int32).Value = stravnik.IdStravnik;
                cmdCheck.Parameters.Add(":p_amt", OracleDbType.Double).Value = Total;
                var res = cmdCheck.ExecuteScalar()?.ToString() ?? string.Empty;
                if (!string.Equals(res, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    if (wasClosed) dbConn.Close();
                    throw new InvalidOperationException(res == "NEDOSTATECNY" ? "Nedostatečný zůstatek na účtu." : "Strávník neexistuje.");
                }
            }

            using var tx = conn.BeginTransaction();
            try
            {
                // Vloží objednávku a položky ručně (ID stavu podle platby), vypočte cenu.
                var stavId = (method == PaymentMethod.Cash) ? 4 : 1;
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

        // Způsoby platby podporované při vytváření objednávky.
        public enum PaymentMethod
        {
            Card,
            Account,
            Cash
        }
    }
}
