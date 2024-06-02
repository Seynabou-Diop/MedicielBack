using MedicielBack.models;
using MedicielBack.services;
using System.Collections.Generic;

namespace MedicielBack.controllers
{
    public class MedicalRecordController
    {
        private readonly MedicalRecordService recordService;
        private readonly DoctorService doctorService;

        public MedicalRecordController(AuditService auditService, DoctorService doctorService, EncryptionService encryptionService)
        {
            this.recordService = new MedicalRecordService(auditService, encryptionService);
            this.doctorService = doctorService;
        }

        public MedicalRecord CreateRecord(string token, string patientName, string diagnosis, string treatment, string patientPhone, DateTime patientDateOfBirth, string patientAddress, string emergencyContactName, string emergencyContactPhone, string insuranceProvider, string policyNumber, string allergies, string medications, string previousConditions, string notes)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Doctor")
            {
                return null; // Only doctors can create medical records
            }

            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }

            return recordService.CreateRecord(doctor.Id, patientName, diagnosis, treatment, patientPhone, patientDateOfBirth, patientAddress, emergencyContactName, emergencyContactPhone, insuranceProvider, policyNumber, allergies, medications, previousConditions, notes);
        }

        public MedicalRecord UpdateRecord(string token, int recordId, string diagnosis, string treatment, string patientPhone, DateTime patientDateOfBirth, string patientAddress, string emergencyContactName, string emergencyContactPhone, string insuranceProvider, string policyNumber, string allergies, string medications, string previousConditions, string notes)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Doctor")
            {
                return null; // Only doctors can update medical records
            }

            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }

            return recordService.UpdateRecord(doctor.Id, recordId, diagnosis, treatment, patientPhone, patientDateOfBirth, patientAddress, emergencyContactName, emergencyContactPhone, insuranceProvider, policyNumber, allergies, medications, previousConditions, notes);
        }

        public List<MedicalRecord> GetRecords(string token)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Doctor" && role != "Admin")
            {
                return null; // Only doctors and admins can view records
            }

            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }

            return recordService.GetRecordsByDoctor(doctor.Id);
        }

        public MedicalRecord GetRecord(string token, int recordId)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Doctor" && role != "Admin")
            {
                return null; // Only doctors and admins can view records
            }

            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }

            return recordService.GetRecordById(doctor.Id, recordId);
        }

        public List<MedicalRecord> GetAllMedicalRecords(string token)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Admin")
            {
                return null; // Only admins can view all records
            }

            return recordService.GetAllMedicalRecords();
        }
    }
}
