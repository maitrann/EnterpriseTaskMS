namespace EnterpriseTask.Application.Departments;

public interface IDepartmentQueries
{
    Task<IReadOnlyList<DepartmentCardDto>> GetCardsAsync(CancellationToken cancellationToken);
}
