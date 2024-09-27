namespace EmployeeProject.Models
{
    public class EmployeeDetails
    {
        public int EmpId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string ProfilePhotoPath { get; set; }
        public List<Company> Companies { get; set; } 
    }
    public class Company
    {
        public int CompanyId { get; set; } 
        public string CompanyName { get; set; }
    }
}
