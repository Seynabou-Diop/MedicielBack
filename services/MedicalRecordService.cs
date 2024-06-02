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
        private readonly EncryptionService encryptionService;

        public MedicalRecordService(AuditService auditService, EncryptionService encryptionService)
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this.auditService = auditService;
            this.encryptionService = encryptionService;
        }

        public MedicalRecord CreateRecord(int doctorId, string patientName, string diagnosis, string treatment, string patientPhone, DateTime patientDateOfBirth, string patientAddress, string emergencyContactName, string emergencyContactPhone, string insuranceProvider, string policyNumber, string allergies, string medications, string previousConditions, string notes)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("INSERT INTO medical_record (doctor_id, patient_name, diagnosis, treatment, date, creation_date, modification_date, patient_phone, patient_date_of_birth, patient_address, emergency_contact_name, emergency_contact_phone, insurance_provider, policy_number, allergies, medications, previous_conditions, notes) OUTPUT INSERTED.id VALUES (@DoctorId, @PatientName, @Diagnosis, @Treatment, @Date, @CreationDate, @ModificationDate, @PatientPhone, @PatientDateOfBirth, @PatientAddress, @EmergencyContactName, @EmergencyContactPhone, @InsuranceProvider, @PolicyNumber, @Allergies, @Medications, @PreviousConditions, @Notes)", connection);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);
                    command.Parameters.AddWithValue("@PatientName", patientName);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Treatment", treatment);
                    command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@PatientPhone", encryptionService.Encrypt(patientPhone));
                    command.Parameters.AddWithValue("@PatientDateOfBirth", patientDateOfBirth);
                    command.Parameters.AddWithValue("@PatientAddress", encryptionService.Encrypt(patientAddress));
                    command.Parameters.AddWithValue("@EmergencyContactName", emergencyContactName);
                    command.Parameters.AddWithValue("@EmergencyContactPhone", encryptionService.Encrypt(emergencyContactPhone));
                    command.Parameters.AddWithValue("@InsuranceProvider", encryptionService.Encrypt(insuranceProvider));
                    command.Parameters.AddWithValue("@PolicyNumber", encryptionService.Encrypt(policyNumber));
                    command.Parameters.AddWithValue("@Allergies", allergies);
                    command.Parameters.AddWithValue("@Medications", medications);
                    command.Parameters.AddWithValue("@PreviousConditions", previousConditions);
                    command.Parameters.AddWithValue("@Notes", notes);

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
                        ModificationDate = DateTime.UtcNow,
                        PatientPhone = patientPhone,
                        PatientDateOfBirth = patientDateOfBirth,
                        PatientAddress = patientAddress,
                        EmergencyContactName = emergencyContactName,
                        EmergencyContactPhone = emergencyContactPhone,
                        InsuranceProvider = insuranceProvider,
                        PolicyNumber = policyNumber,
                        Allergies = allergies,
                        Medications = medications,
                        PreviousConditions = previousConditions,
                        Notes = notes
                    };

                    auditService.LogInfo($"Medical record created successfully: {record.Id} by doctor {doctorId}");
                    return record;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"CreateRecord failed for doctor '{doctorId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public MedicalRecord UpdateRecord(int doctorId, int recordId, string diagnosis, string treatment, string patientPhone, DateTime patientDateOfBirth, string patientAddress, string emergencyContactName, string emergencyContactPhone, string insuranceProvider, string policyNumber, string allergies, string medications, string previousConditions, string notes)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("UPDATE medical_record SET diagnosis = @Diagnosis, treatment = @Treatment, date = @Date, modification_date = @ModificationDate, patient_phone = @PatientPhone, patient_date_of_birth = @PatientDateOfBirth, patient_address = @PatientAddress, emergency_contact_name = @EmergencyContactName, emergency_contact_phone = @EmergencyContactPhone, insurance_provider = @InsuranceProvider, policy_number = @PolicyNumber, allergies = @Allergies, medications = @Medications, previous_conditions = @PreviousConditions, notes = @Notes WHERE id = @Id AND doctor_id = @DoctorId", connection);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Treatment", treatment);
                    command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModificationDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@PatientPhone", encryptionService.Encrypt(patientPhone));
                    command.Parameters.AddWithValue("@PatientDateOfBirth", patientDateOfBirth);
                    command.Parameters.AddWithValue("@PatientAddress", encryptionService.Encrypt(patientAddress));
                    command.Parameters.AddWithValue("@EmergencyContactName", emergencyContactName);
                    command.Parameters.AddWithValue("@EmergencyContactPhone", encryptionService.Encrypt(emergencyContactPhone));
                    command.Parameters.AddWithValue("@InsuranceProvider", encryptionService.Encrypt(insuranceProvider));
                    command.Parameters.AddWithValue("@PolicyNumber", encryptionService.Encrypt(policyNumber));
                    command.Parameters.AddWithValue("@Allergies", allergies);
                    command.Parameters.AddWithValue("@Medications", medications);
                    command.Parameters.AddWithValue("@PreviousConditions", previousConditions);
                    command.Parameters.AddWithValue("@Notes", notes);
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
                        ModificationDate = DateTime.UtcNow,
                        PatientPhone = patientPhone,
                        PatientDateOfBirth = patientDateOfBirth,
                        PatientAddress = patientAddress,
                        EmergencyContactName = emergencyContactName,
                        EmergencyContactPhone = emergencyContactPhone,
                        InsuranceProvider = insuranceProvider,
                        PolicyNumber = policyNumber,
                        Allergies = allergies,
                        Medications = medications,
                        PreviousConditions = previousConditions,
                        Notes = notes
                    };

                    auditService.LogInfo($"Medical record updated successfully: {record.Id} by doctor {doctorId}");
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
                    var command = new SqlCommand("SELECT * FROM medical_record WHERE doctor_id = @DoctorId", connection);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new MedicalRecord
                            {
                                Id = (int)reader["id"],
                                DoctorId = (int)reader["doctor_id"],
                                PatientName = (string)reader["patient_name"],
                                Diagnosis = (string)reader["diagnosis"],
                                Treatment = (string)reader["treatment"],
                                Date = (DateTime)reader["date"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                PatientPhone = encryptionService.Decrypt((string)reader["patient_phone"]),
                                PatientDateOfBirth = (DateTime)reader["patient_date_of_birth"],
                                PatientAddress = encryptionService.Decrypt((string)reader["patient_address"]),
                                EmergencyContactName = (string)reader["emergency_contact_name"],
                                EmergencyContactPhone = encryptionService.Decrypt((string)reader["emergency_contact_phone"]),
                                InsuranceProvider = encryptionService.Decrypt((string)reader["insurance_provider"]),
                                PolicyNumber = encryptionService.Decrypt((string)reader["policy_number"]),
                                Allergies = (string)reader["allergies"],
                                Medications = (string)reader["medications"],
                                PreviousConditions = (string)reader["previous_conditions"],
                                Notes = (string)reader["notes"]
                            });
                        }
                    }
                }
                auditService.LogInfo($"Retrieved records for doctor {doctorId} successfully.");
                return records;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetRecordsByDoctor failed for doctor '{doctorId}'. Exception: {ex.Message}");
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
                    var command = new SqlCommand("SELECT * FROM medical_record WHERE id = @Id AND doctor_id = @DoctorId", connection);
                    command.Parameters.AddWithValue("@Id", recordId);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new MedicalRecord
                            {
                                Id = (int)reader["id"],
                                DoctorId = (int)reader["doctor_id"],
                                PatientName = (string)reader["patient_name"],
                                Diagnosis = (string)reader["diagnosis"],
                                Treatment = (string)reader["treatment"],
                                Date = (DateTime)reader["date"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                PatientPhone = encryptionService.Decrypt((string)reader["patient_phone"]),
                                PatientDateOfBirth = (DateTime)reader["patient_date_of_birth"],
                                PatientAddress = encryptionService.Decrypt((string)reader["patient_address"]),
                                EmergencyContactName = (string)reader["emergency_contact_name"],
                                EmergencyContactPhone = encryptionService.Decrypt((string)reader["emergency_contact_phone"]),
                                InsuranceProvider = encryptionService.Decrypt((string)reader["insurance_provider"]),
                                PolicyNumber = encryptionService.Decrypt((string)reader["policy_number"]),
                                Allergies = (string)reader["allergies"],
                                Medications = (string)reader["medications"],
                                PreviousConditions = (string)reader["previous_conditions"],
                                Notes = (string)reader["notes"]
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetRecordById failed for doctor '{doctorId}' and record '{recordId}'. Exception: {ex.Message}");
                return null;
            }
        }

        public bool DeleteRecord(int doctorId, int recordId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("DELETE FROM medical_record WHERE id = @RecordId AND doctor_id = @DoctorId", connection);
                    command.Parameters.AddWithValue("@RecordId", recordId);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        auditService.LogWarning($"Delete failed: Record {recordId} not found for doctor {doctorId}");
                        return false;
                    }

                    auditService.LogInfo($"Medical record deleted successfully: {recordId} by doctor {doctorId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"DeleteRecord failed for doctor '{doctorId}' and record '{recordId}'. Exception: {ex.Message}");
                return false;
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
                    var command = new SqlCommand("SELECT * FROM medical_record", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new MedicalRecord
                            {
                                Id = (int)reader["id"],
                                DoctorId = (int)reader["doctor_id"],
                                PatientName = (string)reader["patient_name"],
                                Diagnosis = (string)reader["diagnosis"],
                                Treatment = (string)reader["treatment"],
                                Date = (DateTime)reader["date"],
                                CreationDate = (DateTime)reader["creation_date"],
                                ModificationDate = (DateTime)reader["modification_date"],
                                PatientPhone = encryptionService.Decrypt((string)reader["patient_phone"]),
                                PatientDateOfBirth = (DateTime)reader["patient_date_of_birth"],
                                PatientAddress = encryptionService.Decrypt((string)reader["patient_address"]),
                                EmergencyContactName = (string)reader["emergency_contact_name"],
                                EmergencyContactPhone = encryptionService.Decrypt((string)reader["emergency_contact_phone"]),
                                InsuranceProvider = encryptionService.Decrypt((string)reader["insurance_provider"]),
                                PolicyNumber = encryptionService.Decrypt((string)reader["policy_number"]),
                                Allergies = (string)reader["allergies"],
                                Medications = (string)reader["medications"],
                                PreviousConditions = (string)reader["previous_conditions"],
                                Notes = (string)reader["notes"]
                            });
                        }
                    }
                }
                auditService.LogInfo("Retrieved all medical records successfully.");
                return records;
            }
            catch (Exception ex)
            {
                auditService.LogError($"Operation GetAllMedicalRecords failed. Exception: {ex.Message}");
                return records;
            }
        }
    }
}
