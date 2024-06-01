using System;

namespace MedicielBack.models
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string PatientName { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
    }
}
