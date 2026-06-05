using EnterpriseTask.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Infrastructure.Persistence;

public sealed class PostgresDatabaseHealthCheck(ApplicationDbContext dbContext) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return dbContext.Database.CanConnectAsync(cancellationToken);
    }
}
