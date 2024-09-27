using EmployeeProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EmployeeProject.Controllers
{
    public class UserController : Controller
    {
        private readonly IConfiguration _configuration;
        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult MyProfile()
        {
            // Assuming you have stored UserId in session after login
            int EmpId = Convert.ToInt32(HttpContext.Session.GetString("EmpId"));
            var userProfile = GetUserProfile(EmpId);

            return View(userProfile);
        }

        private UserProfile GetUserProfile(int EmpId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            UserProfile userProfile = new UserProfile();
            userProfile.Companies = new List<string>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetUserProfileByIdTest", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EmpId", EmpId);

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                       
                        if (reader.Read())
                        {
                            userProfile.FirstName = reader["FirstName"].ToString();
                            userProfile.LastName = reader["LastName"].ToString();
                            userProfile.PhoneNumber = reader["PhoneNumber"].ToString();
                            userProfile.Address = reader["Address"].ToString();
                            userProfile.Email = reader["Email"].ToString();
                            userProfile.ProfilePhotoPath = reader["PhotoPath"].ToString();
                        }

                        // Move to the second result set (companies list)
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                userProfile.Companies.Add(reader["CompanyName"].ToString());
                            }
                        }
                    }
                }
            }

            return userProfile;
        }
        [HttpPost]
        public IActionResult AddCompany(string companyName)
        {
            int EmpId = Convert.ToInt32(HttpContext.Session.GetString("EmpId"));

            if (!string.IsNullOrWhiteSpace(companyName) && EmpId > 0)
            {
                
                AddCompanyToDatabase(EmpId, companyName);
                return Json(new { companyName = companyName }); 
            }

            return Json(new { error = "Company name is required." });
        }


        private void AddCompanyToDatabase(int empId, string companyName)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                string query = "INSERT INTO Companies (CompanyName, EmpId) VALUES (@CompanyName, @EmpId)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CompanyName", companyName);
                    command.Parameters.AddWithValue("@EmpId", empId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

       
    }
}
