﻿namespace EmployeeProject.Models
{
    public class LoginViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public bool RememberMe { get; set; }  
        
    public string UserCaptchaInput { get; set; } 
    }
}
