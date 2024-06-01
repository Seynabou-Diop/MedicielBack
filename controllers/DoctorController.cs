using MedicielBack.models;
using MedicielBack.services;
using System;

namespace MedicielBack.controllers
{
    public class DoctorController
    {
        private readonly DoctorService doctorService;
        private readonly AuditService auditService;

        public DoctorController(AuditService auditService)
        {
            this.auditService = auditService;
            doctorService = new DoctorService(auditService);
        }

        public Doctor Login(string matricule, string password)
        {
            var doctor = doctorService.Authenticate(matricule, password);
            if (doctor != null)
            {
                auditService.LogInfo($"Doctor logged in successfully: {matricule}");
            }
            else
            {
                auditService.LogWarning($"Failed to login doctor: {matricule}");
            }
            return doctor;
        }

        public void Logout(Doctor doctor)
        {
            if (doctor != null)
            {
                doctorService.Logout(doctor);
                auditService.LogInfo($"Doctor logged out: {doctor.Matricule}");
            }
            else
            {
                auditService.LogWarning($"Failed to logout doctor: Invalid token");
            }
        }

        public Doctor GetDoctorByToken(string token)
        {
            return doctorService.GetDoctorByToken(token);
        }

        public List<Doctor> GetAllDoctors()
        {
            return doctorService.GetAllDoctors();
        }
    }
}
