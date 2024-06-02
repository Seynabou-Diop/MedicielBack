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
        public string PatientPhone { get; set; }
        public DateTime PatientDateOfBirth { get; set; }
        public string PatientAddress { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string InsuranceProvider { get; set; }
        public string PolicyNumber { get; set; }
        public string Allergies { get; set; }
        public string Medications { get; set; }
        public string PreviousConditions { get; set; }
        public string Notes { get; set; }
    }
}
