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

                    command = new SqlCommand("INSERT INTO Doctors (Matricule, PasswordHash, Salt) OUTPUT INSERTED.Id VALUES (@Matricule, @PasswordHash, @Salt)", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);

                    var id = (int)command.ExecuteScalar();

                    var doctor = new Doctor
                    {
                        Id = id,
                        Matricule = matricule,
                        PasswordHash = passwordHash,
                        Salt = salt
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
                                Salt = (string)reader["Salt"]
                            };

                            var passwordHash = HashPassword(password, doctor.Salt);
                            if (passwordHash == doctor.PasswordHash)
                            {
                                doctor.Token = GenerateToken();
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
                    UpdateToken(doctor);
                    auditService.LogInfo($"Doctor logged out: {doctor.Matricule} (ID: {doctor.Id})");
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Logout failed for doctor '{doctor.Matricule}'. Exception: {ex.Message}");
            }
        }

        public Doctor GetDoctorByToken(string token)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Doctors WHERE Token = @Token", connection);
                    command.Parameters.AddWithValue("@Token", token);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Doctor
                            {
                                Id = (int)reader["Id"],
                                Matricule = (string)reader["Matricule"],
                                PasswordHash = (string)reader["PasswordHash"],
                                Salt = (string)reader["Salt"],
                                Token = (string)reader["Token"]
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetDoctorByToken failed for token '{token}'. Exception: {ex.Message}");
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
                                Token = reader["Token"] as string
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
                    var command = new SqlCommand("UPDATE Doctors SET Token = @Token WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Token", (object)doctor.Token ?? DBNull.Value);
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

        private string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}
