using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class ComplexController : Controller
    {
        private readonly OracleDbService _db;
        public ComplexController(OracleDbService db) => _db = db;

        
        public IActionResult UserTicket()
        {
            using var conn = _db.GetConnection();
            ViewBag.Customers = GetCustomers(conn);
            return View();
        }

        [HttpPost]
        public IActionResult UserTicket(int customerId)
        {
            using var conn = _db.GetConnection();
            ViewBag.Customers = GetCustomers(conn);
            ViewBag.SelectedCustomerId = customerId;

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
    WHERE CT.CUSTOMERID = :cid
    AND T.TICKETSTATUS = 'PURCHASED'
    AND T.TICKETPURCHASETIME >= ADD_MONTHS(SYSDATE, -6)
    ORDER BY T.TICKETPURCHASETIME DESC";
            
            var list = new List<UserTicketResult>();
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("cid", customerId);
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

            ViewBag.TicketResults = list;
            return View();
        }

        // ============ FORM 2: Theater Movie ============
        public IActionResult TheaterMovie()
        {
            using var conn = _db.GetConnection();
            ViewBag.Theaters = GetTheaters(conn);
            return View();
        }

        [HttpPost]
        public IActionResult TheaterMovie(int theaterId)
        {
            using var conn = _db.GetConnection();
            ViewBag.Theaters = GetTheaters(conn);
            ViewBag.SelectedTheaterId = theaterId;

            var sql = @"
                SELECT TH.THEATRENAME, H.HALLNAME, H.HALLCAPACITY,
                    M.MOVIENAME, M.MOVIELANGUAGE, M.MOVIEGENRE, M.MOVIERELEASEDATE, M.MOVIEDURATION,
                    S.SHOWTIMING, S.SHOWDURATION
                FROM SHOW3 S
                JOIN MOVIE3 M ON S.MOVIEID = M.MOVIEID
                JOIN HALL3 H ON S.HALLID = H.HALLID
                JOIN THEATER3 TH ON H.THEATREID = TH.THEATREID
                WHERE TH.THEATREID = :tid
                ORDER BY S.SHOWTIMING";

            var list = new List<TheaterMovieResult>();
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("tid", theaterId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new TheaterMovieResult
                {
                    TheaterName = reader.GetString(0),
                    HallName = reader.GetString(1),
                    HallCapacity = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    MovieName = reader.GetString(3),
                    Language = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                    Genre = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                    ReleaseDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    Duration = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    ShowTiming = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    ShowDuration = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                });

            ViewBag.TheaterResults = list;
            return View();
        }

        // ============ FORM 3: Occupancy Performer ============
        public IActionResult OccupancyPerformer()
        {
            using var conn = _db.GetConnection();
            ViewBag.Movies = GetMovies(conn);
            return View();
        }

        [HttpPost]
        public IActionResult OccupancyPerformer(int movieId)
        {
            using var conn = _db.GetConnection();
            ViewBag.Movies = GetMovies(conn);
            ViewBag.SelectedMovieId = movieId;

            var sql = @"
                SELECT * FROM (
                    SELECT TH.THEATRENAME, H.HALLNAME, NVL(H.HALLCAPACITY, 1) AS HALLCAPACITY,
                        COUNT(T.TICKETID) AS PAID_TICKETS,
                        ROUND(COUNT(T.TICKETID) * 100.0 / NULLIF(NVL(H.HALLCAPACITY, 1), 0), 2) AS OCCUPANCY_PCT
                    FROM CUSTOMERTICKET3 CT
                    JOIN TICKET3 T ON CT.TICKETID = T.TICKETID
                    JOIN THEATER3 TH ON CT.THEATREID = TH.THEATREID
                    JOIN HALL3 H ON CT.HALLID = H.HALLID
                    WHERE CT.MOVIEID = :mid AND UPPER(TRIM(T.TICKETSTATUS)) = 'PURCHASED'
                    GROUP BY TH.THEATRENAME, H.HALLNAME, H.HALLCAPACITY
                    ORDER BY OCCUPANCY_PCT DESC
                ) WHERE ROWNUM <= 3";

            var list = new List<OccupancyResult>();
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("mid", movieId);
            using var reader = cmd.ExecuteReader();
            int rank = 1;
            while (reader.Read())
                list.Add(new OccupancyResult
                {
                    Rank = rank++,
                    TheaterName = reader.GetString(0),
                    HallName = reader.GetString(1),
                    HallCapacity = reader.GetInt32(2),
                    PaidTickets = reader.GetInt32(3),
                    OccupancyPct = reader.GetDouble(4)
                });

            ViewBag.OccupancyResults = list;
            return View();
        }

        // ── Helpers ────────────────────────────────────────────────────

        private List<Customer> GetCustomers(OracleConnection conn)
        {
            var list = new List<Customer>();
            using var cmd = new OracleCommand("SELECT CUSTOMERID, CUSTOMERNAME FROM CUSTOMER3 ORDER BY CUSTOMERNAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Customer { CustomerId = r.GetInt32(0), CustomerName = r.GetString(1) });
            return list;
        }

        private List<Theater> GetTheaters(OracleConnection conn)
        {
            var list = new List<Theater>();
            using var cmd = new OracleCommand("SELECT THEATREID, THEATRENAME FROM THEATER3 ORDER BY THEATRENAME", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Theater { TheatreId = r.GetInt32(0), TheatreName = r.GetString(1) });
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
    }
}