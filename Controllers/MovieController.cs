using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class MovieController : Controller
    {
        private readonly OracleDbService _db;
        public MovieController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            var movies = new List<Movie>();
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT MOVIEID, MOVIENAME, MOVIELANGUAGE, MOVIERELEASEDATE, MOVIEDURATION, MOVIEGENRE FROM MOVIE3", conn);
            cmd.BindByName = true;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                movies.Add(new Movie
                {
                    MovieId = reader.GetInt32(0),
                    MovieName = reader.GetString(1),
                    MovieLanguage = reader.IsDBNull(2) ? null : reader.GetString(2),
                    MovieReleaseDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    MovieDuration = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    MovieGenre = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            return View(movies);
        }

        public IActionResult Create() => View(new Movie());

        [HttpPost]
        public IActionResult Create(Movie m)
        {
            if (string.IsNullOrWhiteSpace(m?.MovieName))
            {
                ModelState.AddModelError("MovieName", "Movie name is required.");
                return View(m ?? new Movie());
            }
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = new OracleCommand(
                    "INSERT INTO MOVIE3 (MOVIEID, MOVIENAME, MOVIELANGUAGE, MOVIERELEASEDATE, MOVIEDURATION, MOVIEGENRE) VALUES (:p_id, :p_name, :p_lang, :p_date, :p_dur, :p_genre)", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = m.MovieId;
                cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = m.MovieName?.Trim() ?? "";
                cmd.Parameters.Add("p_lang", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(m.MovieLanguage) ? (object)DBNull.Value : m.MovieLanguage;
                cmd.Parameters.Add("p_date", OracleDbType.Date).Value = m.MovieReleaseDate.HasValue ? m.MovieReleaseDate.Value : (object)DBNull.Value;
                cmd.Parameters.Add("p_dur", OracleDbType.Int32).Value = m.MovieDuration.HasValue ? m.MovieDuration.Value : (object)DBNull.Value;
                cmd.Parameters.Add("p_genre", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(m.MovieGenre) ? (object)DBNull.Value : m.MovieGenre;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", ex.Number == 1 ? "A movie with this ID already exists." : "Database error: " + ex.Message);
                return View(m);
            }
        }

        public IActionResult CheckId(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT COUNT(*) FROM MOVIE3 WHERE MOVIEID = :p_id", conn);
            cmd.Parameters.Add("p_id", id);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return Json(count > 0);
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT MOVIEID, MOVIENAME, MOVIELANGUAGE, MOVIERELEASEDATE, MOVIEDURATION, MOVIEGENRE FROM MOVIE3 WHERE MOVIEID=:p_id", conn);
            cmd.BindByName = true;
            cmd.Parameters.Add("p_id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();
            return View(new Movie
            {
                MovieId = reader.GetInt32(0),
                MovieName = reader.GetString(1),
                MovieLanguage = reader.IsDBNull(2) ? null : reader.GetString(2),
                MovieReleaseDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                MovieDuration = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                MovieGenre = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        [HttpPost]
        public IActionResult Edit(Movie m)
        {
            if (string.IsNullOrWhiteSpace(m?.MovieName))
            {
                ModelState.AddModelError("MovieName", "Movie name is required.");
                return View(m ?? new Movie());
            }
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = new OracleCommand(
                    "UPDATE MOVIE3 SET MOVIENAME=:p_name, MOVIELANGUAGE=:p_lang, MOVIERELEASEDATE=:p_date, MOVIEDURATION=:p_dur, MOVIEGENRE=:p_genre WHERE MOVIEID=:p_id", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = m.MovieName.Trim();
                cmd.Parameters.Add("p_lang", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(m.MovieLanguage) ? (object)DBNull.Value : m.MovieLanguage;
                cmd.Parameters.Add("p_date", OracleDbType.Date).Value = m.MovieReleaseDate.HasValue ? m.MovieReleaseDate.Value : (object)DBNull.Value;
                cmd.Parameters.Add("p_dur", OracleDbType.Int32).Value = m.MovieDuration.HasValue ? m.MovieDuration.Value : (object)DBNull.Value;
                cmd.Parameters.Add("p_genre", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(m.MovieGenre) ? (object)DBNull.Value : m.MovieGenre;
                cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = m.MovieId;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", "Database error: " + ex.Message);
                return View(m);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand("DELETE FROM MOVIE3 WHERE MOVIEID=:id", conn);
                cmd.Parameters.Add("id", id);
                cmd.ExecuteNonQuery();
            }
            catch (OracleException)
            {
                TempData["Error"] = "Cannot delete: this movie has shows assigned to it.";
            }
            return RedirectToAction("Index");
        }
    }
}