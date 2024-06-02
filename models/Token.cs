using System;

namespace MedicielBack.models
{
    public class Token
    {
        public string Value { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpirationDate { get; set; }
    }
}
