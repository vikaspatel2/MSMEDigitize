using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Ensure DB and all tables exist.
        // EnsureCreated only creates tables if the DB is brand new.
        // If DB exists but has no tables (e.g. after a failed run), we recreate.
        bool canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
        {
            // Check if tables exist by trying a known table
            bool tablesExist = false;
            try
            {
                _ = db.SubscriptionPlans.Count();
                tablesExist = true;
            }
            catch { }

            if (!tablesExist)
            {
                // DB exists but tables don't - delete and recreate
                await db.Database.EnsureDeletedAsync();
            }
        }

        await db.Database.EnsureCreatedAsync();

        if (!db.SubscriptionPlans.Any())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlanDefinition
                {
                    Name = "Free",
                    Plan = (SubscriptionPlanTier)SubscriptionPlan.Free,
                    Description = "Perfect for freelancers starting out",
                    MonthlyPrice = 0,
                    AnnualPrice = 0,
                    MaxUsers = 1,
                    MaxInvoicesPerMonth = 25,
                    MaxInventoryItems = 50,
                    HasGSTFiling = false,
                    HasPayroll = false,
                    IsActive = true
                },
                new SubscriptionPlanDefinition
                {
                    Name = "Starter",
                    Plan = (SubscriptionPlanTier)SubscriptionPlan.Starter,
                    Description = "Great for small businesses",
                    MonthlyPrice = 999,
                    AnnualPrice = 9999,
                    MaxUsers = 3,
                    MaxInvoicesPerMonth = 200,
                    MaxInventoryItems = 500,
                    HasGSTFiling = true,
                    HasPayroll = false,
                    HasEInvoicing = true,
                    IsActive = true
                },
                new SubscriptionPlanDefinition
                {
                    Name = "Professional",
                    Plan = (SubscriptionPlanTier)SubscriptionPlan.Professional,
                    Description = "For growing businesses",
                    MonthlyPrice = 2499,
                    AnnualPrice = 24999,
                    MaxUsers = 10,
                    MaxInvoicesPerMonth = 2000,
                    MaxInventoryItems = 5000,
                    HasGSTFiling = true,
                    HasPayroll = true,
                    HasBankReconciliation = true,
                    HasEInvoicing = true,
                    HasAIInsights = true,
                    IsActive = true
                },
                new SubscriptionPlanDefinition
                {
                    Name = "Enterprise",
                    Plan = (SubscriptionPlanTier)SubscriptionPlan.Enterprise,
                    Description = "For large businesses",
                    MonthlyPrice = 4999,
                    AnnualPrice = 49999,
                    MaxUsers = -1,
                    MaxInvoicesPerMonth = -1,
                    MaxInventoryItems = -1,
                    HasGSTFiling = true,
                    HasPayroll = true,
                    HasBankReconciliation = true,
                    HasEInvoicing = true,
                    HasAIInsights = true,
                    HasMultiWarehouse = true,
                    HasAPIAccess = true,
                    HasPrioritySupport = true,
                    HasDedicatedAccountManager = true,
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();
        }
    }
}


//using Microsoft.EntityFrameworkCore;
//using MSMEDigitize.Core.Entities.Subscriptions;
//using MSMEDigitize.Core.Enums;

//namespace MSMEDigitize.Infrastructure.Data;

//public static class DbSeeder
//{
//    public static async Task SeedAsync(AppDbContext db)
//    {
//        try { await db.Database.MigrateAsync(); }
//        catch { }

