using MedicielBack.models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MedicielBack.services
{


    public class AuditService
    {
        private readonly string connectionString;
        private readonly List<AuditLog> auditLogs = new List<AuditLog>();

        public AuditService()
        {
            this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        public void LogInfo(string message, string userId = null)
        {
            Log("INFO", message, userId);
        }

        public void LogWarning(string message, string userId = null)
        {
            Log("WARNING", message, userId);
        }

        public void LogError(string message, string userId = null)
        {
            Log("ERROR", message, userId);
        }

        private void Log(string level, string message, string userId)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = message,
                Timestamp = DateTime.UtcNow,
                Level = level
            };

            auditLogs.Add(log);
            SaveLogToDatabase(log);
            Console.WriteLine($"[{log.Timestamp}] {log.Level}: {log.Action} (UserId={log.UserId})");
        }

        private void SaveLogToDatabase(AuditLog log)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("INSERT INTO AuditLogs (UserId, Action, Timestamp, Level) VALUES (@UserId, @Action, @Timestamp, @Level)", connection);
                command.Parameters.AddWithValue("@UserId", (object)log.UserId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Action", log.Action);
                command.Parameters.AddWithValue("@Timestamp", log.Timestamp);
                command.Parameters.AddWithValue("@Level", log.Level);

                command.ExecuteNonQuery();
            }
        }
    }

}
