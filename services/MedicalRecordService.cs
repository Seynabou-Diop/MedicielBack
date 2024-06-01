using MedicielBack.models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MedicielBack.services
{
    public class MedicalRecordService
    {
        private readonly string connectionString;
        private readonly AuditService auditService;

        public MedicalRecordService(AuditService auditService)
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this.auditService = auditService;
        }

        public MedicalRecord CreateRecord(int doctorId, string patientName, string diagnosis, string treatment)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("INSERT INTO MedicalRecords (DoctorId, PatientName, Diagnosis, Treatment, Date, CreationDate, ModificationDate) OUTPUT INSERTED.Id VALUES (@DoctorId, @PatientName, @Diagnosis, @Treatment, @Date, @CreationDate, @ModificationDate)", connection);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);
                    command.Parameters.AddWithValue("@PatientName", patientName);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Treatment", treatment);
                    command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);

                    var id = (int)command.ExecuteScalar();

                    var record = new MedicalRecord
                    {
                        Id = id,
                        DoctorId = doctorId,
                        PatientName = patientName,
                        Diagnosis = diagnosis,
                        Treatment = treatment,
                        Date = DateTime.UtcNow,
                        CreationDate = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow
                    };

                    auditService.LogInfo($"Medical record created: {record.Id} by doctor {doctorId}");
                    return record;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"CreateRecord failed for doctor '{doctorId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public MedicalRecord UpdateRecord(int doctorId, int recordId, string diagnosis, string treatment)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("UPDATE MedicalRecords SET Diagnosis = @Diagnosis, Treatment = @Treatment, Date = @Date, ModificationDate = @ModificationDate WHERE Id = @Id AND DoctorId = @DoctorId", connection);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Treatment", treatment);
                    command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@Id", recordId);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        auditService.LogWarning($"Update failed: Record {recordId} not found for doctor {doctorId}");
                        return null;
                    }

                    var record = new MedicalRecord
                    {
                        Id = recordId,
                        DoctorId = doctorId,
                        Diagnosis = diagnosis,
                        Treatment = treatment,
                        Date = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow
                    };

                    auditService.LogInfo($"Medical record updated: {record.Id} by doctor {doctorId}");
                    return record;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"UpdateRecord failed for doctor '{doctorId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public List<MedicalRecord> GetRecordsByDoctor(int doctorId)
        {
            var records = new List<MedicalRecord>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM MedicalRecords WHERE DoctorId = @DoctorId", connection);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new MedicalRecord
                            {
                                Id = (int)reader["Id"],
                                DoctorId = (int)reader["DoctorId"],
                                PatientName = (string)reader["PatientName"],
                                Diagnosis = (string)reader["Diagnosis"],
                                Treatment = (string)reader["Treatment"],
                                Date = (DateTime)reader["Date"],
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            });
                        }
                    }
                }
                auditService.LogInfo($"Retrieved records for doctor {doctorId}.");
                return records;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetRecordsByDoctor failed for doctor '{doctorId}'. Exception: {ex.Message}");
                return records;
            }
        }

        public MedicalRecord GetRecordById(int doctorId, int recordId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM MedicalRecords WHERE Id = @Id AND DoctorId = @DoctorId", connection);
                    command.Parameters.AddWithValue("@Id", recordId);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new MedicalRecord
                            {
                                Id = (int)reader["Id"],
                                DoctorId = (int)reader["DoctorId"],
                                PatientName = (string)reader["PatientName"],
                                Diagnosis = (string)reader["Diagnosis"],
                                Treatment = (string)reader["Treatment"],
                                Date = (DateTime)reader["Date"],
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetRecordById failed for doctor '{doctorId}' and record '{recordId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public List<MedicalRecord> GetAllMedicalRecords()
        {
            var records = new List<MedicalRecord>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM MedicalRecords", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new MedicalRecord
                            {
                                Id = (int)reader["Id"],
                                DoctorId = (int)reader["DoctorId"],
                                PatientName = (string)reader["PatientName"],
                                Diagnosis = (string)reader["Diagnosis"],
                                Treatment = (string)reader["Treatment"],
                                Date = (DateTime)reader["Date"],
                                CreationDate = (DateTime)reader["CreationDate"],
                                ModificationDate = (DateTime)reader["ModificationDate"]
                            });
                        }
                    }
                }
                auditService.LogInfo("Retrieved all medical records.");
                return records;
            }
            catch (Exception ex)
            {
                auditService.LogError($"GetAllMedicalRecords failed. Exception: {ex.Message}");
                return records;
            }
        }
    }
}