//        if (!db.SubscriptionPlans.Any())
//        {
//            db.SubscriptionPlans.AddRange(
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Free",
//                    Plan = SubscriptionPlanTier.Free,
//                    Description = "Perfect for freelancers starting out",
//                    MonthlyPrice = 0,
//                    AnnualPrice = 0,
//                    MaxUsers = 1,
//                    MaxInvoicesPerMonth = 25,
//                    MaxInventoryItems = 50,
//                    HasGSTFiling = false,
//                    HasPayroll = false,
//                    IsActive = true
//                },
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Starter",
//                    Plan = SubscriptionPlanTier.Starter,
//                    Description = "Great for small businesses",
//                    MonthlyPrice = 999,
//                    AnnualPrice = 9999,
//                    MaxUsers = 3,
//                    MaxInvoicesPerMonth = 200,
//                    MaxInventoryItems = 500,
//                    HasGSTFiling = true,
//                    HasPayroll = false,
//                    HasEInvoicing = true,
//                    IsActive = true
//                },
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Professional",
//                    Plan = SubscriptionPlanTier.Professional,
//                    Description = "For growing businesses",
//                    MonthlyPrice = 2499,
//                    AnnualPrice = 24999,
//                    MaxUsers = 10,
//                    MaxInvoicesPerMonth = 2000,
//                    MaxInventoryItems = 5000,
//                    HasGSTFiling = true,
//                    HasPayroll = true,
//                    HasBankReconciliation = true,
//                    HasEInvoicing = true,
//                    HasAIInsights = true,
//                    IsActive = true
//                },
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Enterprise",
//                    Plan = SubscriptionPlanTier.Enterprise,
//                    Description = "For large businesses",
//                    MonthlyPrice = 4999,
//                    AnnualPrice = 49999,
//                    MaxUsers = -1,
//                    MaxInvoicesPerMonth = -1,
//                    MaxInventoryItems = -1,
//                    HasGSTFiling = true,
//                    HasPayroll = true,
//                    HasBankReconciliation = true,
//                    HasEInvoicing = true,
//                    HasAIInsights = true,
//                    HasMultiWarehouse = true,
//                    HasAPIAccess = true,
//                    HasPrioritySupport = true,
//                    HasDedicatedAccountManager = true,
//                    IsActive = true
//                }
//            );

//            await db.SaveChangesAsync();
//        }
//    }
//}

//using Microsoft.EntityFrameworkCore;
//using MSMEDigitize.Core.Entities.Subscriptions;
//using MSMEDigitize.Core.Enums;

//namespace MSMEDigitize.Infrastructure.Data;

//public static class DbSeeder
//{
//    public static async Task SeedAsync(AppDbContext db)
//    {
//        try { await db.Database.MigrateAsync(); }
//        catch { /* May fail in dev without DB */ }

//        if (!db.SubscriptionPlans.Any())
//        {
//            db.SubscriptionPlans.AddRange(
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Free", Plan = SubscriptionPlan.Free,
//                    Description = "Perfect for freelancers starting out",
//                    MonthlyPrice = 0, AnnualPrice = 0,
//                    MaxUsers = 1, MaxInvoicesPerMonth = 25, MaxInventoryItems = 50,
//                    HasGSTFiling = false, HasPayroll = false, IsActive = true
//                },
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Starter", Plan = SubscriptionPlan.Starter,
//                    Description = "Great for small businesses",
//                    MonthlyPrice = 999, AnnualPrice = 9999,
//                    MaxUsers = 3, MaxInvoicesPerMonth = 200, MaxInventoryItems = 500,
//                    HasGSTFiling = true, HasPayroll = false, HasEInvoicing = true, IsActive = true
//                },
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Professional", Plan = SubscriptionPlan.Professional,
//                    Description = "For growing businesses",
//                    MonthlyPrice = 2499, AnnualPrice = 24999,
//                    MaxUsers = 10, MaxInvoicesPerMonth = 2000, MaxInventoryItems = 5000,
//                    HasGSTFiling = true, HasPayroll = true, HasBankReconciliation = true,
//                    HasEInvoicing = true, HasAIInsights = true, IsActive = true
//                },
//                new SubscriptionPlanDefinition
//                {
//                    Name = "Enterprise", Plan = SubscriptionPlan.Enterprise,
//                    Description = "For large businesses",
//                    MonthlyPrice = 4999, AnnualPrice = 49999,
//                    MaxUsers = -1, MaxInvoicesPerMonth = -1, MaxInventoryItems = -1,
//                    HasGSTFiling = true, HasPayroll = true, HasBankReconciliation = true,
//                    HasEInvoicing = true, HasAIInsights = true, HasMultiWarehouse = true,
//                    HasAPIAccess = true, HasPrioritySupport = true,
//                    HasDedicatedAccountManager = true, IsActive = true
//                }
//            );
//            await db.SaveChangesAsync();
//        }
//    }
//}
