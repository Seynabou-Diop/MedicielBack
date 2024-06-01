

namespace MedicielBack.models
{

    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
    }


}
