using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class TicketController : Controller
    {
        private readonly OracleDbService _db;
        public TicketController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            using var conn = _db.GetConnection();
            AutoCancelExpiredBookings(conn);

            var list = new List<Ticket>();
            using (var cmd = new OracleCommand(
                "SELECT TICKETID, TICKETSTATUS, TICKETBOOKINGTIME, TICKETPURCHASETIME, SEATNO FROM TICKET3 ORDER BY TICKETID", conn))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    list.Add(new Ticket
                    {
                        TicketId = reader.GetInt32(0),
                        TicketStatus = reader.IsDBNull(1) ? null : reader.GetString(1),
                        TicketBookingTime = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                        TicketPurchaseTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                        SeatNo = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });

            ViewBag.CustomerByTicketId = GetCustomerNameByTicketId(conn);
            ViewBag.ShowTextByTicketId = GetShowTextByTicketId(conn);
            return View(list);
        }

        public IActionResult Create()
        {
            using var conn = _db.GetConnection();
            ViewBag.Customers = GetCustomers(conn);
            ViewBag.Movies = GetMovies(conn);
            ViewBag.Shows = GetShowOptions(conn);
            return View();
        }

        [HttpPost]
        public IActionResult Create(Ticket t, int customerId, int showId, int movieId)
        {
            if (!t.TicketBookingTime.HasValue)
                t.TicketBookingTime = DateTime.Now;

            using var conn = _db.GetConnection();

            int hallId = 0, theatreId = 0;
            GetShowDetails(conn, showId, ref movieId, ref hallId, ref theatreId);

            if (customerId == 0 || movieId == 0 || showId == 0)
            {
                TempData["Error"] = "Please select Customer, Movie and Show.";
                return RedirectToAction("Create");
            }

            if (theatreId == 0 || hallId == 0)
            {
                TempData["Error"] = "Could not determine Theatre/Hall for this show. Check show setup.";
                return RedirectToAction("Create");
            }

            int ticketPolicyId = GetTicketPolicyForShow(conn, showId);

            using var trans = conn.BeginTransaction();
            try
            {
                using (var cmd = new OracleCommand(
                    "INSERT INTO TICKET3 (TICKETID, TICKETSTATUS, TICKETBOOKINGTIME, TICKETPURCHASETIME, SEATNO) VALUES (:id, :status, :booktime, :purchtime, :seat)", conn))
                {
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = t.TicketId;
                    cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = (object?)t.TicketStatus ?? "BOOKED";
                    cmd.Parameters.Add("booktime", OracleDbType.Date).Value = t.TicketBookingTime!.Value;
                    cmd.Parameters.Add("purchtime", OracleDbType.Date).Value =
                        t.TicketStatus == "PURCHASED" && t.TicketPurchaseTime.HasValue
                            ? (object)t.TicketPurchaseTime.Value
                            : DBNull.Value;
                    cmd.Parameters.Add("seat", OracleDbType.Varchar2).Value = (object?)t.SeatNo ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new OracleCommand(
                    "INSERT INTO CUSTOMERTICKET3 (CUSTOMERID, TICKETID, MOVIEID, THEATREID, HALLID, SHOWID, TICKETPOLICYID) VALUES (:cid, :tid, :mid, :thid, :hid, :sid, :pid)", conn))
                {
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("cid", OracleDbType.Int32).Value = customerId;
                    cmd.Parameters.Add("tid", OracleDbType.Int32).Value = t.TicketId;
                    cmd.Parameters.Add("mid", OracleDbType.Int32).Value = movieId;
                    cmd.Parameters.Add("thid", OracleDbType.Int32).Value = theatreId;
                    cmd.Parameters.Add("hid", OracleDbType.Int32).Value = hallId;
                    cmd.Parameters.Add("sid", OracleDbType.Int32).Value = showId;
                    cmd.Parameters.Add("pid", OracleDbType.Int32).Value = ticketPolicyId > 0 ? (object)ticketPolicyId : DBNull.Value;
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch (OracleException ex)
            {
                trans.Rollback();
                ModelState.AddModelError("", ex.Number == 1 ? "A ticket with this ID already exists." : "Database error: " + ex.Message);
                ViewBag.Customers = GetCustomers(conn);
                ViewBag.Movies = GetMovies(conn);
                ViewBag.Shows = GetShowOptions(conn);
                return View(t);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();

            using var cmd = new OracleCommand(
                "SELECT TICKETID, TICKETSTATUS, TICKETBOOKINGTIME, TICKETPURCHASETIME, SEATNO FROM TICKET3 WHERE TICKETID=:id", conn);
            cmd.Parameters.Add("id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();

            var ticket = new Ticket
            {
                TicketId = reader.GetInt32(0),
                TicketStatus = reader.IsDBNull(1) ? null : reader.GetString(1),
                TicketBookingTime = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                TicketPurchaseTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                SeatNo = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
            reader.Close();

            using (var ct = new OracleCommand(
                "SELECT CUSTOMERID, MOVIEID, THEATREID, HALLID, SHOWID FROM CUSTOMERTICKET3 WHERE TICKETID=:id", conn))
            {
                ct.Parameters.Add("id", id);
                using var cr = ct.ExecuteReader();
                if (cr.Read())
                {
                    ViewBag.CurrentCustomerId = cr.GetInt32(0);
                    ViewBag.CurrentMovieId = cr.GetInt32(1);
                    ViewBag.CurrentShowId = cr.GetInt32(4);
                }
            }

            ViewBag.Customers = GetCustomers(conn);
            ViewBag.Movies = GetMovies(conn);
            ViewBag.Shows = GetShowOptions(conn);
            return View(ticket);
        }

        [HttpPost]
        public IActionResult Edit(int id, Ticket t, int customerId, int showId, int movieId)
        {
            t.TicketId = id;

            if (customerId == 0 || movieId == 0 || showId == 0)
            {
                TempData["Error"] = "Please select Customer, Movie and Show.";
                return RedirectToAction("Edit", new { id });
            }

            using var conn = _db.GetConnection();

            int hallId = 0, theatreId = 0;
            GetShowDetails(conn, showId, ref movieId, ref hallId, ref theatreId);

            if (theatreId == 0 || hallId == 0)
            {
                TempData["Error"] = "Could not determine Theatre/Hall for this show.";
                return RedirectToAction("Edit", new { id });
            }

            if (t.TicketStatus == "PURCHASED" && !t.TicketPurchaseTime.HasValue)
                t.TicketPurchaseTime = DateTime.Now;

            using var trans = conn.BeginTransaction();
            try
            {
                if (t.TicketStatus == "CANCELLED")
                {
                    CreateCancellationRecord(conn, trans, t.TicketId, "Cancelled by admin");
                }

                using (var cmd = new OracleCommand(
                    "UPDATE TICKET3 SET TICKETSTATUS=:status, TICKETBOOKINGTIME=:booktime, TICKETPURCHASETIME=:purchtime, SEATNO=:seat WHERE TICKETID=:id", conn))
                {
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = (object?)t.TicketStatus ?? DBNull.Value;
                    cmd.Parameters.Add("booktime", OracleDbType.Date).Value = t.TicketBookingTime.HasValue ? (object)t.TicketBookingTime.Value : DBNull.Value;
                    cmd.Parameters.Add("purchtime", OracleDbType.Date).Value = t.TicketPurchaseTime.HasValue ? (object)t.TicketPurchaseTime.Value : DBNull.Value;
                    cmd.Parameters.Add("seat", OracleDbType.Varchar2).Value = (object?)t.SeatNo ?? DBNull.Value;
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = t.TicketId;
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new OracleCommand(
                    "UPDATE CUSTOMERTICKET3 SET CUSTOMERID=:cid, MOVIEID=:mid, THEATREID=:thid, HALLID=:hid, SHOWID=:sid WHERE TICKETID=:tid", conn))
                {
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("cid", OracleDbType.Int32).Value = customerId;
                    cmd.Parameters.Add("mid", OracleDbType.Int32).Value = movieId;
                    cmd.Parameters.Add("thid", OracleDbType.Int32).Value = theatreId;
                    cmd.Parameters.Add("hid", OracleDbType.Int32).Value = hallId;
                    cmd.Parameters.Add("sid", OracleDbType.Int32).Value = showId;
                    cmd.Parameters.Add("tid", OracleDbType.Int32).Value = t.TicketId;
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                TempData["Error"] = "Update failed: " + ex.Message;
                return RedirectToAction("Edit", new { id });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            using var trans = conn.BeginTransaction();
            try
            {
                using (var cmd = new OracleCommand("DELETE FROM CANCELLATION3 WHERE TICKETID=:id", conn))
                {
                    cmd.Transaction = trans;
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new OracleCommand("DELETE FROM CUSTOMERTICKET3 WHERE TICKETID=:id", conn))
                {
                    cmd.Transaction = trans;
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new OracleCommand("DELETE FROM TICKET3 WHERE TICKETID=:id", conn))
                {
                    cmd.Transaction = trans;
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
            }
            catch (OracleException)
            {
                trans.Rollback();
                TempData["Error"] = "Cannot delete this ticket.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult TicketOverview()
        {
            using var conn = _db.GetConnection();
            AutoCancelExpiredBookings(conn);

            var list = new List<UserTicketResult>();
            var sql = @"
                SELECT C.CUSTOMERNAME, C.CUSTOMERADDRESS, C.CUSTOMERCONTACTNUMBER,
                    T.TICKETID, T.TICKETSTATUS, T.SEATNO, T.TICKETBOOKINGTIME, T.TICKETPURCHASETIME,
                    M.MOVIENAME, TH.THEATRENAME, H.HALLNAME, S.SHOWTIMING,
                    TP.TICKETPOLICYPRICE, TP.TICKETPAYMENTPOLICY
                FROM CUSTOMERTICKET3 CT
                JOIN CUSTOMER3 C ON CT.CUSTOMERID = C.CUSTOMERID
                JOIN TICKET3 T ON CT.TICKETID = T.TICKETID
                JOIN MOVIE3 M ON CT.MOVIEID = M.MOVIEID
                JOIN THEATER3 TH ON CT.THEATREID = TH.THEATREID
                JOIN HALL3 H ON CT.HALLID = H.HALLID
                JOIN SHOW3 S ON CT.SHOWID = S.SHOWID
                LEFT JOIN SHOWPOLICY3 SP ON CT.SHOWID = SP.SHOWID
                LEFT JOIN TICKETPOLICY3 TP ON SP.TICKETPOLICYID = TP.TICKETPOLICYID
                ORDER BY T.TICKETID";
            using var cmd = new OracleCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new UserTicketResult
                {
                    CustomerName = reader.GetString(0),
                    CustomerAddress = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                    ContactNumber = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                    TicketId = reader.GetInt32(3),
                    TicketStatus = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                    SeatNo = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                    BookingTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    PurchaseTime = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    MovieName = reader.GetString(8),
                    TheaterName = reader.GetString(9),
                    HallName = reader.GetString(10),
                    ShowTiming = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    TicketPrice = reader.IsDBNull(12) ? null : reader.GetDecimal(12),
                    PolicyName = reader.IsDBNull(13) ? null : reader.GetString(13)
                });
            return View(list);
        }
        

        private void AutoCancelExpiredBookings(OracleConnection conn)
        {
            try
            {
                var sql = @"
                    SELECT T.TICKETID
                    FROM TICKET3 T
                    JOIN CUSTOMERTICKET3 CT ON T.TICKETID = CT.TICKETID
                    JOIN SHOW3 S ON CT.SHOWID = S.SHOWID
                    WHERE T.TICKETSTATUS = 'BOOKED'
                    AND S.SHOWTIMING <= SYSDATE + (1/24)";

                var ticketIds = new List<int>();
                using (var cmd = new OracleCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        ticketIds.Add(r.GetInt32(0));

                foreach (var ticketId in ticketIds)
                {
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        using (var cmd = new OracleCommand(
                            "UPDATE TICKET3 SET TICKETSTATUS = 'CANCELLED' WHERE TICKETID = :id", conn))
                        {
                            cmd.Transaction = trans;
                            cmd.Parameters.Add("id", ticketId);
                            cmd.ExecuteNonQuery();
                        }
                        CreateCancellationRecord(conn, trans, ticketId, "Auto-cancelled: not purchased within 1hr before showtime");
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }
            }
            catch { }
        }

        private void CreateCancellationRecord(OracleConnection conn, OracleTransaction trans, int ticketId, string reason)
        {
            int cancellationId = 1;
            using (var cmd = new OracleCommand("SELECT NVL(MAX(CANCELLATIONID), 0) + 1 FROM CANCELLATION3", conn))
            {
                cmd.Transaction = trans;
                cancellationId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            using var ins = new OracleCommand(
                "INSERT INTO CANCELLATION3 (CANCELLATIONID, TICKETID, CANCELLATIONTIME, CANCELLATIONREASON) VALUES (:cid, :tid, SYSDATE, :reason)", conn);
            ins.Transaction = trans;
            ins.BindByName = true;
            ins.Parameters.Add("cid", OracleDbType.Int32).Value = cancellationId;
            ins.Parameters.Add("tid", OracleDbType.Int32).Value = ticketId;
            ins.Parameters.Add("reason", OracleDbType.Varchar2).Value = reason;
            ins.ExecuteNonQuery();
        }

        // ── Helpers ────────────────────────────────────────────────────

        private void GetShowDetails(OracleConnection conn, int showId, ref int movieId, ref int hallId, ref int theatreId)
        {
            if (showId == 0) return;
            var sql = @"SELECT S.MOVIEID, S.HALLID, H.THEATREID 
                        FROM SHOW3 S 
                        JOIN HALL3 H ON S.HALLID = H.HALLID 
                        WHERE S.SHOWID = :id";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("id", showId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return;
            if (movieId == 0 && !r.IsDBNull(0)) movieId = Convert.ToInt32(r.GetValue(0));
            if (hallId == 0 && !r.IsDBNull(1)) hallId = Convert.ToInt32(r.GetValue(1));
            if (theatreId == 0 && !r.IsDBNull(2)) theatreId = Convert.ToInt32(r.GetValue(2));
        }

        private int GetTicketPolicyForShow(OracleConnection conn, int showId)
        {
            using var cmd = new OracleCommand(
                "SELECT TICKETPOLICYID FROM SHOWPOLICY3 WHERE SHOWID = :id AND ROWNUM = 1", conn);
            cmd.Parameters.Add("id", showId);
            var val = cmd.ExecuteScalar();
            return val != null && val != DBNull.Value ? Convert.ToInt32(val) : 0;
        }

        private Dictionary<int, string> GetCustomerNameByTicketId(OracleConnection conn)
        {
            var d = new Dictionary<int, string>();
            var sql = @"SELECT CT.TICKETID, C.CUSTOMERNAME FROM CUSTOMERTICKET3 CT
                        JOIN CUSTOMER3 C ON CT.CUSTOMERID = C.CUSTOMERID";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) d[r.GetInt32(0)] = r.GetString(1);
            return d;
        }

        private Dictionary<int, string> GetShowTextByTicketId(OracleConnection conn)
        {
            var d = new Dictionary<int, string>();
            var sql = @"SELECT CT.TICKETID, M.MOVIENAME, H.HALLNAME, S.SHOWTIMING
                        FROM CUSTOMERTICKET3 CT 
                        JOIN SHOW3 S ON CT.SHOWID = S.SHOWID
                        JOIN MOVIE3 M ON CT.MOVIEID = M.MOVIEID 
                        JOIN HALL3 H ON CT.HALLID = H.HALLID";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var time = r.IsDBNull(3) ? "" : r.GetDateTime(3).ToString("MMM dd, yyyy");
                d[r.GetInt32(0)] = $"{r.GetString(1)} - {r.GetString(2)} - {time}";
            }
            return d;
        }

        private List<Customer> GetCustomers(OracleConnection conn)
        {
            var list = new List<Customer>();
            using var cmd = new OracleCommand("SELECT CUSTOMERID, CUSTOMERNAME FROM CUSTOMER3 ORDER BY CUSTOMERNAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Customer { CustomerId = r.GetInt32(0), CustomerName = r.GetString(1) });
            return list;
        }

        private List<Movie> GetMovies(OracleConnection conn)
        {
            var list = new List<Movie>();
            using var cmd = new OracleCommand("SELECT MOVIEID, MOVIENAME FROM MOVIE3 ORDER BY MOVIENAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Movie { MovieId = r.GetInt32(0), MovieName = r.GetString(1) });
            return list;
        }

        private List<ShowOption> GetShowOptions(OracleConnection conn)
        {
            var list = new List<ShowOption>();
            var sql = @"SELECT S.SHOWID, S.SHOWTIMING, S.MOVIEID, T.THEATRENAME, H.HALLNAME, S.HALLID, H.THEATREID
                        FROM SHOW3 S
                        JOIN HALL3 H ON S.HALLID = H.HALLID
                        JOIN THEATER3 T ON H.THEATREID = T.THEATREID
                        ORDER BY S.SHOWTIMING";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var time = r.IsDBNull(1) ? "" : r.GetDateTime(1).ToString("MMM dd, yyyy");
                list.Add(new ShowOption
                {
                    ShowId = r.GetInt32(0),
                    DisplayText = $"{r.GetString(3)} - {r.GetString(4)} - {time}",
                    MovieId = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                    HallId = r.IsDBNull(5) ? 0 : r.GetInt32(5),
                    TheatreId = r.IsDBNull(6) ? 0 : r.GetInt32(6)
                });
            }
            return list;
        }
    }
}