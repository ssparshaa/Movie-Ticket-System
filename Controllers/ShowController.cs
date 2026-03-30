using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class ShowController : Controller
    {
        private readonly OracleDbService _db;
        public ShowController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            var list = new List<Show>();
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT SHOWID, MOVIEID, HALLID, SHOWTIMING, SHOWDURATION FROM SHOW3 ORDER BY SHOWTIMING", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Show
                {
                    ShowId = reader.GetInt32(0),
                    MovieId = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                    HallId = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                    ShowTiming = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    ShowDuration = reader.IsDBNull(4) ? null : Convert.ToInt32(reader.GetValue(4))
                });

            ViewBag.MovieNames = GetMovieNames(conn);
            ViewBag.HallNames = GetHallNames(conn);
            ViewBag.PolicyByShow = GetPolicyByShowId(conn);
            return View(list);
        }

        public IActionResult Create()
        {
            using var conn = _db.GetConnection();
            ViewBag.Movies = GetMovies(conn);
            ViewBag.Halls = GetHallsWithTheatre(conn);
            ViewBag.Policies = GetPolicies(conn);
            return View(new Show());
        }

        [HttpPost]
        public IActionResult Create(Show s, int ticketPolicyId)
        {
            using var conn = _db.GetConnection();
            using var trans = conn.BeginTransaction();
            try
            {
                // Insert show
                using (var cmd = new OracleCommand(
                    "INSERT INTO SHOW3 (SHOWID, MOVIEID, HALLID, SHOWTIMING, SHOWDURATION) VALUES (:id, :mid, :hid, :timing, :dur)", conn))
                {
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = s.ShowId;
                    cmd.Parameters.Add("mid", OracleDbType.Int32).Value = s.MovieId;
                    cmd.Parameters.Add("hid", OracleDbType.Int32).Value = s.HallId;
                    cmd.Parameters.Add("timing", OracleDbType.Date).Value = s.ShowTiming.HasValue ? (object)s.ShowTiming.Value : DBNull.Value;
                    cmd.Parameters.Add("dur", OracleDbType.Int32).Value = s.ShowDuration.HasValue ? (object)s.ShowDuration.Value : DBNull.Value;
                    cmd.ExecuteNonQuery();
                }

                // Link ticket policy if selected
                if (ticketPolicyId > 0)
                {
                    using var cmd = new OracleCommand(
                        "INSERT INTO SHOWPOLICY3 (SHOWID, TICKETPOLICYID) VALUES (:sid, :pid)", conn);
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("sid", OracleDbType.Int32).Value = s.ShowId;
                    cmd.Parameters.Add("pid", OracleDbType.Int32).Value = ticketPolicyId;
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                trans.Rollback();
                ModelState.AddModelError("", ex.Number == 1 ? "A show with this ID already exists." : "Database error: " + ex.Message);
                ViewBag.Movies = GetMovies(conn);
                ViewBag.Halls = GetHallsWithTheatre(conn);
                ViewBag.Policies = GetPolicies(conn);
                return View(s);
            }
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT SHOWID, MOVIEID, HALLID, SHOWTIMING, SHOWDURATION FROM SHOW3 WHERE SHOWID=:id", conn);
            cmd.Parameters.Add("id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();

            var show = new Show
            {
                ShowId = reader.GetInt32(0),
                MovieId = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                HallId = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                ShowTiming = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                ShowDuration = reader.IsDBNull(4) ? null : Convert.ToInt32(reader.GetValue(4))
            };
            reader.Close();

            // Get current policy
            ViewBag.CurrentPolicyId = GetCurrentPolicyId(conn, id);
            ViewBag.Movies = GetMovies(conn);
            ViewBag.Halls = GetHallsWithTheatre(conn);
            ViewBag.Policies = GetPolicies(conn);
            return View(show);
        }

        [HttpPost]
        public IActionResult Edit(Show s, int ticketPolicyId)
        {
            using var conn = _db.GetConnection();
            using var trans = conn.BeginTransaction();
            try
            {
                // Update show
                using (var cmd = new OracleCommand(
                    "UPDATE SHOW3 SET MOVIEID=:mid, HALLID=:hid, SHOWTIMING=:timing, SHOWDURATION=:dur WHERE SHOWID=:id", conn))
                {
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("mid", OracleDbType.Int32).Value = s.MovieId;
                    cmd.Parameters.Add("hid", OracleDbType.Int32).Value = s.HallId;
                    cmd.Parameters.Add("timing", OracleDbType.Date).Value = s.ShowTiming.HasValue ? (object)s.ShowTiming.Value : DBNull.Value;
                    cmd.Parameters.Add("dur", OracleDbType.Int32).Value = s.ShowDuration.HasValue ? (object)s.ShowDuration.Value : DBNull.Value;
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = s.ShowId;
                    cmd.ExecuteNonQuery();
                }

                // Update policy: delete old, insert new
                using (var cmd = new OracleCommand("DELETE FROM SHOWPOLICY3 WHERE SHOWID=:sid", conn))
                {
                    cmd.Transaction = trans;
                    cmd.Parameters.Add("sid", s.ShowId);
                    cmd.ExecuteNonQuery();
                }

                if (ticketPolicyId > 0)
                {
                    using var cmd = new OracleCommand(
                        "INSERT INTO SHOWPOLICY3 (SHOWID, TICKETPOLICYID) VALUES (:sid, :pid)", conn);
                    cmd.Transaction = trans;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("sid", OracleDbType.Int32).Value = s.ShowId;
                    cmd.Parameters.Add("pid", OracleDbType.Int32).Value = ticketPolicyId;
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                trans.Rollback();
                ModelState.AddModelError("", "Database error: " + ex.Message);
                ViewBag.CurrentPolicyId = ticketPolicyId;
                ViewBag.Movies = GetMovies(conn);
                ViewBag.Halls = GetHallsWithTheatre(conn);
                ViewBag.Policies = GetPolicies(conn);
                return View(s);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            try
            {
                // Delete policy link first
                using (var cmd = new OracleCommand("DELETE FROM SHOWPOLICY3 WHERE SHOWID=:id", conn))
                {
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new OracleCommand("DELETE FROM SHOW3 WHERE SHOWID=:id", conn))
                {
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (OracleException)
            {
                TempData["Error"] = "Cannot delete: this show has tickets booked.";
            }
            return RedirectToAction("Index");
        }

        // ── Helpers ────────────────────────────────────────────────────

        private List<Movie> GetMovies(OracleConnection conn)
        {
            var list = new List<Movie>();
            using var cmd = new OracleCommand("SELECT MOVIEID, MOVIENAME FROM MOVIE3 ORDER BY MOVIENAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Movie { MovieId = r.GetInt32(0), MovieName = r.GetString(1) });
            return list;
        }

        private List<Hall> GetHallsWithTheatre(OracleConnection conn)
        {
            var list = new List<Hall>();
            using var cmd = new OracleCommand(
                "SELECT H.HALLID, H.HALLNAME, H.THEATREID, T.THEATRENAME FROM HALL3 H JOIN THEATER3 T ON H.THEATREID = T.THEATREID ORDER BY T.THEATRENAME, H.HALLNAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Hall
            {
                HallId = r.GetInt32(0),
                HallName = r.GetString(3) + " - " + r.GetString(1), // "Theatre - Hall"
                TheatreId = Convert.ToInt32(r.GetValue(2))
            });
            return list;
        }

        private List<TicketPolicy> GetPolicies(OracleConnection conn)
        {
            var list = new List<TicketPolicy>();
            using var cmd = new OracleCommand(
                "SELECT TICKETPOLICYID, TICKETBASEPRICE, TICKETPOLICYPRICE, TICKETPAYMENTPOLICY FROM TICKETPOLICY3 ORDER BY TICKETPOLICYID", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new TicketPolicy
            {
                TicketPolicyId = r.GetInt32(0),
                TicketBasePrice = r.GetDecimal(1),
                TicketPolicyPrice = r.IsDBNull(2) ? null : r.GetDecimal(2),
                TicketPaymentPolicy = r.IsDBNull(3) ? null : r.GetString(3)
            });
            return list;
        }

        private int GetCurrentPolicyId(OracleConnection conn, int showId)
        {
            using var cmd = new OracleCommand("SELECT TICKETPOLICYID FROM SHOWPOLICY3 WHERE SHOWID=:id AND ROWNUM=1", conn);
            cmd.Parameters.Add("id", showId);
            var val = cmd.ExecuteScalar();
            return val != null && val != DBNull.Value ? Convert.ToInt32(val) : 0;
        }

        private Dictionary<int, string> GetMovieNames(OracleConnection conn)
        {
            var d = new Dictionary<int, string>();
            using var cmd = new OracleCommand("SELECT MOVIEID, MOVIENAME FROM MOVIE3", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) d[r.GetInt32(0)] = r.GetString(1);
            return d;
        }

        private Dictionary<int, string> GetHallNames(OracleConnection conn)
        {
            var d = new Dictionary<int, string>();
            using var cmd = new OracleCommand(
                "SELECT H.HALLID, T.THEATRENAME || ' - ' || H.HALLNAME FROM HALL3 H JOIN THEATER3 T ON H.THEATREID = T.THEATREID", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) d[r.GetInt32(0)] = r.GetString(1);
            return d;
        }

        private Dictionary<int, string> GetPolicyByShowId(OracleConnection conn)
        {
            var d = new Dictionary<int, string>();
            var sql = @"SELECT SP.SHOWID, TP.TICKETPAYMENTPOLICY || ' (Rs ' || TP.TICKETPOLICYPRICE || ')'
                        FROM SHOWPOLICY3 SP JOIN TICKETPOLICY3 TP ON SP.TICKETPOLICYID = TP.TICKETPOLICYID";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) d[r.GetInt32(0)] = r.IsDBNull(1) ? "-" : r.GetString(1);
            return d;
        }
    }
}