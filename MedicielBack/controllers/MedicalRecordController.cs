using MedicielBack.models;
using MedicielBack.services;
using System;
using System.Collections.Generic;

namespace MedicielBack.controllers
{
    public class MedicalRecordController
    {
        private readonly MedicalRecordService recordService;
        private readonly DoctorService doctorService;

        public MedicalRecordController(AuditService auditService, DoctorService doctorService)
        {
            this.recordService = new MedicalRecordService(auditService);
            this.doctorService = doctorService;
        }

        public MedicalRecord CreateRecord(string token, string patientName, string diagnosis, string treatment)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }
            return recordService.CreateRecord(doctor.Id, patientName, diagnosis, treatment);
        }

        public MedicalRecord UpdateRecord(string token, int recordId, string diagnosis, string treatment)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }
            return recordService.UpdateRecord(doctor.Id, recordId, diagnosis, treatment);
        }

        public List<MedicalRecord> GetRecords(string token)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }
            return recordService.GetRecordsByDoctor(doctor.Id);
        }

        public MedicalRecord GetRecord(string token, int recordId)
        {
            var doctor = doctorService.GetDoctorByToken(token);
            if (doctor == null)
            {
                return null;
            }
            return recordService.GetRecordById(doctor.Id, recordId);
        }

        public List<MedicalRecord> GetAllMedicalRecords()
        {
            return recordService.GetAllMedicalRecords();
        }
    }
}
