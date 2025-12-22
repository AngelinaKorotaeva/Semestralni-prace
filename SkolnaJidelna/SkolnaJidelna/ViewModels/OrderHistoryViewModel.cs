using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
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

        public string SelectedStav
        {
            get => _selectedStav;
            set { _selectedStav = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<OrderSummary> Orders
        {
            get => _orders;
            set { _orders = value; RaisePropertyChanged(); }
        }

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

        public string OrderDateText { get => _orderDateText; private set { _orderDateText = value; RaisePropertyChanged(); } }
        public string OrderStatusText { get => _orderStatusText; private set { _orderStatusText = value; RaisePropertyChanged(); } }
        public string OrderTotalText { get => _orderTotalText; private set { _orderTotalText = value; RaisePropertyChanged(); } }
        public string OrderNoteText { get => _orderNoteText; private set { _orderNoteText = value; RaisePropertyChanged(); } }
        public ObservableCollection<OrderItemDetail> SelectedOrderItems { get => _selectedOrderItems; private set { _selectedOrderItems = value; RaisePropertyChanged(); } }
        public bool IsSelectedOrderUnpaid { get => _isSelectedOrderUnpaid; private set { _isSelectedOrderUnpaid = value; RaisePropertyChanged(); } }

        public OrderHistoryViewModel(string email)
        {
            _email = email;
            using var ctx = new AppDbContext();
            _idStravnik = ctx.Stravnik.AsNoTracking().Where(s => s.Email == _email).Select(s => s.IdStravnik).FirstOrDefault();
        }

        public class OrderItemDetail
        {
            public string Nazev { get; set; } = string.Empty;
            public int Mnozstvi { get; set; }
            public double Cena { get; set; } // celkem for the item
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

        public void SetFilter(string value)
        {
            SelectedStav = value;
            LoadOrders();
        }

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
        }

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

            foreach (var g in rows.GroupBy(r => r.IdObjednavka))
            {
                var first = g.First();
                var summary = new OrderSummary
                {
                    IdObjednavka = first.IdObjednavka,
                    DatumOdberu = first.DatumOdberu,
                    DatumVytvoreni = first.DatumVytvoreni,
                    StavNazev = first.Stav,
                    CelkovaCena = first.CelkovaCena,
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

            // notes
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

        public enum PaymentMethod
        {
            Card,
            Account
        }

        public void PayOrder(OrderSummary order, PaymentMethod method)
        {
            using var ctx = new AppDbContext();
            using var tx = ctx.Database.BeginTransaction();
            try
            {
                if (method == PaymentMethod.Account)
                {
                    var str = ctx.Stravnik.Single(s => s.IdStravnik == _idStravnik);
                    var needed = order.CelkovaCena;
                    if (str.Zustatek < needed)
                        throw new InvalidOperationException("Nedostatečný zůstatek na účtu.");
                    str.Zustatek -= needed;
                    ctx.SaveChanges();
                }

                var obj = ctx.Objednavka.Single(o => o.IdObjednavka == order.IdObjednavka);
                obj.IdStav = 1; // původní chování: nastavit na ID=1
                ctx.SaveChanges();

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

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
                if (canceledId == 0) canceledId = obj.IdStav; // fallback, aby se nezměnilo na neplatnou hodnotu
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
