﻿namespace EmployeeProject.Models
{
    public class UserDetails
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
       
        public List<string> Companies { get; set; }
    }
}
