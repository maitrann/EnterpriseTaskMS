using EnterpriseTask.Application.Development;

namespace EnterpriseTask.Infrastructure.Development;

public sealed class DatabaseSeeder : IDatabaseSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Development seeding is handled by supabase_schema_v2_clean.sql. Create users in Supabase Auth, then assign roles in public.user_roles.");
    }
}
