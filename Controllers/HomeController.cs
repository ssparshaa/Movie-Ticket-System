using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MovieTicketAppFinal.Controllers
{
    public class HomeController : Controller
    {
        private readonly OracleDbService _db;
        public HomeController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            using var conn = _db.GetConnection();

            ViewBag.TotalMovies    = GetCount(conn, "SELECT COUNT(*) FROM MOVIE3");
            ViewBag.TotalCustomers = GetCount(conn, "SELECT COUNT(*) FROM CUSTOMER3");
            ViewBag.TotalTheaters  = GetCount(conn, "SELECT COUNT(*) FROM THEATER3");
            ViewBag.TotalHalls     = GetCount(conn, "SELECT COUNT(*) FROM HALL3");
            ViewBag.TotalTickets   = GetCount(conn, "SELECT COUNT(*) FROM TICKET3");
            ViewBag.TotalShows     = GetCount(conn, "SELECT COUNT(*) FROM SHOW3");
            ViewBag.Booked         = GetCount(conn, "SELECT COUNT(*) FROM TICKET3 WHERE TICKETSTATUS='BOOKED'");
            ViewBag.Purchased      = GetCount(conn, "SELECT COUNT(*) FROM TICKET3 WHERE TICKETSTATUS='PURCHASED'");
            ViewBag.Cancelled      = GetCount(conn, "SELECT COUNT(*) FROM TICKET3 WHERE TICKETSTATUS='CANCELLED'");

            ViewBag.TotalRevenue = GetDecimal(conn, @"
                SELECT NVL(SUM(TP.TICKETPOLICYPRICE), 0)
                FROM CUSTOMERTICKET3 CT
                JOIN TICKET3 T ON CT.TICKETID = T.TICKETID
                JOIN SHOWPOLICY3 SP ON CT.SHOWID = SP.SHOWID
                JOIN TICKETPOLICY3 TP ON SP.TICKETPOLICYID = TP.TICKETPOLICYID
                WHERE T.TICKETSTATUS = 'PURCHASED'");

            ViewBag.RevenueByPolicy = GetRevenueByPolicy(conn);

            return View();
        }

        private int GetCount(OracleConnection conn, string sql)
        {
            using var cmd = new OracleCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result);
        }

        private decimal GetDecimal(OracleConnection conn, string sql)
        {
            using var cmd = new OracleCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value || result == null ? 0 : Convert.ToDecimal(result);
        }

        private List<KeyValuePair<string, decimal>> GetRevenueByPolicy(OracleConnection conn)
        {
            var list = new List<KeyValuePair<string, decimal>>();
            var sql = @"
                SELECT TP.TICKETPAYMENTPOLICY, NVL(SUM(TP.TICKETPOLICYPRICE), 0) AS REV
                FROM CUSTOMERTICKET3 CT
                JOIN TICKET3 T ON CT.TICKETID = T.TICKETID
                JOIN SHOWPOLICY3 SP ON CT.SHOWID = SP.SHOWID
                JOIN TICKETPOLICY3 TP ON SP.TICKETPOLICYID = TP.TICKETPOLICYID
                WHERE T.TICKETSTATUS = 'PURCHASED'
                GROUP BY TP.TICKETPAYMENTPOLICY
                ORDER BY REV DESC";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new KeyValuePair<string, decimal>(
                    r.IsDBNull(0) ? "Unknown" : r.GetString(0),
                    r.IsDBNull(1) ? 0 : r.GetDecimal(1)));
            return list;
        }
    }
}