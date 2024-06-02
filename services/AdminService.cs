using MedicielBack.models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MedicielBack.services
{
    public class AdminService
    {
        private readonly string connectionString;
        private readonly AuditService auditService;
        private readonly TokenService tokenService;

        public AdminService(AuditService auditService, TokenService tokenService)
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this.auditService = auditService;
            this.tokenService = tokenService;
        }
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
                    var command = new SqlCommand("SELECT COUNT(*) FROM admin WHERE Username = @Username", connection);
                    command.Parameters.AddWithValue("@Username", username);
                    var count = (int)command.ExecuteScalar();

                    if (count > 0)
                    {
                        auditService.LogError($"Registration failed: Username '{username}' already exists.");
                        return null;
                    }

                    var salt = GenerateSalt();
                    var passwordHash = HashPassword(password, salt);

                    command = new SqlCommand("INSERT INTO admin (username, password_hash, salt, creation_date, modification_date) OUTPUT INSERTED.id VALUES (@Username, @PasswordHash, @Salt, @CreationDate, @ModificationDate)", connection);
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

                    auditService.LogInfo($"Admin registered successfully: {username} (ID: {admin.Id})");
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM admins WHERE username = @Username", connection);
                command.Parameters.AddWithValue("@Username", username);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var admin = new Admin
                        {
                            Id = (int)reader["id"],
                            Username = (string)reader["username"],
                            PasswordHash = (string)reader["password_hash"],
                            Salt = (string)reader["salt"]
                        };

                        var passwordHash = HashPassword(password, admin.Salt);
                        if (passwordHash == admin.PasswordHash)
                        {
                            var token = tokenService.GenerateToken(admin.Id, "Admin");
                            admin.Token = new Token
                            {
                                Value = token,
                                ExpirationDate = DateTime.UtcNow.AddHours(1)
                            };
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

        public string GetUserRole(string token)
        {
            var claimsPrincipal = tokenService.ValidateToken(token);
            if (claimsPrincipal == null)
            {
                return null;
            }

            var roleClaim = claimsPrincipal.FindFirst(ClaimTypes.Role);
            return roleClaim?.Value;
        }

        public void Logout(Admin admin)
        {
            if (admin != null)
            {
                admin.Token = null;
                UpdateToken(admin);
                auditService.LogInfo($"Admin logged out: {admin.Username} (ID: {admin.Id})");
            }
        }


        public Admin GetAdminByToken(string tokenValue)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM admin WHERE token = @Token", connection);
                    command.Parameters.AddWithValue("@Token", tokenValue);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var admin = new Admin
                            {
                                Id = (int)reader["id"],
                                Username = (string)reader["username"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                Token = new Token
                                {
                                    Value = (string)reader["token"],
                                    ExpirationDate = (DateTime)reader["token_expiration"]
                                },
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"]
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
                    var command = new SqlCommand("SELECT * FROM admin", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            admins.Add(new Admin
                            {
                                Id = (int)reader["id"],
                                Username = (string)reader["username"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                Token = reader["token"] as Token,
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"]
                            });
                        }
                    }
                }
                auditService.LogInfo("Retrieved successfully all admins.");
                return admins;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetAllAdmins failed. Exception: {ex.Message}");
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
                    var command = new SqlCommand("UPDATE admin SET token = @Token, token_expiration = @TokenExpiration, modification_date = @ModificationDate WHERE id = @Id", connection);
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
