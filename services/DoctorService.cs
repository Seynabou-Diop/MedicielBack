using MedicielBack.models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MedicielBack.services
{
    public class DoctorService
    {
        private readonly string connectionString;
        private readonly AuditService auditService;
        private readonly TokenService tokenService;
        private readonly EncryptionService encryptionService;

        public DoctorService(AuditService auditService, TokenService tokenService, EncryptionService encryptionService)
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this.auditService = auditService;
            this.tokenService = tokenService;
            this.encryptionService = encryptionService;
        }

        public Doctor Register(string matricule, string password, string phone, DateTime dateOfBirth, string specialty, string department, string email, string address, string gender, string qualifications, int yearsOfExperience)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT COUNT(*) FROM doctor WHERE matricule = @Matricule", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);
                    var count = (int)command.ExecuteScalar();

                    if (count > 0)
                    {
                        auditService.LogError($"Registration failed: Matricule '{matricule}' already exists.");
                        return null;
                    }

                    var salt = GenerateSalt();
                    var passwordHash = HashPassword(password, salt);

                    command = new SqlCommand("INSERT INTO doctor (matricule, password_hash, salt, creation_date, modification_date, phone, date_of_birth, specialty, department, email, address, gender, qualifications, years_of_experience) OUTPUT INSERTED.id VALUES (@Matricule, @PasswordHash, @Salt, @CreationDate, @ModificationDate, @Phone, @DateOfBirth, @Specialty, @Department, @Email, @Address, @Gender, @Qualifications, @YearsOfExperience)", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@Specialty", specialty);
                    command.Parameters.AddWithValue("@Department", department);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@Qualifications", qualifications);
                    command.Parameters.AddWithValue("@YearsOfExperience", yearsOfExperience);

                    var id = (int)command.ExecuteScalar();

                    var doctor = new Doctor
                    {
                        Id = id,
                        Matricule = matricule,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        CreationDate = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow,
                        Phone = phone,
                        DateOfBirth = dateOfBirth,
                        Specialty = specialty,
                        Department = department,
                        Email = email,
                        Address = address,
                        Gender = gender,
                        Qualifications = qualifications,
                        YearsOfExperience = yearsOfExperience
                    };

                    auditService.LogInfo($"Doctor registered successfully: {matricule} (ID: {doctor.Id})");
                    return doctor;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Registration failed for doctor matricule '{matricule}'. Exception: {ex.Message}");
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
                    var command = new SqlCommand("SELECT * FROM doctor WHERE matricule = @Matricule", connection);
                    command.Parameters.AddWithValue("@Matricule", matricule);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var doctor = new Doctor
                            {
                                Id = (int)reader["id"],
                                Matricule = (string)reader["matricule"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"]
                            };

                            var passwordHash = HashPassword(password, doctor.Salt);
                            if (passwordHash == doctor.PasswordHash)
                            {
                                var token = tokenService.GenerateToken(doctor.Matricule, "Doctor", DateTime.UtcNow.AddHours(1)); doctor.Token = new Token
                                {
                                    Value = token,
                                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                                    RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                                };
                                doctor.ModificationDate = DateTime.UtcNow;
                                UpdateToken(doctor);
                                auditService.LogInfo($"Doctor logged in successfully: {matricule} (ID: {doctor.Id})");
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
                    auditService.LogInfo($"Doctor logged out successfully: {doctor.Matricule} (ID: {doctor.Id})");
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Logout failed for doctor '{doctor.Matricule}'. Exception: {ex.Message}");
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

        public Doctor GetDoctorById(int doctorId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM doctor WHERE id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", doctorId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Doctor
                            {
                                Id = (int)reader["id"],
                                Matricule = (string)reader["matricule"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                Phone = (string)reader["phone"],
                                DateOfBirth = (DateTime)reader["date_of_birth"],
                                Specialty = (string)reader["specialty"],
                                Department = (string)reader["department"],
                                Email = (string)reader["email"],
                                Address = (string)reader["address"],
                                Gender = (string)reader["gender"],
                                Qualifications = (string)reader["qualifications"],
                                YearsOfExperience = (int)reader["years_of_experience"],
                                Token = reader["token"] as Token
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetDoctorById failed for doctor ID '{doctorId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public List<Doctor> GetDoctorsBySpecialty(string specialty)
        {
            var doctors = new List<Doctor>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM doctor WHERE specialty = @Specialty", connection);
                    command.Parameters.AddWithValue("@Specialty", specialty);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctors.Add(new Doctor
                            {
                                Id = (int)reader["id"],
                                Matricule = (string)reader["matricule"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                Phone = (string)reader["phone"],
                                DateOfBirth = (DateTime)reader["date_of_birth"],
                                Specialty = (string)reader["specialty"],
                                Department = (string)reader["department"],
                                Email = (string)reader["email"],
                                Address = (string)reader["address"],
                                Gender = (string)reader["gender"],
                                Qualifications = (string)reader["qualifications"],
                                YearsOfExperience = (int)reader["years_of_experience"],
                                Token = reader["token"] as Token
                            });
                        }
                    }
                }
                auditService.LogInfo($"Retrieved doctors by specialty '{specialty}' successfully.");
                return doctors;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetDoctorsBySpecialty failed for specialty '{specialty}'. Exception: {ex.Message}");
                return doctors;
            }
        }

        public List<Doctor> GetDoctorsByDepartment(string department)
        {
            var doctors = new List<Doctor>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM doctor WHERE department = @Department", connection);
                    command.Parameters.AddWithValue("@Department", department);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctors.Add(new Doctor
                            {
                                Id = (int)reader["id"],
                                Matricule = (string)reader["matricule"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                Phone = (string)reader["phone"],
                                DateOfBirth = (DateTime)reader["date_of_birth"],
                                Specialty = (string)reader["specialty"],
                                Department = (string)reader["department"],
                                Email = (string)reader["email"],
                                Address = (string)reader["address"],
                                Gender = (string)reader["gender"],
                                Qualifications = (string)reader["qualifications"],
                                YearsOfExperience = (int)reader["years_of_experience"],
                                Token = reader["token"] as Token
                            });
                        }
                    }
                }
                auditService.LogInfo($"Retrieved doctors by department '{department}' successfully.");
                return doctors;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetDoctorsByDepartment failed for department '{department}'. Exception: {ex.Message}");
                return doctors;
            }
        }

        public Doctor GetDoctorByToken(string tokenValue)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM doctor WHERE token = @Token", connection);
                    command.Parameters.AddWithValue("@Token", tokenValue);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var doctor = new Doctor
                            {
                                Id = (int)reader["id"],
                                Matricule = (string)reader["matricule"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                Token = new Token
                                {
                                    Value = (string)reader["token"],
                                    ExpirationDate = (DateTime)reader["token_expiration"]
                                },
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                Phone = (string)reader["phone"],
                                DateOfBirth = (DateTime)reader["date_of_birth"],
                                Specialty = (string)reader["specialty"],
                                Department = (string)reader["department"],
                                Email = (string)reader["email"],
                                Address = (string)reader["address"],
                                Gender = (string)reader["gender"],
                                Qualifications = (string)reader["qualifications"],
                                YearsOfExperience = (int)reader["years_of_experience"]
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
                auditService.LogError($"Operation GetDoctorByToken failed for token '{tokenValue}'. Exception: {ex.Message}");
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
                    var command = new SqlCommand("SELECT * FROM doctor", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctors.Add(new Doctor
                            {
                                Id = (int)reader["id"],
                                Matricule = (string)reader["matricule"],
                                PasswordHash = (string)reader["password_hash"],
                                Salt = (string)reader["salt"],
                                Token = reader["token"] as Token,
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                Phone = (string)reader["phone"],
                                DateOfBirth = (DateTime)reader["date_of_birth"],
                                Specialty = (string)reader["specialty"],
                                Department = (string)reader["department"],
                                Email = (string)reader["email"],
                                Address = (string)reader["address"],
                                Gender = (string)reader["gender"],
                                Qualifications = (string)reader["qualifications"],
                                YearsOfExperience = (int)reader["years_of_experience"]
                            });
                        }
                    }
                }
                auditService.LogInfo("Retrieved all doctors successfully.");
                return doctors;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetAllDoctors failed. Exception: {ex.Message}");
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
                    var command = new SqlCommand("UPDATE doctor SET token = @Token, token_expiration = @TokenExpiration, modification_date = @ModificationDate WHERE id = @Id", connection);
                    command.Parameters.AddWithValue("@Token", (object)doctor.Token?.Value ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TokenExpiration", (object)doctor.Token?.ExpirationDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ModificationDate", doctor.ModificationDate);
                    command.Parameters.AddWithValue("@Id", doctor.Id);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation UpdateToken failed for doctor '{doctor.Matricule}'. Exception: {ex.Message}");
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

        public Doctor UpdateDoctor(int doctorId, string phone, DateTime dateOfBirth, string specialty, string department, string email, string address, string gender, string qualifications, int yearsOfExperience)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("UPDATE doctor SET phone = @Phone, date_of_birth = @DateOfBirth, specialty = @Specialty, department = @Department, email = @Email, address = @Address, gender = @Gender, qualifications = @Qualifications, years_of_experience = @YearsOfExperience, modification_date = @ModificationDate WHERE id = @DoctorId", connection);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@Specialty", specialty);
                    command.Parameters.AddWithValue("@Department", department);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@Qualifications", qualifications);
                    command.Parameters.AddWithValue("@YearsOfExperience", yearsOfExperience);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        auditService.LogWarning($"Update failed: Doctor {doctorId} not found.");
                        return null;
                    }

                    var doctor = GetDoctorById(doctorId);
                    auditService.LogInfo($"Doctor updated successfully: {doctorId}");
                    return doctor;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"UpdateDoctor failed for doctor '{doctorId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public bool DeleteDoctor(int doctorId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("DELETE FROM doctor WHERE id = @DoctorId", connection);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        auditService.LogWarning($"Delete failed: Doctor {doctorId} not found.");
                        return false;
                    }

                    auditService.LogInfo($"Doctor deleted successfully: {doctorId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"DeleteDoctor failed for doctor '{doctorId}'. Exception: {ex.Message}");
                return false;
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
