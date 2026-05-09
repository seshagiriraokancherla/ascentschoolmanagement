using AscentSchools.Core.DTOs.Control.Subscriptions;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.Control
{
    public class SubscriptionRepository
    {
        private readonly IConnectionFactory _db;
        public SubscriptionRepository(IConnectionFactory db) { _db = db; }

        public SubscriptionDto GetByGroupId(int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<SubscriptionDto>(
                    @"SELECT subscription_id SubscriptionId, group_id GroupId, plan_name PlanName,
                             billing_cycle BillingCycle, amount Amount, max_schools MaxSchools,
                             max_students MaxStudents, start_date StartDate, expiry_date ExpiryDate, status Status
                      FROM subscriptions WHERE group_id = @groupId",
                    new { groupId });
        }

        public void Upsert(int groupId, UpdateSubscriptionRequest r)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM subscriptions WHERE group_id = @groupId)
                          UPDATE subscriptions
                          SET plan_name = @PlanName, billing_cycle = @BillingCycle, amount = @Amount,
                              max_schools = @MaxSchools, max_students = @MaxStudents,
                              start_date = @StartDate, expiry_date = @ExpiryDate, status = @Status
                          WHERE group_id = @groupId
                      ELSE
                          INSERT INTO subscriptions
                              (group_id, plan_name, billing_cycle, amount, max_schools, max_students, start_date, expiry_date, status)
                          VALUES
                              (@groupId, @PlanName, @BillingCycle, @Amount, @MaxSchools, @MaxStudents, @StartDate, @ExpiryDate, @Status)",
                    new { groupId, r.PlanName, r.BillingCycle, r.Amount, r.MaxSchools,
                          r.MaxStudents, r.StartDate, r.ExpiryDate, r.Status });
        }
    }
}
