using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class HallController : Controller
    {
        private readonly OracleDbService _db;
        public HallController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            var list = new List<Hall>();
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT HALLID, THEATREID, HALLNAME, HALLCAPACITY FROM HALL3 ORDER BY HALLID", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Hall
                {
                    HallId = reader.GetInt32(0),
                    TheatreId = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                    HallName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    HallCapacity = reader.IsDBNull(3) ? null : Convert.ToInt32(reader.GetValue(3))
                });
            ViewBag.TheaterNames = GetTheaterNames(conn);
            return View(list);
        }

        public IActionResult Create()
        {
            using var conn = _db.GetConnection();
            ViewBag.Theaters = GetTheaters(conn);
            return View(new Hall());
        }

        [HttpPost]
        public IActionResult Create(Hall h)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "INSERT INTO HALL3 (HALLID, THEATREID, HALLNAME, HALLCAPACITY) VALUES (:id, :tid, :name, :cap)", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = h.HallId;
                cmd.Parameters.Add("tid", OracleDbType.Int32).Value = h.TheatreId;
                cmd.Parameters.Add("name", OracleDbType.Varchar2).Value = (object?)h.HallName ?? DBNull.Value;
                cmd.Parameters.Add("cap", OracleDbType.Int32).Value = h.HallCapacity.HasValue ? (object)h.HallCapacity.Value : DBNull.Value;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", ex.Number == 1 ? "A hall with this ID already exists." : "Database error: " + ex.Message);
                ViewBag.Theaters = GetTheaters(conn);
                return View(h);
            }
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT HALLID, THEATREID, HALLNAME, HALLCAPACITY FROM HALL3 WHERE HALLID=:id", conn);
            cmd.Parameters.Add("id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();

            var hall = new Hall
            {
                HallId = reader.GetInt32(0),
                TheatreId = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                HallName = reader.IsDBNull(2) ? null : reader.GetString(2),
                HallCapacity = reader.IsDBNull(3) ? null : Convert.ToInt32(reader.GetValue(3))
            };
            reader.Close();

            ViewBag.Theaters = GetTheaters(conn);
            return View(hall);
        }

        [HttpPost]
        public IActionResult Edit(Hall h)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "UPDATE HALL3 SET THEATREID=:tid, HALLNAME=:name, HALLCAPACITY=:cap WHERE HALLID=:id", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("tid", OracleDbType.Int32).Value = h.TheatreId;
                cmd.Parameters.Add("name", OracleDbType.Varchar2).Value = (object?)h.HallName ?? DBNull.Value;
                cmd.Parameters.Add("cap", OracleDbType.Int32).Value = h.HallCapacity.HasValue ? (object)h.HallCapacity.Value : DBNull.Value;
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = h.HallId;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", "Database error: " + ex.Message);
                ViewBag.Theaters = GetTheaters(conn);
                return View(h);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand("DELETE FROM HALL3 WHERE HALLID=:id", conn);
                cmd.Parameters.Add("id", id);
                cmd.ExecuteNonQuery();
            }
            catch (OracleException)
            {
                TempData["Error"] = "Cannot delete: this hall has shows assigned to it.";
            }
            return RedirectToAction("Index");
        }

        private List<Theater> GetTheaters(OracleConnection conn)
        {
            var list = new List<Theater>();
            using var cmd = new OracleCommand("SELECT THEATREID, THEATRENAME FROM THEATER3 ORDER BY THEATRENAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Theater { TheatreId = r.GetInt32(0), TheatreName = r.GetString(1) });
            return list;
        }

        private Dictionary<int, string> GetTheaterNames(OracleConnection conn)
        {
            var d = new Dictionary<int, string>();
            using var cmd = new OracleCommand("SELECT THEATREID, THEATRENAME FROM THEATER3", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) d[r.GetInt32(0)] = r.GetString(1);
            return d;
        }
    }
}