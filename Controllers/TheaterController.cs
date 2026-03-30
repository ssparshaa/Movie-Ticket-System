using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class TheaterController : Controller
    {
        private readonly OracleDbService _db;
        public TheaterController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            var list = new List<Theater>();
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT THEATREID, THEATRENAME, THEATREADDRESS FROM THEATER3 ORDER BY THEATREID", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Theater
                {
                    TheatreId = reader.GetInt32(0),
                    TheatreName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TheatreAddress = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            return View(list);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Theater t)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "INSERT INTO THEATER3 (THEATREID, THEATRENAME, THEATREADDRESS) VALUES (:id, :name, :addr)", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = t.TheatreId;
                cmd.Parameters.Add("name", OracleDbType.Varchar2).Value = (object?)t.TheatreName ?? DBNull.Value;
                cmd.Parameters.Add("addr", OracleDbType.Varchar2).Value = (object?)t.TheatreAddress ?? DBNull.Value;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", ex.Number == 1 ? "A theater with this ID already exists." : "Database error: " + ex.Message);
                return View(t);
            }
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT THEATREID, THEATRENAME, THEATREADDRESS FROM THEATER3 WHERE THEATREID=:id", conn);
            cmd.Parameters.Add("id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();
            return View(new Theater
            {
                TheatreId = reader.GetInt32(0),
                TheatreName = reader.IsDBNull(1) ? null : reader.GetString(1),
                TheatreAddress = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        [HttpPost]
        public IActionResult Edit(Theater t)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "UPDATE THEATER3 SET THEATRENAME=:name, THEATREADDRESS=:addr WHERE THEATREID=:id", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("name", OracleDbType.Varchar2).Value = (object?)t.TheatreName ?? DBNull.Value;
                cmd.Parameters.Add("addr", OracleDbType.Varchar2).Value = (object?)t.TheatreAddress ?? DBNull.Value;
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = t.TheatreId;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", "Database error: " + ex.Message);
                return View(t);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand("DELETE FROM THEATER3 WHERE THEATREID=:id", conn);
                cmd.Parameters.Add("id", id);
                cmd.ExecuteNonQuery();
            }
            catch (OracleException)
            {
                TempData["Error"] = "Cannot delete: this theater has halls assigned to it.";
            }
            return RedirectToAction("Index");
        }
    }
}