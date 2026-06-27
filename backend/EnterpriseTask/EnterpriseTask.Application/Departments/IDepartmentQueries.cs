using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Departments;

public interface IDepartmentQueries
{
    Task<IReadOnlyList<DepartmentCardDto>> GetCardsAsync(UserScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentOptionDto>> GetOptionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentListItemDto>> GetListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentTreeNodeDto>> GetTreeAsync(bool includeInactive, CancellationToken cancellationToken);
}
