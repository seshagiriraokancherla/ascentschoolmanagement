namespace AscentSchools.Core.DTOs.School.Master
{
    public class PaymentModeDto
    {
        public int    PaymentModeId { get; set; }
        public string ModeName      { get; set; }
        public string Description   { get; set; }
        public string Status        { get; set; }
        public bool   IsOnline      { get; set; }   // true = triggers gateway checkout
        public int    SchoolId      { get; set; }
    }

    public class SavePaymentModeRequest
    {
        public string ModeName    { get; set; }
        public string Description { get; set; }
        public string Status      { get; set; }
        public bool   IsOnline    { get; set; }
    }
}
