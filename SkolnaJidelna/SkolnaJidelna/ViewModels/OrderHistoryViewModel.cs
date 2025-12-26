using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    // ViewModel pro historii objednávek konkrétního strávníka.
    // Načítá seznam objednávek, detail vybrané objednávky, stavové přehledy a umožňuje platbu/zrušení.
    public class OrderHistoryViewModel : BaseViewModel
    {
        private readonly string _email;
        private int _idStravnik;
        private string _selectedStav = "Vše";
        private ObservableCollection<OrderSummary> _orders = new();

        private OrderSummary? _selectedOrder;
        private string _orderDateText = "Datum: -";
        private string _orderStatusText = "Stav: -";
        private string _orderTotalText = "Celková cena: 0 Kč";
        private string _orderNoteText = "Poznámka: -";
        private ObservableCollection<OrderItemDetail> _selectedOrderItems = new();
        private bool _isSelectedOrderUnpaid;
        private string _ordersStatusOverview = string.Empty;
        private bool _isCancelVisible;

        // Filtr podle stavu ("Vše", "V procesu", "Nezaplacený", "Dokončeno", "Zrušený")
        public string SelectedStav
        {
            get => _selectedStav;
            set { _selectedStav = value; RaisePropertyChanged(); }
        }

        // Kolekce objednávek pro UI
        public ObservableCollection<OrderSummary> Orders
        {
            get => _orders;
            set { _orders = value; RaisePropertyChanged(); }
        }

        // Aktuálně vybraná objednávka – přepočítá texty a viditelnosti tlačítek
        public OrderSummary? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                UpdateSelectedOrderComputed();
                RaisePropertyChanged();
            }
        }

        // Texty pro panel detailu
        public string OrderDateText { get => _orderDateText; private set { _orderDateText = value; RaisePropertyChanged(); } }
        public string OrderStatusText { get => _orderStatusText; private set { _orderStatusText = value; RaisePropertyChanged(); } }
        public string OrderTotalText { get => _orderTotalText; private set { _orderTotalText = value; RaisePropertyChanged(); } }
        public string OrderNoteText { get => _orderNoteText; private set { _orderNoteText = value; RaisePropertyChanged(); } }
        public ObservableCollection<OrderItemDetail> SelectedOrderItems { get => _selectedOrderItems; private set { _selectedOrderItems = value; RaisePropertyChanged(); } }
        public bool IsSelectedOrderUnpaid { get => _isSelectedOrderUnpaid; private set { _isSelectedOrderUnpaid = value; RaisePropertyChanged(); } }
        public string OrdersStatusOverview { get => _ordersStatusOverview; private set { _ordersStatusOverview = value; RaisePropertyChanged(); } }
        public bool IsCancelVisible { get => _isCancelVisible; private set { _isCancelVisible = value; RaisePropertyChanged(); } }

        // Konstruktor – zjistí Id strávníka podle e‑mailu
        public OrderHistoryViewModel(string email)
        {
            _email = email;
            using var ctx = new AppDbContext();
            _idStravnik = ctx.Stravnik.AsNoTracking().Where(s => s.Email == _email).Select(s => s.IdStravnik).FirstOrDefault();
        }

        // DTO pro položky a souhrn objednávky
        public class OrderItemDetail
        {
            public string Nazev { get; set; } = string.Empty;
            public int Mnozstvi { get; set; }
            public double Cena { get; set; } // celkem za položku
        }

        public class OrderSummary
        {
            public int IdObjednavka { get; set; }
            public DateTime DatumOdberu { get; set; }
            public DateTime? DatumVytvoreni { get; set; }
            public string StavNazev { get; set; } = string.Empty;
            public double CelkovaCena { get; set; }
            public string? Poznamka { get; set; }
            public ObservableCollection<OrderItemDetail> Items { get; set; } = new();
        }

        // Nastaví filtr a znovu načte objednávky
        public void SetFilter(string value)
        {
            SelectedStav = value;
            LoadOrders();
        }

        // Přepočítá odvozené texty a viditelnosti pro vybranou objednávku
        private void UpdateSelectedOrderComputed()
        {
            if (SelectedOrder == null)
            {
                OrderDateText = "Datum: -";
                OrderStatusText = "Stav: -";
                OrderTotalText = "Celková cena: 0 Kč";
                OrderNoteText = "Poznámka: -";
                SelectedOrderItems = new ObservableCollection<OrderItemDetail>();
                IsSelectedOrderUnpaid = false;
                IsCancelVisible = false;
                return;
            }

            var o = SelectedOrder;
            var created = o.DatumVytvoreni.HasValue ? o.DatumVytvoreni.Value.ToString("dd.MM.yyyy HH:mm") : "-";
            OrderDateText = $"Datum odběru: {o.DatumOdberu:dd.MM.yyyy} | Vytvořeno: {created}";
            OrderStatusText = $"Stav: {o.StavNazev}";
            OrderTotalText = $"Celková cena: {o.CelkovaCena:0.##} Kč";
            OrderNoteText = $"Poznámka: {(string.IsNullOrWhiteSpace(o.Poznamka) ? "-" : o.Poznamka)}";
            SelectedOrderItems = o.Items;
            var statusUp = (o.StavNazev ?? string.Empty).ToUpperInvariant();
            IsSelectedOrderUnpaid = statusUp.StartsWith("NEZAPLAC");
            // Skrýt tlačítko zrušení, pokud je objednávka už zrušená
            IsCancelVisible = !statusUp.StartsWith("ZRU");
        }

        // Načte objednávky z pohledu, aplikuje filtr a doplní přehled přes F_OBJEDNAVKA_STAV a ceny přes F_CELKOVA_CENA
        public void LoadOrders()
        {
            var list = new System.Collections.Generic.List<OrderSummary>();
            using var ctx = new AppDbContext();

            var q = ctx.VObjHistorieDetail.AsNoTracking()
                .Where(r => r.IdStravnik == _idStravnik);

            var stav = (SelectedStav ?? "").Trim();
            string? like = null;
            if (!stav.Equals("Vše", System.StringComparison.OrdinalIgnoreCase))
            {
                var up = stav.ToUpperInvariant();
                if (up.StartsWith("V PROCES")) like = "V PROCES%";
                else if (up.StartsWith("NEZAPLAC")) like = "NEZAPLAC%";
                else if (up.StartsWith("DOKONC")) like = "DOKONC%";
                else if (up.StartsWith("ZRU")) like = "ZRU%"; // Zrušený / Zrušeno
            }
            if (like != null)
            {
                q = q.Where(r => EF.Functions.Like(r.Stav.ToUpper(), like));
            }

            var rows = q
                .OrderByDescending(r => r.IdObjednavka)
                .ThenByDescending(r => r.DatumVytvoreni)
                .ToList();

            // Připraví jedno spojení a zavolá DB funkce pro přehled a ceny
            var dbConn = ctx.Database.GetDbConnection();
            var needClose = dbConn.State != ConnectionState.Open;
            if (needClose) dbConn.Open();
            try
            {
                // Přehled stavů objednávek pro strávníka
                using (var cmdOv = dbConn.CreateCommand())
                {
                    cmdOv.CommandType = CommandType.Text;
                    cmdOv.CommandText = "SELECT F_OBJEDNAVKA_STAV(:p_sid) FROM dual";
                    var pSid = cmdOv.CreateParameter();
                    pSid.ParameterName = ":p_sid";
                    pSid.Value = _idStravnik;
                    pSid.DbType = DbType.Int32;
                    cmdOv.Parameters.Add(pSid);
                    var ov = cmdOv.ExecuteScalar();
                    OrdersStatusOverview = ov == null || ov == DBNull.Value ? string.Empty : ov.ToString() ?? string.Empty;
                }

                foreach (var g in rows.GroupBy(r => r.IdObjednavka))
                {
                    var first = g.First();

                    // Celková cena přes F_CELKOVA_CENA
                    double celkovaCena = 0;
                    using (var cmd = dbConn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT F_CELKOVA_CENA(:p_id) FROM dual";
                        var p = cmd.CreateParameter();
                        p.ParameterName = ":p_id";
                        p.Value = first.IdObjednavka;
                        p.DbType = DbType.Int32;
                        cmd.Parameters.Add(p);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value)
                        {
                            celkovaCena = Convert.ToDouble(obj);
                        }
                    }

                    var summary = new OrderSummary
                    {
                        IdObjednavka = first.IdObjednavka,
                        DatumOdberu = first.DatumOdberu,
                        DatumVytvoreni = first.DatumVytvoreni,
                        StavNazev = first.Stav,
                        CelkovaCena = Math.Round(celkovaCena, 2),
                        Items = new ObservableCollection<OrderItemDetail>()
                    };

                    foreach (var it in g)
                    {
                        summary.Items.Add(new OrderItemDetail
                        {
                            Nazev = it.Jidlo,
                            Mnozstvi = it.Mnozstvi,
                            Cena = Math.Round(it.CenaPolozkyCelkem, 2)
                        });
                    }

                    list.Add(summary);
                }

                // Poznámky k objednávkám
                if (list.Count > 0)
                {
                    var ids = list.Select(o => o.IdObjednavka).ToList();
                    var notes = ctx.Objednavka.AsNoTracking()
                        .Where(o => ids.Contains(o.IdObjednavka))
                        .Select(o => new { o.IdObjednavka, o.Poznamka })
                        .ToList()
                        .ToDictionary(x => x.IdObjednavka, x => x.Poznamka);

                    foreach (var o in list)
                    {
                        if (notes.TryGetValue(o.IdObjednavka, out var p)) o.Poznamka = p;
                    }
                }

                Orders = new ObservableCollection<OrderSummary>(list);
            }
            finally
            {
                if (needClose) dbConn.Close();
            }
        }

        // Způsoby platby
        public enum PaymentMethod
        {
            Card,
            Account
        }

        // Uhradí objednávku – u účtu ověří zůstatek F_ZUSTATEK a aktualizuje zůstatek i stav
        public void PayOrder(OrderSummary order, PaymentMethod method)
        {
            using var ctx = new AppDbContext();
            using var tx = ctx.Database.BeginTransaction();
            try
            {
                if (method == PaymentMethod.Account)
                {
                    // Ověření zůstatku přes DB funkci F_ZUSTATEK
                    var dbConn = ctx.Database.GetDbConnection();
                    var needClose = dbConn.State != ConnectionState.Open;
                    if (needClose) dbConn.Open();
                    try
                    {
                        using var cmd = dbConn.CreateCommand();
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT F_ZUSTATEK(:p_sid, :p_amt) FROM dual";
                        var pSid = cmd.CreateParameter();
                        pSid.ParameterName = ":p_sid";
                        pSid.Value = _idStravnik;
                        pSid.DbType = DbType.Int32;
                        cmd.Parameters.Add(pSid);
                        var pAmt = cmd.CreateParameter();
                        pAmt.ParameterName = ":p_amt";
                        pAmt.Value = order.CelkovaCena;
                        pAmt.DbType = DbType.Double;
                        cmd.Parameters.Add(pAmt);
                        var res = cmd.ExecuteScalar()?.ToString() ?? string.Empty;
                        if (!string.Equals(res, "OK", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(res == "NEDOSTATECNY" ? "Nedostatečný zůstatek na účtu." : "Strávník neexistuje.");
                        }
                    }
                    finally
                    {
                        if (needClose) dbConn.Close();
                    }

                    var str = ctx.Stravnik.Single(s => s.IdStravnik == _idStravnik);
                    str.Zustatek -= order.CelkovaCena;
                    ctx.SaveChanges();
                }

                var obj = ctx.Objednavka.Single(o => o.IdObjednavka == order.IdObjednavka);
                obj.IdStav = 1; // Dokončeno
                ctx.SaveChanges();

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        // Zruší objednávku – vrátí peníze pokud byla zaplacena, nastaví stav "Zrušený"
        public void CancelOrder(OrderSummary order)
        {
            using var ctx = new AppDbContext();
            using var tx = ctx.Database.BeginTransaction();
            try
            {
                var statusUp = (order.StavNazev ?? string.Empty).ToUpperInvariant();
                var isUnpaid = statusUp.StartsWith("NEZAPLAC");
                if (!isUnpaid)
                {
                    var str = ctx.Stravnik.Single(s => s.IdStravnik == _idStravnik);
                    str.Zustatek += order.CelkovaCena;
                    ctx.SaveChanges();
                }

                var obj = ctx.Objednavka.Single(o => o.IdObjednavka == order.IdObjednavka);
                var canceledId = ctx.Stav.AsNoTracking()
                    .Where(s => (s.Nazev ?? string.Empty).ToUpper().StartsWith("ZRU"))
                    .Select(s => s.IdStav)
                    .FirstOrDefault();
                if (canceledId == 0) canceledId = obj.IdStav; // Fallback, aby se nenastavila neplatná hodnota
                obj.IdStav = canceledId;
                ctx.SaveChanges();

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        // Uloží PDF výtisk objednávky do tabulky SOUBORY
        public void SavePdfFile(OrderSummary order, string fileName, byte[] content)
        {
            using var ctx = new AppDbContext();
            var soubor = new Soubor
            {
                Nazev = System.IO.Path.GetFileNameWithoutExtension(fileName),
                Typ = "application/pdf",
                Pripona = "pdf",
                Obsah = content,
                DatumNahrani = DateTime.Now,
                DatumModifikace = null,
                Operace = "PRINT",
                Tabulka = "OBJEDNAVKY",
                IdZaznam = order.IdObjednavka,
                IdStravnik = _idStravnik
            };
            ctx.Soubor.Add(soubor);
            ctx.SaveChanges();
        }
    }
}
