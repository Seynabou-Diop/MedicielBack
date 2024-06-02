using MedicielBack.models;
using MedicielBack.services;
using System.Collections.Generic;

namespace MedicielBack.controllers
{
    public class DoctorController
    {
        private readonly DoctorService doctorService;

        public DoctorController(AuditService auditService, TokenService tokenService, EncryptionService encryptionService)
        {
            doctorService = new DoctorService(auditService, tokenService, encryptionService);
        }

        public Doctor Register(string matricule, string password, string phone, DateTime dateOfBirth, string specialty, string department, string email, string address, string gender, string qualifications, int yearsOfExperience)
        {
            return doctorService.Register(matricule, password, phone, dateOfBirth, specialty, department, email, address, gender, qualifications, yearsOfExperience);
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

        public Doctor GetDoctorById(int doctorId, string token)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Admin" && role != "Doctor")
            {
                return null; // Only admins and doctors can view doctor details
            }

            return doctorService.GetDoctorById(doctorId);
        }

        public List<Doctor> GetDoctorsBySpecialty(string specialty, string token)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Admin" && role != "Doctor")
            {
                return null; // Only admins and doctors can view doctor details
            }

            return doctorService.GetDoctorsBySpecialty(specialty);
        }

        public List<Doctor> GetDoctorsByDepartment(string department, string token)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Admin" && role != "Doctor")
            {
                return null; // Only admins and doctors can view doctor details
            }

            return doctorService.GetDoctorsByDepartment(department);
        }

        public List<Doctor> GetAllDoctors(string token)
        {
            var role = doctorService.GetUserRole(token);
            if (role != "Admin")
            {
                return null; // Only admins can view all doctors
            }

            return doctorService.GetAllDoctors();
        }
    }
}
