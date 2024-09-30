using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using EmployeeProject.Models;

namespace EmployeeProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult AllUsers()
        {
            var users = GetAllEmployeesWithCompanies();
            return View(users);
        }

        private List<EmployeeDetails> GetAllEmployeesWithCompanies()
        {
            var employeeList = new List<EmployeeDetails>();
            var employeeMap = new Dictionary<int, EmployeeDetails>(); 

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("GetAllEmployeesWithCompaniesTest", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int empId = reader.GetInt32(0);

                           
                            if (!employeeMap.ContainsKey(empId))
                            {
                                var employee = new EmployeeDetails
                                {
                                    EmpId = empId,
                                    FirstName = reader.GetString(1),
                                    LastName = reader.GetString(2),
                                    PhoneNumber = reader.GetString(3),
                                    Address = reader.GetString(4),
                                    Email = reader.GetString(5),
                                    ProfilePhotoPath = reader.IsDBNull(6) ? null : reader.GetString(6), 
                                    Role = reader.GetString(7),
                                    Companies = new List<Company>() 
                                };
                                employeeMap[empId] = employee; 
                            }

                            
                            if (!reader.IsDBNull(7)) 
                            {
                                string companyNames = reader.GetString(8);
                               
                                foreach (var companyName in companyNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    employeeMap[empId].Companies.Add(new Company { CompanyName = companyName.Trim() }); // Trim whitespace
                                }
                            }
                        }
                    }
                }
            }

            // Convert the dictionary values to a list
            employeeList = employeeMap.Values.ToList();
            return employeeList;
        }

        [HttpGet]
        public IActionResult AddUserDetails() { return View(); }

        [HttpPost]
        public IActionResult AddUserDetails(UserDetails model, List<string> Companies, IFormFile file)
        {
            // Define your connection string
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert into Users table
                        string insertUserQuery = @"INSERT INTO Users (Username, Password, Role)
                                            OUTPUT INSERTED.Id
                                            VALUES (@Username, @Password, @Role)";
                        int userId;

                        using (SqlCommand cmd = new SqlCommand(insertUserQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Username", model.UserName);
                            cmd.Parameters.AddWithValue("@Password", model.Password);
                            cmd.Parameters.AddWithValue("@Role", "User");

                            userId = (int)cmd.ExecuteScalar();
                        }

                        // Insert into Employee table
                        string insertEmployeeQuery = @"INSERT INTO Employee (FirstName, LastName, PhoneNumber, Address, Email, UserId)
                                                OUTPUT INSERTED.EmpId
                                                VALUES (@FirstName, @LastName, @PhoneNumber, @Address, @Email, @UserId)";
                        int empId;

                        using (SqlCommand employeeCmd = new SqlCommand(insertEmployeeQuery, connection, transaction))
                        {
                            employeeCmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                            employeeCmd.Parameters.AddWithValue("@LastName", model.LastName);
                            employeeCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                            employeeCmd.Parameters.AddWithValue("@Address", model.Address);
                            employeeCmd.Parameters.AddWithValue("@Email", model.Email);
                            employeeCmd.Parameters.AddWithValue("@UserId", userId);
                            empId = (int)employeeCmd.ExecuteScalar();
                        }

                        // Handle file upload
                        string photoPath = null;
                        if (file != null && file.Length > 0)
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var fileName = Path.GetFileName(file.FileName);
                            photoPath = $"/images/profiles/{fileName}";
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                file.CopyTo(stream);
                            }
                        }

                        // Insert into Companies table
                        string insertCompanyQuery = "INSERT INTO Companies (CompanyName, EmpId) VALUES (@CompanyName, @EmpId)";
                        foreach (var company in Companies)
                        {
                            using (SqlCommand companyCmd = new SqlCommand(insertCompanyQuery, connection, transaction))
                            {
                                companyCmd.Parameters.AddWithValue("@CompanyName", company);
                                companyCmd.Parameters.AddWithValue("@EmpId", empId);
                                companyCmd.ExecuteNonQuery();
                            }
                        }

                        // Insert into ProfileEMP table with the uploaded photo path
                        if (!string.IsNullOrEmpty(photoPath))
                        {
                            string insertPhotoQuery = "INSERT INTO ProfileEMP (EmployeeId, PhotoPath) VALUES (@EmployeeId, @PhotoPath)";
                            using (SqlCommand photoCmd = new SqlCommand(insertPhotoQuery, connection, transaction))
                            {
                                photoCmd.Parameters.AddWithValue("@EmployeeId", empId);
                                photoCmd.Parameters.AddWithValue("@PhotoPath", photoPath);
                                photoCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        // Optionally log the error or handle it as needed
                        throw;
                    }
                }
            }

            return RedirectToAction("AllUsers");
        }


        //[HttpPost]
        //public IActionResult AddUserDetails(UserDetails model, List<string> Companies)
        //{
        //    // Define your connection string
        //    string connectionString = _configuration.GetConnectionString("DefaultConnection");

        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {

        //        connection.Open();
        //        using (SqlTransaction transaction = connection.BeginTransaction())
        //        {
        //            try
        //            {
        //                string insertUserQuery = @"INSERT INTO Users (Username, Password, Role)
        //                                    OUTPUT INSERTED.Id
        //                                    VALUES (@Username, @Password, @Role)"; 
        //                int userId; 

        //                using (SqlCommand cmd = new SqlCommand(insertUserQuery, connection, transaction))
        //                {
        //                    cmd.Parameters.AddWithValue("@Username", model.UserName);
        //                    cmd.Parameters.AddWithValue("@Password", model.Password);
        //                    cmd.Parameters.AddWithValue("@Role", "User"); 

        //                    userId = (int)cmd.ExecuteScalar();
        //                }

        //                string insertEmployeeQuery = @"INSERT INTO Employee (FirstName, LastName, PhoneNumber, Address, Email, UserId)
        //                                        OUTPUT INSERTED.EmpId
        //                                        VALUES (@FirstName, @LastName, @PhoneNumber, @Address, @Email, @UserId)";

        //                int empId; 

        //                using (SqlCommand employeeCmd = new SqlCommand(insertEmployeeQuery, connection, transaction))
        //                {
        //                    employeeCmd.Parameters.AddWithValue("@FirstName", model.FirstName);
        //                    employeeCmd.Parameters.AddWithValue("@LastName", model.LastName);
        //                    employeeCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
        //                    employeeCmd.Parameters.AddWithValue("@Address", model.Address);
        //                    employeeCmd.Parameters.AddWithValue("@Email", model.Email);
        //                    employeeCmd.Parameters.AddWithValue("@UserId", userId); 
        //                    empId = (int)employeeCmd.ExecuteScalar();
        //                }


        //                string insertCompanyQuery = "INSERT INTO Companies (CompanyName, EmpId) VALUES (@CompanyName, @EmpId)";
        //                foreach (var company in Companies)
        //                {
        //                    using (SqlCommand companyCmd = new SqlCommand(insertCompanyQuery, connection, transaction))
        //                    {
        //                        companyCmd.Parameters.AddWithValue("@CompanyName", company);
        //                        companyCmd.Parameters.AddWithValue("@EmpId", empId); 
        //                        companyCmd.ExecuteNonQuery();
        //                    }
        //                }


        //                transaction.Commit();
        //            }
        //            catch (Exception ex)
        //            {

        //                transaction.Rollback();

        //                throw;
        //            }
        //        }
        //    }

        //    return RedirectToAction("AllUsers");
        //}


    }
}
