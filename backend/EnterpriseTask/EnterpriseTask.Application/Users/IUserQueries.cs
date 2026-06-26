using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Users;

public interface IUserQueries
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<UserDetailDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}
