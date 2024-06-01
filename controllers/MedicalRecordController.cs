using MedicielBack.models;
using MedicielBack.services;
using System;

namespace MedicielBack.controllers
{
    public class MedicalRecordController
    {
        private readonly MedicalRecordService recordService;
        private readonly DoctorService doctorService;
        private readonly AuditService auditService;

        public MedicalRecordController(AuditService auditService, DoctorService doctorService)
        {
            this.auditService = auditService;
            this.doctorService = doctorService;
            recordService = new MedicalRecordService(auditService);
        }

        public MedicalRecord CreateRecord(string token, string patientName, string diagnosis, string treatment)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                auditService.LogWarning($"Failed to create record: Invalid doctor token");
                return null;
            }

            var record = recordService.CreateRecord(doctor.Id, patientName, diagnosis, treatment);
            if (record != null)
            {
                auditService.LogInfo($"Record created successfully for doctor: {doctor.Matricule}");
            }
            else
            {
                auditService.LogError($"Failed to create record for doctor: {doctor.Matricule}");
            }
            return record;
        }

        public MedicalRecord UpdateRecord(string token, int recordId, string diagnosis, string treatment)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                auditService.LogWarning($"Failed to update record: Invalid doctor token");
                return null;
            }

            var record = recordService.UpdateRecord(doctor.Id, recordId, diagnosis, treatment);
            if (record != null)
            {
                auditService.LogInfo($"Record updated successfully for doctor: {doctor.Matricule}");
            }
            else
            {
                auditService.LogError($"Failed to update record for doctor: {doctor.Matricule}");
            }
            return record;
        }

        public List<MedicalRecord> GetRecords(string token)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                auditService.LogWarning($"Failed to get records: Invalid doctor token");
                return null;
            }

            var records = recordService.GetRecordsByDoctor(doctor.Id);
            auditService.LogInfo($"Records retrieved for doctor: {doctor.Matricule}");
            return records;
        }

        public MedicalRecord GetRecord(string token, int recordId)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                auditService.LogWarning($"Failed to get record: Invalid doctor token");
                return null;
            }

            var record = recordService.GetRecordById(doctor.Id, recordId);
            if (record != null)
            {
                auditService.LogInfo($"Record retrieved for doctor: {doctor.Matricule}");
            }
            else
            {
                auditService.LogWarning($"Failed to retrieve record for doctor: {doctor.Matricule}");
            }
            return record;
        }

        public List<MedicalRecord> GetAllMedicalRecords()
        {
            var records = recordService.GetAllMedicalRecords();
            auditService.LogInfo("All medical records retrieved.");
            return records;
        }
    }
}
