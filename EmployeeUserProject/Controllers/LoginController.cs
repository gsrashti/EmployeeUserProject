using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeProject.Models;
using Microsoft.AspNetCore.Authentication;

using System.Data;
using Microsoft.AspNetCore.Http;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Drawing.Imaging;
using System.Drawing;
using EmployeeUserProject.Models;

public class LoginController : Controller
{
    private readonly string _connectionString;

    public LoginController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet]
    public IActionResult SignIn()
    {
        return View();
    }
    [HttpPost]
    public IActionResult SignIn(SignInModel model)
    {
        if (!ModelState.IsValid)
        {

            return View(model);
        }


        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string checkUserQuery = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
            SqlCommand checkUserCmd = new SqlCommand(checkUserQuery, conn);
            checkUserCmd.Parameters.AddWithValue("@Username", model.Username);

            int userExists = (int)checkUserCmd.ExecuteScalar();

            if (userExists > 0)
            {
                ModelState.AddModelError("", "User already exists with this username.");
                return View(model);
            }

            string insertUserQuery = @"
            INSERT INTO Users (Username, Password, Role) 
            VALUES (@Username, @Password, @Role)";

            SqlCommand insertUserCmd = new SqlCommand(insertUserQuery, conn);
            insertUserCmd.Parameters.AddWithValue("@Username", model.Username);
            insertUserCmd.Parameters.AddWithValue("@Password", model.Password);
            insertUserCmd.Parameters.AddWithValue("@Role", model.Role);

            insertUserCmd.ExecuteNonQuery();
            ViewData["Success"] = true;
            return View();

        }
        ModelState.AddModelError("", "An error occurred during registration.");
        return View(model);
    }


    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");

        if (sessionCaptcha == null || !sessionCaptcha.Trim().Equals(model.UserCaptchaInput.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Invalid CAPTCHA code.");
            return View(model);
        }

        if (ModelState.IsValid && IsValidUser(model.Username, model.Password, model.Role, out int userId, out int empId))
        {
            model.Id = userId;
            HttpContext.Session.SetString("UserRole", model.Role);
            HttpContext.Session.SetString("UserId", Convert.ToString(model.Id));
            HttpContext.Session.SetString("EmpId", Convert.ToString(empId));

            // Create claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, model.Username),
        new Claim(ClaimTypes.Role, model.Role),
        new Claim("Id", model.Id.ToString())
    };

            // Create identity
            var claimsIdentity = new ClaimsIdentity(claims, "Login");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddMinutes(20)
            };
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal, authProperties);
            if (model.RememberMe)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = false
                };

                Response.Cookies.Append("Username", model.Username, cookieOptions);
                Response.Cookies.Append("Password", model.Password, cookieOptions);
            }
            else
            {
                Response.Cookies.Delete("Username");
                Response.Cookies.Delete("Password");
            }
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
    private string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var code = new char[length];

        for (int i = 0; i < length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }

    public IActionResult GenerateCaptcha()
    {
        var randomCode = GenerateRandomCode();
        HttpContext.Session.SetString("CaptchaCode", randomCode);

        using var bitmap = new Bitmap(200, 60);
        using var graphics = Graphics.FromImage(bitmap);
        Color backgroundColor = Color.FromArgb(240, 240, 240);
        graphics.Clear(backgroundColor);
        Random random = new Random();
        using (var pen = new Pen(Color.LightGray, 2))
        {
            for (int i = 0; i < 10; i++)
            {
                graphics.DrawLine(pen, 0, random.Next(0, 100), 200, random.Next(0, 100));
            }
        }

        var font = new System.Drawing.Font("Arial", 20, FontStyle.Bold);
        float xPos = 20;

        foreach (char c in randomCode)
        {
            graphics.DrawString(c.ToString(), font, Brushes.Black, new PointF(xPos, 30));
            xPos += 25;
        }
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return File(memoryStream.ToArray(), "image/png");
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
