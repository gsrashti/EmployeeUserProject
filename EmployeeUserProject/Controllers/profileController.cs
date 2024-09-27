using EmployeeProject.Models;
using Microsoft.AspNetCore.Mvc;

using System.Data.SqlClient;
using System.Reflection;
using System.Transactions;

namespace EmployeeProject.Controllers
{
    public class profileController : Controller
    {
        private readonly IConfiguration _configuration;
        public profileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file) 
        {
            int empid= Convert.ToInt32 (HttpContext.Session.GetString("EmpId"));
            if (file != null && file.Length > 0) 
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileName(file.FileName); 
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream); 
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open(); 
                    string insertPhotoQuery = "INSERT INTO ProfileEMP (EmployeeId, PhotoPath) VALUES (@EmployeeId, @PhotoPath)";
                    using (SqlCommand photoCmd = new SqlCommand(insertPhotoQuery, connection))
                    {
                        photoCmd.Parameters.AddWithValue("@EmployeeId", empid); 
                        photoCmd.Parameters.AddWithValue("@PhotoPath", $"/images/profiles/{fileName}"); 

                        photoCmd.ExecuteNonQuery();
                    }
                }

                ViewBag.Message = "Profile photo uploaded successfully.";
            }
            else
            {
                ViewBag.Message = "Please select a file to upload.";
            }

            return View(); // Return to the same view
        }

    }

}
