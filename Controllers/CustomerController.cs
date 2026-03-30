using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MovieTicketAppFinal.Models;

namespace MovieTicketAppFinal.Controllers
{
    public class CustomerController : Controller
    {
        private readonly OracleDbService _db;
        public CustomerController(OracleDbService db) => _db = db;

        public IActionResult Index()
        {
            var list = new List<Customer>();
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT CUSTOMERID, CUSTOMERNAME, CUSTOMERADDRESS, CUSTOMERCONTACTNUMBER FROM CUSTOMER3", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Customer
                {
                    CustomerId = reader.GetInt32(0),
                    CustomerName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    CustomerAddress = reader.IsDBNull(2) ? null : reader.GetString(2),
                    CustomerContactNumber = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            return View(list);
        }

        public IActionResult Create() => View(new Customer());

        public IActionResult CheckId(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT COUNT(*) FROM CUSTOMER3 WHERE CUSTOMERID = :p_id", conn);
            cmd.Parameters.Add("p_id", id);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return Json(count > 0);
        }

        [HttpPost]
        public IActionResult Create(Customer c)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "INSERT INTO CUSTOMER3 (CUSTOMERID, CUSTOMERNAME, CUSTOMERADDRESS, CUSTOMERCONTACTNUMBER) VALUES (:p_id, :p_name, :p_addr, :p_contact)", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = c.CustomerId;
                cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = (object?)c.CustomerName ?? DBNull.Value;
                cmd.Parameters.Add("p_addr", OracleDbType.Varchar2).Value = (object?)c.CustomerAddress ?? DBNull.Value;
                cmd.Parameters.Add("p_contact", OracleDbType.Varchar2).Value = (object?)c.CustomerContactNumber ?? DBNull.Value;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", ex.Number == 1 ? "A customer with this ID already exists." : "Database error: " + ex.Message);
                return View(c);
            }
        }

        public IActionResult Edit(int id)
        {
            using var conn = _db.GetConnection();
            using var cmd = new OracleCommand("SELECT CUSTOMERID, CUSTOMERNAME, CUSTOMERADDRESS, CUSTOMERCONTACTNUMBER FROM CUSTOMER3 WHERE CUSTOMERID=:p_id", conn);
            cmd.BindByName = true;
            cmd.Parameters.Add("p_id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound();
            return View(new Customer
            {
                CustomerId = reader.GetInt32(0),
                CustomerName = reader.IsDBNull(1) ? null : reader.GetString(1),
                CustomerAddress = reader.IsDBNull(2) ? null : reader.GetString(2),
                CustomerContactNumber = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        [HttpPost]
        public IActionResult Edit(Customer c)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand(
                    "UPDATE CUSTOMER3 SET CUSTOMERNAME=:p_name, CUSTOMERADDRESS=:p_addr, CUSTOMERCONTACTNUMBER=:p_contact WHERE CUSTOMERID=:p_id", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = (object?)c.CustomerName ?? DBNull.Value;
                cmd.Parameters.Add("p_addr", OracleDbType.Varchar2).Value = (object?)c.CustomerAddress ?? DBNull.Value;
                cmd.Parameters.Add("p_contact", OracleDbType.Varchar2).Value = (object?)c.CustomerContactNumber ?? DBNull.Value;
                cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = c.CustomerId;
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (OracleException ex)
            {
                ModelState.AddModelError("", "Database error: " + ex.Message);
                return View(c);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            try
            {
                using var cmd = new OracleCommand("DELETE FROM CUSTOMER3 WHERE CUSTOMERID=:p_id", conn);
                cmd.Parameters.Add("p_id", id);
                cmd.ExecuteNonQuery();
            }
            catch (OracleException)
            {
                TempData["Error"] = "Cannot delete: customer has existing bookings.";
            }
            return RedirectToAction("Index");
        }
    }
}