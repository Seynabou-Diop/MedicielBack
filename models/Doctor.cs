
namespace MedicielBack.models
{

    public class Doctor
    { 
        public int Id { get; set; }
        public string Matricule { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string Token { get; set; }
    }
}