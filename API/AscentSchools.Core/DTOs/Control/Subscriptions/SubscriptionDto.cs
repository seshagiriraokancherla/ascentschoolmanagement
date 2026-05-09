using System;

namespace AscentSchools.Core.DTOs.Control.Subscriptions
{
    public class SubscriptionDto
    {
        public int      SubscriptionId { get; set; }
        public int      GroupId        { get; set; }
        public string   PlanName       { get; set; }
        public string   BillingCycle   { get; set; }
        public decimal? Amount         { get; set; }
        public int?     MaxSchools     { get; set; }
        public int?     MaxStudents    { get; set; }
        public DateTime StartDate      { get; set; }
        public DateTime ExpiryDate     { get; set; }
        public string   Status         { get; set; }
    }
}
