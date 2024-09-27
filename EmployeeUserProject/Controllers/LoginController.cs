using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeProject.Models;
using Microsoft.AspNetCore.Authentication;

using System.Data;
using Microsoft.AspNetCore.Http;
using System.Data.SqlClient;

public class LoginController : Controller
{
    private readonly string _connectionString;

    public LoginController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        if (ModelState.IsValid && IsValidUser(model.Username, model.Password, model.Role, out int userId, out int empId))
        {
            model.Id = userId;
            HttpContext.Session.SetString("UserRole", model.Role);
            HttpContext.Session.SetString("UserId", Convert.ToString(model.Id));
            HttpContext.Session.SetString("EmpId", Convert.ToString(empId));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, model.Role),
                new Claim("Id", model.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Login");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            HttpContext.SignInAsync(claimsPrincipal).Wait();
            return model.Role == "Admin" ? RedirectToAction("AllUsers", "Admin") : RedirectToAction("MyProfile", "User");
        }

        ModelState.AddModelError("", "Invalid username or password.");
        return View("Index", model);
    }

    private bool IsValidUser(string username, string password, string role, out int userId, out int empId)
    {
        userId = 0;
        empId = 0;
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            var command = new SqlCommand(@"
            SELECT u.Id, e.EmpId 
            FROM Users u 
            INNER JOIN Employee e ON u.Id = e.UserId 
            WHERE u.Username = @Username 
            AND u.Password = @Password 
            AND u.Role = @Role", connection);

            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);
            command.Parameters.AddWithValue("@Role", role);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    userId = reader.GetInt32(0); 
                    empId = reader.GetInt32(1);   
                    return true;                  
                }
            }
        }

        return false; 
    }

    [HttpPost]
    public IActionResult Logout()
    {
        try
        {            
            HttpContext.Session.Clear();
             HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Login");
        }
        catch (Exception ex)
        {
            return RedirectToAction("Error", "Home");
        }
    }

    //private bool IsValidUser(string username, string password, string role, out int userId)
    //{
    //    userId = 0; 
    //    using (SqlConnection connection = new SqlConnection(_connectionString))
    //    {
    //        connection.Open();
    //        string query = "SELECT Id FROM Users WHERE Username = @Username AND Password = @Password AND Role = @Role";

    //        using (SqlCommand command = new SqlCommand(query, connection))
    //        {
    //            command.Parameters.AddWithValue("@Username", username);
    //            command.Parameters.AddWithValue("@Password", password); 
    //            command.Parameters.AddWithValue("@Role", role);
    //            var result = command.ExecuteScalar();
    //            if (result != null)
    //            {
    //                userId = Convert.ToInt32(result); 
    //                return true; 
    //            }
    //        }
    //    }

    //    return false; 
    //}
}
