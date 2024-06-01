using System;

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
    }
}
