using MedicielBack.models;
using MedicielBack.services;
using System;

namespace MedicielBack.controllers
{
    public class AdminController
    {
        private readonly AdminService adminService;
        private readonly DoctorService doctorService;
        private readonly AuditService auditService;

        public AdminController(AuditService auditService)
        {
            this.auditService = auditService;
            adminService = new AdminService(auditService);
            doctorService = new DoctorService(auditService);
        }

        public Admin RegisterAdmin(string username, string password)
        {
            var admin = adminService.Register(username, password);
            if (admin != null)
            {
                auditService.LogInfo($"Admin registered successfully: {username}");
            }
            else
            {
                auditService.LogError($"Failed to register admin: {username}");
            }
            return admin;
        }

        public Admin LoginAdmin(string username, string password)
        {
            var admin = adminService.Authenticate(username, password);
            if (admin != null)
            {
                auditService.LogInfo($"Admin logged in successfully: {username}");
            }
            else
            {
                auditService.LogWarning($"Failed to login admin: {username}");
            }
            return admin;
        }

        public void LogoutAdmin(string token)
        {
            var admin = adminService.GetAdminByToken(token);
            if (admin != null)
            {
                adminService.Logout(admin);
                auditService.LogInfo($"Admin logged out: {admin.Username}");
            }
            else
            {
                auditService.LogWarning($"Failed to logout admin: Invalid token");
            }
        }

        public Admin GetAdminByToken(string token)
        {
            return adminService.GetAdminByToken(token);
        }

        public List<Admin> GetAllAdmins()
        {
            return adminService.GetAllAdmins();
        }

        public Doctor RegisterDoctor(string token, string matricule, string password)
        {
            var admin = adminService.GetAdminByToken(token);
            if (admin == null)
            {
                auditService.LogWarning($"Failed to register doctor: Invalid admin token");
                return null;
            }

            var doctor = doctorService.Register(matricule, password);
            if (doctor != null)
            {
                auditService.LogInfo($"Doctor registered successfully: {matricule}");
            }
            else
            {
                auditService.LogError($"Failed to register doctor: {matricule}");
            }
            return doctor;
        }
    }
}
