using MedicielBack.models;
using MedicielBack.services;
using System;
using System.Collections.Generic;

namespace MedicielBack.controllers
{
    public class DoctorController
    {
        private readonly DoctorService doctorService;
        private readonly MedicalRecordService recordService;

        public DoctorController(AuditService auditService)
        {
            doctorService = new DoctorService(auditService);
            recordService = new MedicalRecordService(auditService);
        }

        public Doctor Register(string matricule, string password)
        {
            return doctorService.Register(matricule, password);
        }

        public Doctor Login(string matricule, string password)
        {
            return doctorService.Authenticate(matricule, password);
        }

        public void Logout(Doctor doctor)
        {
            doctorService.Logout(doctor);
        }

        public Doctor GetDoctorByToken(string token)
        {
            return doctorService.GetDoctorByToken(token);
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

        public List<Doctor> GetAllDoctors()
        {
            return doctorService.GetAllDoctors();
        }
    }
}
