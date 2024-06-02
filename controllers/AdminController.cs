using MedicielBack.models;
using MedicielBack.services;
using System;
using System.Collections.Generic;

namespace MedicielBack.controllers
{
    public class AdminController
    {
        private readonly AdminService adminService;
        private readonly DoctorService doctorService;

        public AdminController(AuditService auditService, TokenService tokenService, EncryptionService encryptionService)
        {
            adminService = new AdminService(auditService, tokenService);
            doctorService = new DoctorService(auditService, tokenService, encryptionService);
        }

        public Admin RegisterAdmin(string username, string password)
        {
            return adminService.Register(username, password);
        }

        public Admin LoginAdmin(string username, string password)
        {
            return adminService.Authenticate(username, password);
        }

        public void LogoutAdmin(Admin admin)
        {
            adminService.Logout(admin);
        }

        public Admin GetAdminByToken(string token)
        {
            return adminService.GetAdminByToken(token);
        }

        public List<Admin> GetAllAdmins()
        {
            return adminService.GetAllAdmins();
        }

        public Doctor RegisterDoctor(string token, string matricule, string password, string phone, DateTime dateOfBirth, string specialty, string department, string email, string address, string gender, string qualifications, int yearsOfExperience)
        {
            var role = adminService.GetUserRole(token);
            if (role != "Admin")
            {
                return null; // Only admins can register doctors
            }

            return doctorService.Register(matricule, password, phone, dateOfBirth, specialty, department, email, address, gender, qualifications, yearsOfExperience);
        }

        public Doctor UpdateDoctor(string token, int doctorId, string phone, DateTime dateOfBirth, string specialty, string department, string email, string address, string gender, string qualifications, int yearsOfExperience)
        {
            var role = adminService.GetUserRole(token);
            if (role != "Admin")
            {
                return null; // Only admins can update doctors
            }

            return doctorService.UpdateDoctor(doctorId, phone, dateOfBirth, specialty, department, email, address, gender, qualifications, yearsOfExperience);
        }

        public bool DeleteDoctor(string token, int doctorId)
        {
            var role = adminService.GetUserRole(token);
            if (role != "Admin")
            {
                return false; // Only admins can delete doctors
            }

            return doctorService.DeleteDoctor(doctorId);
        }
    }
}
