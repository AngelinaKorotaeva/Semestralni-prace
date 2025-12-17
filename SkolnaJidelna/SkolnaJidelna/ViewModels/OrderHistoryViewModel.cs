using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    public class OrderHistoryViewModel : BaseViewModel
    {
        private readonly string _email;
        private int _idStravnik;
        private string _selectedStav = "V?e";
        private ObservableCollection<OrderSummary> _orders = new();

        private OrderSummary? _selectedOrder;
        private string _orderDateText = "Datum: -";
        private string _orderStatusText = "Stav: -";
        private string _orderTotalText = "Celkov? cena: 0 K?";
        private string _orderNoteText = "Pozn?mka: -";
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
                OrderTotalText = "Celkov? cena: 0 K?";
                OrderNoteText = "Pozn?mka: -";
                SelectedOrderItems = new ObservableCollection<OrderItemDetail>();
                IsSelectedOrderUnpaid = false;
                return;
            }

            var o = SelectedOrder;
            OrderDateText = $"Datum odb?ru: {o.DatumOdberu:dd.MM.yyyy} | Vytvo?eno: {(o.DatumVytvoreni.HasValue ? o.DatumVytvoreni.Value.ToString("dd.MM.yyyy HH:mm") : "-" )}";
            OrderStatusText = $"Stav: {o.StavNazev}";
            OrderTotalText = $"Celkov? cena: {o.CelkovaCena:0.##} K?";
            OrderNoteText = $"Pozn?mka: {(string.IsNullOrWhiteSpace(o.Poznamka) ? "-" : o.Poznamka)}";
            SelectedOrderItems = o.Items;
            var statusUp = (o.StavNazev ?? string.Empty).ToUpperInvariant();
            IsSelectedOrderUnpaid = statusUp.StartsWith("NEZAPLAC");
        }

        public void LoadOrders()
        {
            var list = new ObservableCollection<OrderSummary>();
            using var ctx = new AppDbContext();
            var dbConn = ctx.Database.GetDbConnection();
            var wasClosed = dbConn.State == ConnectionState.Closed;
            if (wasClosed) dbConn.Open();
            var conn = (OracleConnection)dbConn;

            try
            {
                string sql = @"SELECT id_objednavka, datum_odberu, datum_vytvoreni, stav, jidlo, mnozstvi, cena_polozky, cena_polozky_celkem, celkova_cena 
                                FROM v_objednavky_historie_detail 
                                WHERE id_stravnik = :sid";

                // Build tolerant filter by status text
                var stav = (SelectedStav ?? "").Trim();
                string? like = null;
                if (!stav.Equals("V?e", System.StringComparison.OrdinalIgnoreCase))
                {
                    var up = stav.ToUpperInvariant();
                    if (up.StartsWith("V PROCES")) like = "V PROCES%";
                    else if (up.StartsWith("NEZAPLAC")) like = "NEZAPLAC%";
                    else if (up.StartsWith("DOKONC")) like = "DOKONC%";
                    else if (up.StartsWith("ZRU")) like = "ZRU%"; // Zru?en? / Zru?eno
                }
                if (like != null)
                {
                    sql += " AND UPPER(stav) LIKE :stav";
                }
                sql += " ORDER BY id_objednavka DESC, datum_vytvoreni DESC";

                using var cmd = new OracleCommand(sql, conn);
                cmd.BindByName = true;
                cmd.Parameters.Add(":sid", OracleDbType.Int32).Value = _idStravnik;
                if (like != null)
                    cmd.Parameters.Add(":stav", OracleDbType.Varchar2).Value = like;

                using var rdr = cmd.ExecuteReader();
                OrderSummary? current = null;
                int? curId = null;
                while (rdr.Read())
                {
                    var id = rdr.GetInt32(0);
                    var datumOdberu = rdr.GetDateTime(1);
                    DateTime? datumVytvoreni = rdr.IsDBNull(2) ? (DateTime?)null : rdr.GetDateTime(2);
                    var stavNazev = rdr.GetString(3);
                    var jidlo = rdr.GetString(4);
                    var mnozstvi = Convert.ToInt32(rdr.GetDecimal(5));
                    var cenaPolozkyCelkem = Convert.ToDouble(rdr.GetDecimal(7));
                    var celkovaCena = Convert.ToDouble(rdr.GetDecimal(8));

                    if (curId != id)
                    {
                        current = new OrderSummary
                        {
                            IdObjednavka = id,
                            DatumOdberu = datumOdberu,
                            DatumVytvoreni = datumVytvoreni,
                            StavNazev = stavNazev,
                            CelkovaCena = celkovaCena
                        };
                        list.Add(current);
                        curId = id;
                    }

                    current!.Items.Add(new OrderItemDetail
                    {
                        Nazev = jidlo,
                        Mnozstvi = mnozstvi,
                        Cena = Math.Round(cenaPolozkyCelkem, 2)
                    });
                }

                // get notes for orders
                if (list.Count > 0)
                {
                    var ids = string.Join(",", list.Select(o => o.IdObjednavka));
                    using var cmdPoz = new OracleCommand($"SELECT id_objednavka, poznamka FROM objednavky WHERE id_objednavka IN ({ids})", conn);
                    using var rdrPoz = cmdPoz.ExecuteReader();
                    var notes = new System.Collections.Generic.Dictionary<int, string?>();
                    while (rdrPoz.Read())
                    {
                        var oid = rdrPoz.GetInt32(0);
                        string? poz = rdrPoz.IsDBNull(1) ? null : rdrPoz.GetString(1);
                        notes[oid] = poz;
                    }
                    foreach (var o in list)
                    {
                        if (notes.TryGetValue(o.IdObjednavka, out var p)) o.Poznamka = p;
                    }
                }

                Orders = list;
            }
            finally
            {
                if (wasClosed) dbConn.Close();
            }
        }

        public enum PaymentMethod
        {
            Card,
            Account
        }

        public void PayOrder(OrderSummary order, PaymentMethod method)
        {
            using var ctx = new AppDbContext();
            var dbConn = ctx.Database.GetDbConnection();
            var wasClosed = dbConn.State == ConnectionState.Closed;
            if (wasClosed) dbConn.Open();
            var conn = (OracleConnection)dbConn;
            using var tx = conn.BeginTransaction();
            try
            {
                if (method == PaymentMethod.Account)
                {
                    // check and deduct balance
                    using var cmdBal = new OracleCommand("SELECT zustatek FROM stravnici WHERE id_stravnik = :sid FOR UPDATE", conn)
                    { CommandType = CommandType.Text, Transaction = tx };
                    cmdBal.Parameters.Add(":sid", OracleDbType.Int32).Value = _idStravnik;
                    var current = Convert.ToDecimal(cmdBal.ExecuteScalar());
                    var needed = Convert.ToDecimal(order.CelkovaCena);
                    if (current < needed)
                        throw new InvalidOperationException("Nedostate?n? z?statek na ??tu.");
                    using var cmdDed = new OracleCommand("UPDATE stravnici SET zustatek = zustatek - :a WHERE id_stravnik = :sid", conn)
                    { CommandType = CommandType.Text, Transaction = tx };
                    cmdDed.Parameters.Add(":a", OracleDbType.Decimal).Value = needed;
                    cmdDed.Parameters.Add(":sid", OracleDbType.Int32).Value = _idStravnik;
                    cmdDed.ExecuteNonQuery();
                }

                // set status to V procesu (1)
                using var cmdUpd = new OracleCommand("UPDATE objednavky SET id_stav = 1 WHERE id_objednavka = :id", conn)
                { CommandType = CommandType.Text, Transaction = tx };
                cmdUpd.Parameters.Add(":id", OracleDbType.Int32).Value = order.IdObjednavka;
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

        public void CancelOrder(OrderSummary order)
        {
            using var ctx = new AppDbContext();
            var dbConn = ctx.Database.GetDbConnection();
            var wasClosed = dbConn.State == ConnectionState.Closed;
            if (wasClosed) dbConn.Open();
            var conn = (OracleConnection)dbConn;
            using var tx = conn.BeginTransaction();
            try
            {
                // refund if already paid (anything except NEZAPLACENY)
                var statusUp = (order.StavNazev ?? string.Empty).ToUpperInvariant();
                var isUnpaid = statusUp.StartsWith("NEZAPLAC");
                if (!isUnpaid)
                {
                    using var cmdAdd = new OracleCommand("UPDATE stravnici SET zustatek = zustatek + :a WHERE id_stravnik = :sid", conn)
                    { CommandType = CommandType.Text, Transaction = tx };
                    cmdAdd.Parameters.Add(":a", OracleDbType.Decimal).Value = Convert.ToDecimal(order.CelkovaCena);
                    cmdAdd.Parameters.Add(":sid", OracleDbType.Int32).Value = _idStravnik;
                    cmdAdd.ExecuteNonQuery();
                }

                // set status to canceled (id from stavy by name)
                using var cmdCancel = new OracleCommand("UPDATE objednavky SET id_stav = (SELECT id_stav FROM stavy WHERE UPPER(nazev) LIKE 'ZRU%') WHERE id_objednavka = :id", conn)
                { CommandType = CommandType.Text, Transaction = tx };
                cmdCancel.Parameters.Add(":id", OracleDbType.Int32).Value = order.IdObjednavka;
                cmdCancel.ExecuteNonQuery();

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
