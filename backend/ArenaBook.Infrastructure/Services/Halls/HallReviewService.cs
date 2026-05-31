using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Infrastructure.Validation;
using ValidationException = ArenaBook.Application.Common.Exceptions.ValidationException;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using ArenaBook.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Halls;

public sealed class HallReviewService : IHallReviewService
{
    private readonly ArenaBookDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<CreateHallReviewRequest> _createValidator;

    public HallReviewService(
        ArenaBookDbContext db,
        UserManager<ApplicationUser> userManager,
        IValidator<CreateHallReviewRequest> createValidator)
    {
        _db = db;
        _userManager = userManager;
        _createValidator = createValidator;
    }

    public async Task<PagedListResponse<HallReviewResponse>> GetByHallIdAsync(
        int hallId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken))
            throw new NotFoundException("Dvorana nije pronađena.");

        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var q = _db.HallReviews.AsNoTracking().Where(r => r.HallId == hallId);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(r => r.CreatedUtc)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(r => new
            {
                r.Id,
                r.HallId,
                HallName = r.Hall.Name,
                r.ScheduledSessionId,
                r.UserId,
                r.RatingStars,
                r.Comment,
                r.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var names = await _userManager.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Display = u.FirstName + " " + u.LastName })
            .ToDictionaryAsync(x => x.Id, x => x.Display, cancellationToken);

        var items = rows.Select(r => new HallReviewResponse
        {
            Id = r.Id,
            HallId = r.HallId,
            HallName = r.HallName,
            ScheduledSessionId = r.ScheduledSessionId,
            UserId = r.UserId,
            UserDisplayName = names.GetValueOrDefault(r.UserId, r.UserId),
            RatingStars = r.RatingStars,
            Comment = r.Comment,
            CreatedUtc = r.CreatedUtc,
        }).ToList();

        return new PagedListResponse<HallReviewResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<HallReviewResponse> CreateAsync(
        int hallId,
        string userId,
        CreateHallReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        if (!await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken))
            throw new NotFoundException("Dvorana nije pronađena.");

        if (request.ScheduledSessionId is { } sessionId && sessionId > 0)
            return await CreateSessionReviewAsync(hallId, userId, sessionId, request, cancellationToken);

        return await UpsertDirectHallReviewAsync(hallId, userId, request, cancellationToken);
    }

    private async Task<HallReviewResponse> CreateSessionReviewAsync(
        int hallId,
        string userId,
        int sessionId,
        CreateHallReviewRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _db.ScheduledSessions
            .Include(s => s.SessionLifecycleStatus)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.HallId == hallId, cancellationToken);
        if (session is null)
            throw new NotFoundException("Termin nije pronađen za ovu dvoranu.");

        if (session.SessionLifecycleStatus.Code != "COMPLETED")
            throw new ValidationException(
                "Recenzija nije dozvoljena.",
                new Dictionary<string, string[]> { ["session"] = ["Recenzija je moguća tek nakon završetka termina."] });

        if (!await _db.ScheduledSessionParticipants.AnyAsync(
                p => p.ScheduledSessionId == sessionId && p.UserId == userId,
                cancellationToken))
            throw new ValidationException(
                "Recenzija nije dozvoljena.",
                new Dictionary<string, string[]> { ["participant"] = ["Morate biti sudionik termina da biste ocijenili dvoranu."] });

        if (await _db.HallReviews.AnyAsync(
                r => r.ScheduledSessionId == sessionId && r.UserId == userId,
                cancellationToken))
            throw new ConflictException("Već ste ocijenili ovaj termin.");

        var entity = new HallReview
        {
            HallId = hallId,
            UserId = userId,
            ScheduledSessionId = sessionId,
            RatingStars = request.RatingStars,
            Comment = NormalizeComment(request.Comment),
            CreatedUtc = DateTime.UtcNow,
        };
        _db.HallReviews.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await MapReviewResponseAsync(entity, cancellationToken);
    }

    private async Task<HallReviewResponse> UpsertDirectHallReviewAsync(
        int hallId,
        string userId,
        CreateHallReviewRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _db.HallReviews
            .FirstOrDefaultAsync(r => r.HallId == hallId && r.UserId == userId, cancellationToken);

        if (existing is null)
        {
            existing = new HallReview
            {
                HallId = hallId,
                UserId = userId,
                ScheduledSessionId = null,
                RatingStars = request.RatingStars,
                Comment = NormalizeComment(request.Comment),
                CreatedUtc = DateTime.UtcNow,
            };
            _db.HallReviews.Add(existing);
        }
        else
        {
            existing.RatingStars = request.RatingStars;
            existing.Comment = NormalizeComment(request.Comment);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapReviewResponseAsync(existing, cancellationToken);
    }

    private async Task<HallReviewResponse> MapReviewResponseAsync(
        HallReview entity,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.AsNoTracking().FirstAsync(u => u.Id == entity.UserId, cancellationToken);
        var hall = await _db.Halls.AsNoTracking().FirstAsync(h => h.Id == entity.HallId, cancellationToken);
        return new HallReviewResponse
        {
            Id = entity.Id,
            HallId = entity.HallId,
            HallName = hall.Name,
            ScheduledSessionId = entity.ScheduledSessionId,
            UserId = entity.UserId,
            UserDisplayName = user.FirstName + " " + user.LastName,
            RatingStars = entity.RatingStars,
            Comment = entity.Comment,
            CreatedUtc = entity.CreatedUtc,
        };
    }

    private static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    public async Task<IReadOnlyList<HallReviewResponse>> GetPendingForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var completedId = await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(s => s.Code == "COMPLETED")
            .Select(s => s.Id)
            .FirstAsync(cancellationToken);

        var participantSessions = await _db.ScheduledSessionParticipants.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.ScheduledSessionId)
            .ToListAsync(cancellationToken);

        var reviewedSessionIds = await _db.HallReviews.AsNoTracking()
            .Where(r => r.UserId == userId && r.ScheduledSessionId != null)
            .Select(r => r.ScheduledSessionId!.Value)
            .ToListAsync(cancellationToken);

        var pending = await _db.ScheduledSessions.AsNoTracking()
            .Where(s =>
                participantSessions.Contains(s.Id) &&
                s.SessionLifecycleStatusId == completedId &&
                !reviewedSessionIds.Contains(s.Id))
            .OrderByDescending(s => s.EndUtc)
            .Take(20)
            .Select(s => new { s.Id, s.HallId, HallName = s.Hall.Name })
            .ToListAsync(cancellationToken);

        return pending.Select(p => new HallReviewResponse
        {
            HallId = p.HallId,
            HallName = p.HallName,
            ScheduledSessionId = p.Id,
            UserId = userId,
        }).ToList();
    }
}

