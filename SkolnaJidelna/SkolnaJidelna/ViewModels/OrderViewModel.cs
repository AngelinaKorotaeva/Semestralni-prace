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
    /// <summary>
    /// ViewModel pro práci s objednávkami ve WPF aplikaci školní jídelny.
    /// Zajišťuje načtení menu a jídel, zvýraznění alergií/omezení dle uživatele,
    /// správu košíku, výpočet celkové ceny a vytvoření objednávky v databázi (Oracle).
    /// Podporuje platbu kartou, hotově a z účtu strávníka včetně kontroly a ručního odečtení zůstatku.
    /// </summary>
    public class OrderViewModel : BaseViewModel
    {
        // Aktuálně vybraný typ menu ("Vše" = bez filtru)
        private string _selectedTypMenu = "Vše";
        // Kolekce dostupných menu včetně jídel
        private ObservableCollection<MenuWithJidla> _menus = new();
        // Položky košíku (vybraná jídla a množství)
        private ObservableCollection<CartItem> _cartItems = new();
        // Celková cena košíku
        private double _total;
        // Zvolený datum pro objednávku
        private DateTime? _selectedDate = DateTime.Today;
        // Normalizované názvy produktů, na které má uživatel alergii
        private HashSet<string> _userAllergicProducts = new(StringComparer.OrdinalIgnoreCase);
        // Normalizované klíčové výrazy dietních omezení uživatele
        private HashSet<string> _userDietKeywords = new(StringComparer.OrdinalIgnoreCase);

        // Vlastnosti pro binding do UI
        public string SelectedTypMenu { get => _selectedTypMenu; set { _selectedTypMenu = value; RaisePropertyChanged(); } }
        public ObservableCollection<MenuWithJidla> Menus { get => _menus; set { _menus = value; RaisePropertyChanged(); } }
        public ObservableCollection<CartItem> CartItems { get => _cartItems; set { _cartItems = value; UpdateTotal(); RaisePropertyChanged(); } }
        public DateTime? SelectedDate { get => _selectedDate; set { _selectedDate = value; RaisePropertyChanged(); } }
        public double Total { get => _total; private set { _total = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TotalFormatted)); } }
        public string TotalFormatted => $"Celkem: {Total:0.##} Kč";

        /// <summary>
        /// Datový model jedné položky jídla z menu, včetně příznaků alergie/dietního konfliktu pro aktuálního uživatele.
        /// </summary>
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

            // Příznak, zda jídlo obsahuje alergenní složky pro uživatele
            public bool IsAllergic { get => _isAllergic; set { _isAllergic = value; RaisePropertyChanged(); } }
            // Seznam nalezených alergenních složek (text)
            public string? MatchedAllergens { get => _matchedAllergens; set { _matchedAllergens = value; RaisePropertyChanged(); } }
            // Příznak, zda jídlo koliduje s dietními omezeními uživatele
            public bool IsDietConflict { get => _isDietConflict; set { _isDietConflict = value; RaisePropertyChanged(); } }
            // Seznam složek způsobujících dietní konflikt (text)
            public string? MatchedDietKeywords { get => _matchedDietKeywords; set { _matchedDietKeywords = value; RaisePropertyChanged(); } }
        }

        /// <summary>
        /// Model menu s kolekcí jídel.
        /// </summary>
        public class MenuWithJidla
        {
            public int IdMenu { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public string TypMenu { get; set; } = string.Empty;
            public ObservableCollection<JidloItem> Jidla { get; set; } = new();
        }

        /// <summary>
        /// Položka košíku – vybrané jídlo, jednotková cena, množství a vypočtená cena celkem.
        /// </summary>
        public class CartItem : BaseViewModel
        {
            private int _mnozstvi;
            private double _cenaJednotkova;
            public int IdJidlo { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public double CenaJednotkova { get => _cenaJednotkova; set { _cenaJednotkova = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(CenaCelkem)); } }
            public int Mnozstvi { get => _mnozstvi; set { _mnozstvi = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(CenaCelkem)); } }
            // Cena celkem = jednotková cena * množství
            public double CenaCelkem => Math.Round(CenaJednotkova * Mnozstvi, 2);
        }

        // Načte menu bez filtru
        public void LoadMenus() { LoadMenus(null); }

        /// <summary>
        /// Načte menu a jídla. Pokud je zadán e‑mail, načtou se alergie a dietní omezení uživatele
        /// a položky se podle nich vizuálně označí.
        /// </summary>
        public void LoadMenus(string? email)
        {
            try
            {
                using var ctx = new AppDbContext();
                // Vyčisti cache alergií a diet
                _userAllergicProducts.Clear();
                _userDietKeywords.Clear();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        // Najdi strávníka dle e‑mailu a načti jeho alergie/diety
                        var stravnik = ctx.Stravnik.AsNoTracking().FirstOrDefault(s => s.Email == email);
                        if (stravnik != null)
                        {
                            // Alergie -> seznam produktů, které budeme porovnávat se složkami jídel
                            var products = ctx.StravnikAlergie.AsNoTracking()
                                .Where(sa => sa.IdStravnik == stravnik.IdStravnik)
                                .Join(ctx.Alergie, sa => sa.IdAlergie, a => a.IdAlergie, (sa, a) => a.Produkt)
                                .Where(p => p != null)
                                .Select(p => p!)
                                .ToList();
                            foreach (var p in products) _userAllergicProducts.Add(Normalize(p));

                            // Dietní omezení -> převedeme na množinu klíčových slov
                            var diets = ctx.StravnikOmezeni.AsNoTracking()
                                .Where(so => so.IdStravnik == stravnik.IdStravnik)
                                .Join(ctx.DietniOmezeni, so => so.IdOmezeni, d => d.IdOmezeni, (so, d) => d.Nazev)
                                .Where(n => n != null)
                                .Select(n => n!)
                                .ToList();
                            var keywords = BuildDietKeywordsSet(diets);
                            foreach (var k in keywords) _userDietKeywords.Add(k);
                        }
                    }
                    catch { }
                }

                // Aplikuj filtr typu menu (pokud není "Vše")
                var menusQuery = ctx.Menu.AsNoTracking();
                var selected = (SelectedTypMenu ?? "").Trim();
                if (!string.Equals(selected, "Vše", StringComparison.OrdinalIgnoreCase))
                {
                    var typ = selected.ToUpperInvariant();
                    menusQuery = menusQuery.Where(m => m.TypMenu != null && m.TypMenu.ToUpper() == typ);
                }

                // Zmaterializuj menu a k nim dobij jídla z pohledu V_JIDLA_MENU
                var rawMenus = menusQuery.OrderBy(m => m.IdMenu).Select(m => new { m.IdMenu, m.Nazev, m.TypMenu }).ToList();
                var list = new List<MenuWithJidla>();
                foreach (var m in rawMenus)
                {
                    var menuNode = new MenuWithJidla { IdMenu = m.IdMenu, Nazev = m.Nazev, TypMenu = m.TypMenu ?? string.Empty, Jidla = new ObservableCollection<JidloItem>() };
                    var foods = ctx.VJidlaMenu.AsNoTracking().Where(v => v.MenuNazev == m.Nazev).OrderBy(v => v.Jidlo)
                        .Select(v => new JidloItem { IdJidlo = v.IdJidlo, Nazev = v.Jidlo, Popis = v.Popis, Cena = v.Cena, Poznamka = null }).ToList();
                    foreach (var f in foods) menuNode.Jidla.Add(f);
                    list.Add(menuNode);
                }

                // Pokud máme alergie/diety, označ položky, které kolidují
                if (_userAllergicProducts.Count > 0 || _userDietKeywords.Count > 0)
                {
                    foreach (var menu in list)
                    foreach (var item in menu.Jidla)
                    {
                        try
                        {
                            // Načti složky jídla a porovnej s alergiemi/dietami (normalizovaně)
                            var slozkyRaw = ctx.SlozkaJidlo.AsNoTracking()
                                .Where(sj => sj.IdJidlo == item.IdJidlo)
                                .Join(ctx.Slozka, sj => sj.IdSlozka, s => s.IdSlozka, (sj, s) => s.Nazev).ToList();
                            var matchedAllergens = new List<string>();
                            foreach (var ing in slozkyRaw)
                            {
                                var n = Normalize(ing);
                                if (_userAllergicProducts.Contains(n) || _userAllergicProducts.Any(p => n.Contains(p) || p.Contains(n))) matchedAllergens.Add(ing);
                            }
                            item.IsAllergic = matchedAllergens.Count > 0;
                            item.MatchedAllergens = item.IsAllergic ? string.Join(", ", matchedAllergens.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
                            if (item.IsAllergic) item.Poznamka = string.IsNullOrWhiteSpace(item.Poznamka) ? "[ALERGIE]" : item.Poznamka + " [ALERGIE]";

                            var matchedDiet = new List<string>();
                            foreach (var ing in slozkyRaw)
                            {
                                var n = Normalize(ing);
                                if (_userDietKeywords.Any(k => n.Contains(k) || k.Contains(n))) matchedDiet.Add(ing);
                            }
                            item.IsDietConflict = matchedDiet.Count > 0;
                            item.MatchedDietKeywords = item.IsDietConflict ? string.Join(", ", matchedDiet.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
                        }
                        catch
                        {
                            // Robustně: při chybě neoznačuj nic
                            item.IsAllergic = false; item.MatchedAllergens = null; item.IsDietConflict = false; item.MatchedDietKeywords = null;
                        }
                    }
                }

                Menus = new ObservableCollection<MenuWithJidla>(list);
            }
            catch (Exception ex)
            {
                // Chybové hlášení při selhání načtení
                MessageBox.Show("Chyba pri nacitani menu: " + ex.Message);
                Menus = new ObservableCollection<MenuWithJidla>();
            }
        }

        /// <summary>
        /// Převod názvů dietních omezení na normalizovanou množinu klíčových slov pro porovnání se složkami jídel.
        /// </summary>
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
                if (map.TryGetValue(dn.Trim(), out var kws)) foreach (var k in kws) set.Add(Normalize(k));
            }
            return set;
        }

        /// <summary>
        /// Normalizace textu na malé znaky bez diakritiky pro robustní porovnávání (ponechává písmena/čísla/mezery).
        /// </summary>
        private static string Normalize(string s)
        {
            var t = (s ?? string.Empty).Trim().ToLowerInvariant();
            t = t.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(t.Length);
            foreach (var c in t)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark && (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))) sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Přidá jídlo do košíku (zvýší množství, pokud už existuje) a přepočítá celkovou cenu.
        /// </summary>
        public void AddToOrder(JidloItem item)
        {
            if (item == null) return;
            var existing = CartItems.FirstOrDefault(ci => ci.IdJidlo == item.IdJidlo);
            if (existing == null)
                CartItems.Add(new CartItem { IdJidlo = item.IdJidlo, Nazev = item.Nazev, CenaJednotkova = item.Cena, Mnozstvi = 1 });
            else existing.Mnozstvi += 1;
            UpdateTotal();
        }

        /// <summary>
        /// Odebere jednu jednotku z košíku (nebo celou položku, pokud množství klesne na 0) a přepočítá cenu.
        /// </summary>
        public void RemoveOneFromOrder(int idJidlo)
        {
            var existing = CartItems.FirstOrDefault(ci => ci.IdJidlo == idJidlo);
            if (existing == null) return;
            if (existing.Mnozstvi > 1) existing.Mnozstvi -= 1; else CartItems.Remove(existing);
            UpdateTotal();
        }

        // Přepočet celkové ceny košíku
        private void UpdateTotal() { Total = CartItems.Sum(ci => ci.CenaCelkem); }

        /// <summary>
        /// Vytvoří objednávku v DB pro daný e‑mail, způsob platby a poznámku.
        /// Kontroluje víkend a budoucí datum, ověřuje zůstatek pro platbu z účtu přes F_ZUSTATECNY,
        /// vloží objednávku (procedura), položky a aktualizuje cenu. Při platbě z účtu provede ruční odečet zůstatku.
        /// Celé proběhne v jedné transakci.
        /// </summary>
        public void CreateOrder(string email, PaymentMethod method, string? note)
        {
            var dateVal = SelectedDate ?? DateTime.Today;
            if (dateVal.DayOfWeek == DayOfWeek.Saturday || dateVal.DayOfWeek == DayOfWeek.Sunday)
                throw new InvalidOperationException("Objednávku nelze vytvořit na víkend (sobota/neděle). Zvolte pracovní den.");
            if (dateVal.Date <= DateTime.Today)
                throw new InvalidOperationException("Objednávku lze vytvářet pouze na budoucí pracovní den.");

            using var ctx = new AppDbContext();
            var stravnik = ctx.Stravnik.AsNoTracking().FirstOrDefault(s => s.Email == email);
            if (stravnik == null) throw new InvalidOperationException("Uživatel nenalezen");

            var dbConn = ctx.Database.GetDbConnection();
            var wasClosed = dbConn.State == System.Data.ConnectionState.Closed;
            if (wasClosed) dbConn.Open();
            var conn = dbConn as OracleConnection; if (conn == null) throw new InvalidOperationException("OracleConnection není dostupné");

            // 1) Kontrola zůstatku pro platbu z účtu (bez odečtu)
            if (method == PaymentMethod.Account)
            {
                using var cmdCheck = new OracleCommand("SELECT F_ZUSTATEK(:p_sid, :p_amt) FROM dual", conn) { CommandType = System.Data.CommandType.Text };
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
                if (CartItems.Count == 0) throw new InvalidOperationException("Objednávka musí obsahovat alespoň jednu položku.");
                var stavId = (method == PaymentMethod.Cash) ? 4 : 1;
                var noteVal = string.IsNullOrWhiteSpace(note) ? null : note;

                // 2) Vložení objednávky pomocí procedury P_INSERT_OBJ
                int objednavkaId;
                using (var cmdIns = new OracleCommand("P_INSERT_OBJ", conn) { CommandType = System.Data.CommandType.StoredProcedure, Transaction = tx })
                {
                    cmdIns.BindByName = true;
                    cmdIns.Parameters.Add("P_ID_STRAVNIK", OracleDbType.Int32).Value = stravnik.IdStravnik;
                    cmdIns.Parameters.Add("P_CELKOVA_CENA", OracleDbType.Decimal).Value = 0m;
                    cmdIns.Parameters.Add("P_DATUM", OracleDbType.Date).Value = dateVal;
                    cmdIns.Parameters.Add("P_POZNAMKA", OracleDbType.Varchar2).Value = (object?)noteVal ?? DBNull.Value;
                    var outId = new OracleParameter("P_ID_OBJEDNAVKA", OracleDbType.Int32) { Direction = System.Data.ParameterDirection.Output };
                    cmdIns.Parameters.Add(outId);
                    cmdIns.Parameters.Add("P_ID_STAV", OracleDbType.Int32).Value = stavId;
                    cmdIns.ExecuteNonQuery();
                    objednavkaId = Convert.ToInt32(outId.Value.ToString());
                }

                // 3) Vložení položek objednávky přes proceduru P_INSERT_POLOZKA
                foreach (var ci in CartItems)
                {
                    using var cmdItem = new OracleCommand("P_INSERT_POLOZKA", conn) { CommandType = System.Data.CommandType.StoredProcedure, Transaction = tx };
                    cmdItem.BindByName = true;
                    cmdItem.Parameters.Add("P_ID_OBJEDNAVKA", OracleDbType.Int32).Value = objednavkaId;
                    cmdItem.Parameters.Add("P_ID_JIDLO", OracleDbType.Int32).Value = ci.IdJidlo;
                    cmdItem.Parameters.Add("P_MNOZSTVI", OracleDbType.Int32).Value = ci.Mnozstvi;
                    cmdItem.ExecuteNonQuery();
                }

                // 4) Aktualizace celkové ceny objednávky
                using (var cmdUpd = new OracleCommand("UPDATE objednavky SET celkova_cena = :c WHERE id_objednavka = :id", conn) { CommandType = System.Data.CommandType.Text, Transaction = tx })
                { cmdUpd.Parameters.Add(":c", OracleDbType.Decimal).Value = Convert.ToDecimal(Total); cmdUpd.Parameters.Add(":id", OracleDbType.Int32).Value = objednavkaId; cmdUpd.ExecuteNonQuery(); }

                // 5) Ruční odečet zůstatku strávníka (pouze u platby z účtu)
                if (method == PaymentMethod.Account)
                {
                    using var cmdDebit = new OracleCommand("UPDATE stravnici SET zustatek = zustatek - :amt WHERE id_stravnik = :sid", conn)
                    { CommandType = System.Data.CommandType.Text, Transaction = tx };
                    cmdDebit.Parameters.Add(":amt", OracleDbType.Decimal).Value = Convert.ToDecimal(Total);
                    cmdDebit.Parameters.Add(":sid", OracleDbType.Int32).Value = stravnik.IdStravnik;
                    var rows = cmdDebit.ExecuteNonQuery();
                    if (rows != 1)
                        throw new InvalidOperationException("Nepodařilo se odečíst částku z účtu strávníka.");
                }

                tx.Commit();
            }
            catch { try { tx.Rollback(); } catch { } throw; }
            finally { if (wasClosed) dbConn.Close(); }
        }

        // Způsoby platby
        public enum PaymentMethod { Card, Account, Cash }
    }
}
