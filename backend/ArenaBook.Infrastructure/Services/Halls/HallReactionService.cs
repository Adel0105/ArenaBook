using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Infrastructure.Validation;
using ValidationException = ArenaBook.Application.Common.Exceptions.ValidationException;
using ArenaBook.Application.Contracts.Halls;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Halls;

public sealed class HallReactionService : IHallReactionService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<SetHallReactionRequest> _validator;

    public HallReactionService(
        ArenaBookDbContext db,
        IValidator<SetHallReactionRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public Task<HallReactionSummaryResponse> GetSummaryAsync(
        int hallId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        return BuildSummaryAsync(hallId, userId, cancellationToken);
    }

    public async Task<HallReactionSummaryResponse> SetReactionAsync(
        int hallId,
        string userId,
        SetHallReactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        if (!await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken))
            throw new NotFoundException("Dvorana nije pronađena.");

        var reaction = request.Reaction.Trim().ToLowerInvariant();
        var existing = await _db.HallReactions
            .FirstOrDefaultAsync(r => r.HallId == hallId && r.UserId == userId, cancellationToken);

        if (reaction == "none")
        {
            if (existing is not null)
                _db.HallReactions.Remove(existing);
        }
        else
        {
            var isLike = reaction == "like";
            var now = DateTime.UtcNow;
            if (existing is null)
            {
                _db.HallReactions.Add(new HallReaction
                {
                    HallId = hallId,
                    UserId = userId,
                    IsLike = isLike,
                    CreatedUtc = now,
                    UpdatedUtc = now,
                });
            }
            else
            {
                existing.IsLike = isLike;
                existing.UpdatedUtc = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildSummaryAsync(hallId, userId, cancellationToken);
    }

    private async Task<HallReactionSummaryResponse> BuildSummaryAsync(
        int hallId,
        string? userId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken))
            throw new NotFoundException("Dvorana nije pronađena.");

        var likeCount = await _db.HallReactions.AsNoTracking()
            .CountAsync(r => r.HallId == hallId && r.IsLike, cancellationToken);
        var dislikeCount = await _db.HallReactions.AsNoTracking()
            .CountAsync(r => r.HallId == hallId && !r.IsLike, cancellationToken);

        string? userReaction = null;
        if (!string.IsNullOrEmpty(userId))
        {
            var userVote = await _db.HallReactions.AsNoTracking()
                .FirstOrDefaultAsync(r => r.HallId == hallId && r.UserId == userId, cancellationToken);
            if (userVote is not null)
                userReaction = userVote.IsLike ? "like" : "dislike";
        }

        return new HallReactionSummaryResponse
        {
            LikeCount = likeCount,
            DislikeCount = dislikeCount,
            UserReaction = userReaction,
        };
    }
}

