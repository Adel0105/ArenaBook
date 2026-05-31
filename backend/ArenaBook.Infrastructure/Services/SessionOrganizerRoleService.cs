using ArenaBook.Application.Abstractions;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ArenaBook.Infrastructure.Services;

public sealed class SessionOrganizerRoleService : ISessionOrganizerRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public SessionOrganizerRoleService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task EnsureOrganizerRoleForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return;

        if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Organizer))
            return;

        await _userManager.AddToRoleAsync(user, ApplicationRoles.Organizer);
    }
}

