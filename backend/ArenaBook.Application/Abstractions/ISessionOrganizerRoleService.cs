namespace ArenaBook.Application.Abstractions;

public interface ISessionOrganizerRoleService
{
    Task EnsureOrganizerRoleForUserAsync(string userId, CancellationToken cancellationToken = default);
}

