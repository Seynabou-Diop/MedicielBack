using MedicielBack.models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace MedicielBack.services
{
    public class DoctorService
    {
        private readonly string connectionString;
        private readonly AuditService auditService;

        public DoctorService(AuditService auditService)
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this.auditService = auditService;
        }

        public Doctor Register(string matricule, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT COUNT(*) FROM Doctors WHERE Matricule = @Matricule", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);
                    var count = (int)command.ExecuteScalar();

                    if (count > 0)
                    {
                        auditService.LogError($"Registration failed: Matricule '{matricule}' already exists.");
                        return null;
                    }

                    var salt = GenerateSalt();
                    var passwordHash = HashPassword(password, salt);

                    command = new SqlCommand("INSERT INTO Doctors (Matricule, PasswordHash, Salt, CreationDate, ModificationDate) OUTPUT INSERTED.Id VALUES (@Matricule, @PasswordHash, @Salt, @CreationDate, @ModificationDate)", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);

                    var id = (int)command.ExecuteScalar();

                    var doctor = new Doctor
                    {
                        Id = id,
                        Matricule = matricule,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        CreationDate = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow
                    };

                    auditService.LogInfo($"Doctor registered: {matricule} (ID: {doctor.Id})");
                    return doctor;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Registration failed for matricule '{matricule}'. Exception: {ex.Message}");
                return null;
            }
        }

        public Doctor Authenticate(string matricule, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Doctors WHERE Matricule = @Matricule", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var doctor = new Doctor
                            {
                                Id = (int)reader["Id"],
                                Matricule = (string)reader["Matricule"],
                                PasswordHash = (string)reader["PasswordHash"],
                                Salt = (string)reader["Salt"],
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            };

                            var passwordHash = HashPassword(password, doctor.Salt);
                            if (passwordHash == doctor.PasswordHash)
                            {
                                var token = GenerateToken();
                                doctor.Token = token;
                                doctor.ModificationDate = DateTime.UtcNow;
                                UpdateToken(doctor);
                                auditService.LogInfo($"Doctor logged in: {matricule} (ID: {doctor.Id})");
                                return doctor;
                            }
                        }
                    }

                    auditService.LogWarning($"Authentication failed: Invalid matricule or password for matricule '{matricule}'.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Authentication failed for matricule '{matricule}'. Exception: {ex.Message}");
                return null;
            }
        }

        public void Logout(Doctor doctor)
        {
            try
            {
                if (doctor != null)
                {
                    doctor.Token = null;
                    doctor.ModificationDate = DateTime.UtcNow;
                    UpdateToken(doctor);
                    auditService.LogInfo($"Doctor logged out: {doctor.Matricule} (ID: {doctor.Id})");
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Logout failed for doctor '{doctor.Matricule}'. Exception: {ex.Message}");
            }
        }

        public Doctor GetDoctorByToken(string tokenValue)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Doctors WHERE Token = @Token", connection);
                    command.Parameters.AddWithValue("@Token", tokenValue);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var doctor = new Doctor
                            {
                                Id = (int)reader["Id"],
                                Matricule = (string)reader["Matricule"],
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

                            if (doctor.Token.ExpirationDate > DateTime.UtcNow)
                            {
                                return doctor;
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetDoctorByToken failed for token '{tokenValue}'. Exception: {ex.Message}");
                return null;
            }
        }

        public List<Doctor> GetAllDoctors()
        {
            var doctors = new List<Doctor>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Doctors", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctors.Add(new Doctor
                            {
                                Id = (int)reader["Id"],
                                Matricule = (string)reader["Matricule"],
                                PasswordHash = (string)reader["PasswordHash"],
                                Salt = (string)reader["Salt"],
                                Token = reader["Token"] as Token,
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            });
                        }
                    }
                }
                auditService.LogInfo("Retrieved all doctors.");
                return doctors;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetAllDoctors failed. Exception: {ex.Message}");
                return doctors;
            }
        }

        private void UpdateToken(Doctor doctor)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("UPDATE Doctors SET Token = @Token, TokenExpiration = @TokenExpiration, ModificationDate = @ModificationDate WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Token", (object)doctor.Token?.Value ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TokenExpiration", (object)doctor.Token?.ExpirationDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ModificationDate", doctor.ModificationDate);
                    command.Parameters.AddWithValue("@Id", doctor.Id);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"UpdateToken failed for doctor '{doctor.Matricule}'. Exception: {ex.Message}");
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
