using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class TicketPolicyController : Controller
    {
        private readonly OracleDbService _db;
        public TicketPolicyController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            var list = new List<TicketPolicy>();
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT TICKETPOLICYID, TICKETBASEPRICE, TICKETPOLICYPRICE, TICKETPAYMENTPOLICY FROM TICKETPOLICY3 ORDER BY TICKETPOLICYID", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new TicketPolicy
                {
                    TicketPolicyId = reader.GetInt32(0),
                    TicketBasePrice = reader.GetDecimal(1),
                    TicketPolicyPrice = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    TicketPaymentPolicy = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            return View(list);
        }

        public IActionResult Create() => View(new TicketPolicy());

        [HttpPost]
        public IActionResult Create(TicketPolicy p)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "INSERT INTO TICKETPOLICY3 (TICKETPOLICYID, TICKETBASEPRICE, TICKETPOLICYPRICE, TICKETPAYMENTPOLICY) VALUES (:id, :base, :price, :policy)", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = p.TicketPolicyId;
                cmd.Parameters.Add("base", OracleDbType.Decimal).Value = p.TicketBasePrice;
                cmd.Parameters.Add("price", OracleDbType.Decimal).Value = p.TicketPolicyPrice.HasValue ? (object)p.TicketPolicyPrice.Value : DBNull.Value;
                cmd.Parameters.Add("policy", OracleDbType.Varchar2).Value = (object?)p.TicketPaymentPolicy ?? DBNull.Value;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", ex.Number == 1 ? "A policy with this ID already exists." : "Database error: " + ex.Message);
                return View(p);
            }
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT TICKETPOLICYID, TICKETBASEPRICE, TICKETPOLICYPRICE, TICKETPAYMENTPOLICY FROM TICKETPOLICY3 WHERE TICKETPOLICYID=:id", conn);
            cmd.Parameters.Add("id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();
            return View(new TicketPolicy
            {
                TicketPolicyId = reader.GetInt32(0),
                TicketBasePrice = reader.GetDecimal(1),
                TicketPolicyPrice = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                TicketPaymentPolicy = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        [HttpPost]
        public IActionResult Edit(TicketPolicy p)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "UPDATE TICKETPOLICY3 SET TICKETBASEPRICE=:base, TICKETPOLICYPRICE=:price, TICKETPAYMENTPOLICY=:policy WHERE TICKETPOLICYID=:id", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("base", OracleDbType.Decimal).Value = p.TicketBasePrice;
                cmd.Parameters.Add("price", OracleDbType.Decimal).Value = p.TicketPolicyPrice.HasValue ? (object)p.TicketPolicyPrice.Value : DBNull.Value;
                cmd.Parameters.Add("policy", OracleDbType.Varchar2).Value = (object?)p.TicketPaymentPolicy ?? DBNull.Value;
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = p.TicketPolicyId;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", "Database error: " + ex.Message);
                return View(p);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            try
            {
                // Remove show links first
                using (var cmd = new OracleCommand("DELETE FROM SHOWPOLICY3 WHERE TICKETPOLICYID=:id", conn))
                {
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new OracleCommand("DELETE FROM TICKETPOLICY3 WHERE TICKETPOLICYID=:id", conn))
                {
                    cmd.Parameters.Add("id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (OracleException)
            {
                TempData["Error"] = "Cannot delete this policy.";
            }
            return RedirectToAction("Index");
        }
    }
}