﻿using MedicielBack.models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace MedicielBack.services
{
    public class AdminService
    {
        private readonly string connectionString;
        private readonly AuditService auditService;

        public AdminService(AuditService auditService)
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this.auditService = auditService;
        }

        public Admin Register(string username, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT COUNT(*) FROM Admins WHERE Username = @Username", connection);
                    command.Parameters.AddWithValue("@Username", username);
                    var count = (int)command.ExecuteScalar();

                    if (count > 0)
                    {
                        auditService.LogError($"Registration failed: Username '{username}' already exists.");
                        return null;
                    }

                    var salt = GenerateSalt();
                    var passwordHash = HashPassword(password, salt);

                    command = new SqlCommand("INSERT INTO Admins (Username, PasswordHash, Salt, CreationDate, ModificationDate) OUTPUT INSERTED.Id VALUES (@Username, @PasswordHash, @Salt, @CreationDate, @ModificationDate)", connection);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);

                    var id = (int)command.ExecuteScalar();

                    var admin = new Admin
                    {
                        Id = id,
                        Username = username,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        CreationDate = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow
                    };

                    auditService.LogInfo($"Admin registered: {username} (ID: {admin.Id})");
                    return admin;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Registration failed for username '{username}'. Exception: {ex.Message}");
                return null;
            }
        }

        public Admin Authenticate(string username, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Admins WHERE Username = @Username", connection);
                    command.Parameters.AddWithValue("@Username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var admin = new Admin
                            {
                                Id = (int)reader["Id"],
                                Username = (string)reader["Username"],
                                PasswordHash = (string)reader["PasswordHash"],
                                Salt = (string)reader["Salt"],
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            };

                            var passwordHash = HashPassword(password, admin.Salt);
                            if (passwordHash == admin.PasswordHash)
                            {
                                admin.Token = GenerateToken();
                                admin.ModificationDate = DateTime.UtcNow;
                                UpdateToken(admin);
                                auditService.LogInfo($"Admin logged in: {username} (ID: {admin.Id})");
                                return admin;
                            }
                        }
                    }

                    auditService.LogWarning($"Authentication failed: Invalid username or password for username '{username}'.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Authentication failed for username '{username}'. Exception: {ex.Message}");
                return null;
            }
        }

        public void Logout(Admin admin)
        {
            try
            {
                if (admin != null)
                {
                    admin.Token = null;
                    admin.ModificationDate = DateTime.UtcNow;
                    UpdateToken(admin);
                    auditService.LogInfo($"Admin logged out: {admin.Username} (ID: {admin.Id})");
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Logout failed for admin '{admin.Username}'. Exception: {ex.Message}");
            }
        }

        public Admin GetAdminByToken(string tokenValue)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Admins WHERE Token = @Token", connection);
                    command.Parameters.AddWithValue("@Token", tokenValue);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var admin = new Admin
                            {
                                Id = (int)reader["Id"],
                                Username = (string)reader["Username"],
                                PasswordHash = (string)reader["PasswordHash"],
                                Salt = (string)reader["Salt"],
                                Token = new Token
                                {
                                    Value = (string)reader["Token"],
                                    ExpirationDate = (DateTime)reader["TokenExpiration"]
                                },
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            };

                            if (admin.Token.ExpirationDate > DateTime.UtcNow)
                            {
                                return admin;
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetAdminByToken failed for token '{tokenValue}'. Exception: {ex.Message}");
                return null;
            }
        }

        public List<Admin> GetAllAdmins()
        {
            var admins = new List<Admin>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Admins", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            admins.Add(new Admin
                            {
                                Id = (int)reader["Id"],
                                Username = (string)reader["Username"],
                                PasswordHash = (string)reader["PasswordHash"],
                                Salt = (string)reader["Salt"],
                                Token = reader["Token"] as Token,
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            });
                        }
                    }
                }
                auditService.LogInfo("Retrieved all admins.");
                return admins;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetAllAdmins failed. Exception: {ex.Message}");
                return admins;
            }
        }

        private void UpdateToken(Admin admin)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("UPDATE Admins SET Token = @Token, TokenExpiration = @TokenExpiration, ModificationDate = @ModificationDate WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Token", (object)admin.Token?.Value ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TokenExpiration", (object)admin.Token?.ExpirationDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ModificationDate", admin.ModificationDate);
                    command.Parameters.AddWithValue("@Id", admin.Id);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"UpdateToken failed for admin '{admin.Username}'. Exception: {ex.Message}");
            }
        }

        private string GenerateSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var buffer = new byte[16];
                rng.GetBytes(buffer);
                return Convert.ToBase64String(buffer);
            }
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = string.Concat(password, salt);
                var saltedPasswordAsBytes = Encoding.UTF8.GetBytes(saltedPassword);
                return Convert.ToBase64String(sha256.ComputeHash(saltedPasswordAsBytes));
            }
        }

        private Token GenerateToken()
        {
            return new Token
            {
                Value = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpirationDate = DateTime.UtcNow.AddHours(1),
                RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            };
        }
    }
}
