namespace MedicielBack.models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string Matricule { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public Token Token { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Specialty { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string Qualifications { get; set; }
        public int YearsOfExperience { get; set; }
        public string Role { get; set; } = "Doctor";  // Default role for Doctor
    }
}
