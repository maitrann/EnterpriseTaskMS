using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Departments;

public interface IDepartmentQueries
{
    Task<IReadOnlyList<DepartmentCardDto>> GetCardsAsync(UserScope scope, CancellationToken cancellationToken);
}
