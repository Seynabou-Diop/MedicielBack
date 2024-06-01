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

        public AdminController(AuditService auditService)
        {
            adminService = new AdminService(auditService);
            doctorService = new DoctorService(auditService);
        }

        public Admin RegisterAdmin(string username, string password)
        {
            return adminService.Register(username, password);
        }

        public Admin LoginAdmin(string username, string password)
        {
            return adminService.Authenticate(username, password);
        }

        public void LogoutAdmin(string token)
        {
            var admin = adminService.GetAdminByToken(token);
            if (admin != null)
            {
                adminService.Logout(admin);
            }
        }

        public Admin GetAdminByToken(string token)
        {
            return adminService.GetAdminByToken(token);
        }

        public Doctor RegisterDoctor(string token, string matricule, string password)
        {
            var admin = adminService.GetAdminByToken(token);
            if (admin == null)
            {
                return null;
            }
            return doctorService.Register(matricule, password);
        }

        public List<Admin> GetAllAdmins()
        {
            return adminService.GetAllAdmins();
        }
    }
}
