using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Abstractions.Notifications;
using ArenaBook.Application.Abstractions.Sessions;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Application.Sessions;
using BusinessRuleException = ArenaBook.Application.Common.Exceptions.ValidationException;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Sessions;
using System.Text.Json;
using ArenaBook.Domain;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Identity;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ArenaBook.Infrastructure.Services.Sessions;

public sealed class ScheduledSessionService : IScheduledSessionService
{
    private const string MaxParticipantsKey = "Platform.Session.MaxParticipantsPerSession";
    private const string MinSessionPriceKey = "Platform.Session.MinSessionPriceCoins";
    private const string AdminDeleteReason = "Administrator je obrisao termin.";

    private readonly ArenaBookDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISessionOrganizerRoleService _organizerRoleService;
    private readonly IUserNotificationPublisher _notifications;
    private readonly IValidator<CreateScheduledSessionRequest> _createValidator;
    private readonly IValidator<UpdateScheduledSessionRequest> _updateValidator;
    private readonly IValidator<JoinScheduledSessionRequest> _joinValidator;
    private readonly IValidator<CancelScheduledSessionRequest> _cancelValidator;

    public ScheduledSessionService(
        ArenaBookDbContext db,
        UserManager<ApplicationUser> userManager,
        ISessionOrganizerRoleService organizerRoleService,
        IUserNotificationPublisher notifications,
        IValidator<CreateScheduledSessionRequest> createValidator,
        IValidator<UpdateScheduledSessionRequest> updateValidator,
        IValidator<JoinScheduledSessionRequest> joinValidator,
        IValidator<CancelScheduledSessionRequest> cancelValidator)
    {
        _db = db;
        _userManager = userManager;
        _organizerRoleService = organizerRoleService;
        _notifications = notifications;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _joinValidator = joinValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<PagedListResponse<ScheduledSessionListItemResponse>> GetPagedAsync(
        PageRequest page,
        ScheduledSessionListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var q = _db.ScheduledSessions.AsNoTracking();

        if (query.HallId.HasValue)
            q = q.Where(s => s.HallId == query.HallId.Value);
        if (query.SessionKindId.HasValue)
            q = q.Where(s => s.SessionKindId == query.SessionKindId.Value);
        if (query.SessionLifecycleStatusId.HasValue)
            q = q.Where(s => s.SessionLifecycleStatusId == query.SessionLifecycleStatusId.Value);
        if (!string.IsNullOrWhiteSpace(query.OrganizerUserId))
            q = q.Where(s => s.OrganizerUserId == query.OrganizerUserId);
        if (!string.IsNullOrWhiteSpace(query.ParticipantUserId))
            q = q.Where(s => s.Participants.Any(p => p.UserId == query.ParticipantUserId));
        if (query.DateFromUtc.HasValue)
            q = q.Where(s => s.StartUtc >= query.DateFromUtc.Value);
        if (query.DateToUtc.HasValue)
            q = q.Where(s => s.StartUtc <= query.DateToUtc.Value);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(s => s.Hall.Name.Contains(term));
        }

        var projected = q
            .OrderBy(s => s.StartUtc)
            .Select(s => new
            {
                s.Id,
                s.HallId,
                HallName = s.Hall.Name,
                s.OrganizerUserId,
                s.SessionKindId,
                KindCode = s.SessionKind.Code,
                s.SessionLifecycleStatusId,
                StatusCode = s.SessionLifecycleStatus.Code,
                s.StartUtc,
                s.EndUtc,
                s.MaxParticipants,
                s.MaxAgeYears,
                s.PriceTotalCoins,
                s.PricePerParticipantCoins,
                ParticipantCount = s.Participants.Count,
            });

        var total = await projected.CountAsync(cancellationToken);
        var rows = await projected.Skip(skip).Take(normalizedPageSize).ToListAsync(cancellationToken);

        var organizerIds = rows.Select(r => r.OrganizerUserId).Distinct().ToList();
        var emails = await _db.Users.AsNoTracking()
            .Where(u => organizerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(x => x.Id, x => x.Email, cancellationToken);

        var items = rows.Select(r => new ScheduledSessionListItemResponse
        {
            Id = r.Id,
            HallId = r.HallId,
            HallName = r.HallName,
            OrganizerUserId = r.OrganizerUserId,
            OrganizerEmail = emails.GetValueOrDefault(r.OrganizerUserId),
            SessionKindId = r.SessionKindId,
            SessionKindCode = r.KindCode,
            SessionLifecycleStatusId = r.SessionLifecycleStatusId,
            SessionLifecycleCode = r.StatusCode,
            StartUtc = r.StartUtc,
            EndUtc = r.EndUtc,
            MaxParticipants = r.MaxParticipants,
            ParticipantCount = r.ParticipantCount,
            MaxAgeYears = r.MaxAgeYears,
            InviteCode = null,
            PriceTotalCoins = r.PriceTotalCoins,
            PricePerParticipantCoins = r.PricePerParticipantCoins,
        }).ToList();

        return new PagedListResponse<ScheduledSessionListItemResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<ScheduledSessionDetailsResponse> GetByIdAsync(
        int id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.ScheduledSessions.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.HallId,
                HallName = s.Hall.Name,
                s.OrganizerUserId,
                s.SessionKindId,
                KindCode = s.SessionKind.Code,
                s.SessionLifecycleStatusId,
                StatusCode = s.SessionLifecycleStatus.Code,
                s.StartUtc,
                s.EndUtc,
                s.MaxParticipants,
                s.MaxAgeYears,
                s.InviteCode,
                s.CreatedUtc,
                s.PriceTotalCoins,
                s.PricePerParticipantCoins,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            throw new NotFoundException("Termin nije pronađen.");

        var participants = await _db.ScheduledSessionParticipants.AsNoTracking()
            .Where(p => p.ScheduledSessionId == id)
            .Select(p => new { p.Id, p.UserId, p.JoinedUtc, p.CoinsPaid, p.IsOrganizer })
            .ToListAsync(cancellationToken);

        var isPricingLocked = participants.Any(p => p.CoinsPaid > 0);

        var userIds = participants.Select(p => p.UserId).Distinct().ToList();
        if (!userIds.Contains(row.OrganizerUserId))
            userIds.Add(row.OrganizerUserId);

        var userEmails = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(x => x.Id, x => x.Email, cancellationToken);

        var canViewInviteCode = CanViewInviteCode(
            row.OrganizerUserId,
            row.InviteCode,
            viewerUserId,
            isAdministrator,
            participants.Select(p => p.UserId));

        return new ScheduledSessionDetailsResponse
        {
            Id = row.Id,
            HallId = row.HallId,
            HallName = row.HallName,
            OrganizerUserId = row.OrganizerUserId,
            OrganizerEmail = userEmails.GetValueOrDefault(row.OrganizerUserId),
            SessionKindId = row.SessionKindId,
            SessionKindCode = row.KindCode,
            SessionLifecycleStatusId = row.SessionLifecycleStatusId,
            SessionLifecycleCode = row.StatusCode,
            StartUtc = row.StartUtc,
            EndUtc = row.EndUtc,
            MaxParticipants = row.MaxParticipants,
            MaxAgeYears = row.MaxAgeYears,
            InviteCode = canViewInviteCode ? row.InviteCode : null,
            CreatedUtc = row.CreatedUtc,
            PriceTotalCoins = row.PriceTotalCoins,
            PricePerParticipantCoins = row.PricePerParticipantCoins,
            IsPricingLocked = isPricingLocked,
            Participants = participants.Select(p => new ScheduledSessionParticipantResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                UserEmail = userEmails.GetValueOrDefault(p.UserId),
                JoinedUtc = p.JoinedUtc,
                CoinsPaid = p.CoinsPaid,
                IsOrganizer = p.IsOrganizer,
            }).ToList(),
        };
    }

    public async Task<IReadOnlyList<ScheduledSessionAuditEntryResponse>> GetAuditTrailAsync(
        int sessionId,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ScheduledSessions.AsNoTracking()
            .Select(s => new { s.Id, s.OrganizerUserId })
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            if (!isAdministrator)
                throw new NotFoundException("Termin nije pronađen.");

            var anyAudit = await _db.ScheduledSessionAuditEntries.AsNoTracking()
                .AnyAsync(a => a.SessionId == sessionId, cancellationToken);
            if (!anyAudit)
                throw new NotFoundException("Termin nije pronađen.");
        }
        else if (!isAdministrator && session.OrganizerUserId != userId)
        {
            throw new BusinessRuleException("Nemate pravo pregleda audit zapisa za ovaj termin.", Err());
        }

        var rows = await _db.ScheduledSessionAuditEntries.AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .OrderByDescending(a => a.OccurredUtc)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

        var actorIds = rows.Select(r => r.ActorUserId).Distinct().ToList();
        var emails = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(x => x.Id, x => x.Email, cancellationToken);

        var statusIds = rows
            .SelectMany(r => new[] { r.FromLifecycleStatusId, r.ToLifecycleStatusId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var statusCodes = statusIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.SessionLifecycleStatuses.AsNoTracking()
                .Where(s => statusIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Code })
                .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);

        return rows.Select(r => new ScheduledSessionAuditEntryResponse
        {
            Id = r.Id,
            SessionId = r.SessionId,
            ActorUserId = r.ActorUserId,
            ActorEmail = emails.GetValueOrDefault(r.ActorUserId),
            OccurredUtc = r.OccurredUtc,
            Action = r.Action,
            FromLifecycleStatusId = r.FromLifecycleStatusId,
            FromLifecycleCode = r.FromLifecycleStatusId.HasValue
                ? statusCodes.GetValueOrDefault(r.FromLifecycleStatusId.Value)
                : null,
            ToLifecycleStatusId = r.ToLifecycleStatusId,
            ToLifecycleCode = r.ToLifecycleStatusId.HasValue
                ? statusCodes.GetValueOrDefault(r.ToLifecycleStatusId.Value)
                : null,
            DetailsJson = r.DetailsJson,
        }).ToList();
    }

    public async Task<SessionJoinCoinQuoteResponse> GetJoinCoinQuoteAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ScheduledSessions
            .AsNoTracking()
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException("Termin nije pronađen.");

        var cancelledId = await LifecycleIdAsync("CANCELLED", cancellationToken);
        var completedId = await LifecycleIdAsync("COMPLETED", cancellationToken);
        if (session.SessionLifecycleStatusId == cancelledId || session.SessionLifecycleStatusId == completedId)
            throw new ConflictException("Termin nije otvoren za prijave.");

        var confirmedId = await LifecycleIdAsync("CONFIRMED", cancellationToken);
        if (session.SessionLifecycleStatusId != confirmedId)
            throw new ConflictException("Cijena u koinima za pridruživanje dostupna je samo za potvrđen termin.");

        return new SessionJoinCoinQuoteResponse
        {
            ScheduledSessionId = sessionId,
            CoinsRequired = session.PricePerParticipantCoins,
            CurrencyCode = "COIN",
        };
    }

    public async Task<ScheduledSessionDetailsResponse> CreateAsync(
        CreateScheduledSessionRequest request,
        string organizerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        EnforceSchedulingPolicy(request.StartUtc, request.EndUtc, isAdministrator);
        await ValidateOrganizerUserAsync(organizerUserId, cancellationToken);

        var kind = await _db.SessionKinds.AsNoTracking().FirstOrDefaultAsync(k => k.Id == request.SessionKindId, cancellationToken);
        if (kind is null)
            throw new NotFoundException("Vrsta termina nije pronađena.");

        var hall = await _db.Halls.AsNoTracking().FirstOrDefaultAsync(h => h.Id == request.HallId, cancellationToken);
        if (hall is null)
            throw new NotFoundException("Dvorana nije pronađena.");
        if (!hall.IsActive)
            throw new BusinessRuleException("Dvorana nije aktivna.", Err());

        if (kind.Code == "INVITE" && request.MaxAgeYears.HasValue)
            throw new BusinessRuleException("Privatni termin ne koristi dobnu granicu.", Err());

        var pendingId = await LifecycleIdAsync("PENDING", cancellationToken);
        var platformMax = await GetPlatformIntAsync(MaxParticipantsKey, 22, cancellationToken);
        var minPrice = await GetPlatformDecimalAsync(MinSessionPriceKey, 0, cancellationToken);

        if (request.MaxParticipants > Math.Min(hall.CapacityPeople, platformMax))
            throw new BusinessRuleException("Maksimalan broj učesnika premašuje kapacitet dvorane ili platformski limit.", Err());

        var totalPrice = SessionPricing.ComputeTotalPrice(hall.PricePerHourCoins, request.StartUtc, request.EndUtc);
        if (totalPrice < minPrice)
            throw new BusinessRuleException($"Ukupna cijena termina mora biti najmanje {minPrice} koina.", Err());

        if (await HasOverlapAsync(request.HallId, request.StartUtc, request.EndUtc, null, cancellationToken))
            throw new ConflictException("Termin se preklapa s drugim aktivnim terminom u istoj dvorani.");

        var participantPrice = SessionPricing.ComputeParticipantJoinPrice(totalPrice);

        string? invite = null;
        if (kind.Code == "INVITE")
        {
            invite = string.IsNullOrWhiteSpace(request.InviteCode)
                ? GenerateInviteCode()
                : request.InviteCode.Trim();
            if (await _db.ScheduledSessions.AnyAsync(s => s.InviteCode == invite, cancellationToken))
                throw new ConflictException("Kod poziva već postoji. Pokušajte ponovo.");
        }

        var entity = new ScheduledSession
        {
            HallId = request.HallId,
            OrganizerUserId = organizerUserId,
            SessionKindId = request.SessionKindId,
            SessionLifecycleStatusId = pendingId,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            PriceTotalCoins = totalPrice,
            PricePerParticipantCoins = participantPrice,
            MaxParticipants = request.MaxParticipants,
            MaxAgeYears = kind.Code == "PUBLIC" ? request.MaxAgeYears : null,
            InviteCode = invite,
            CreatedUtc = DateTime.UtcNow,
        };

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.ScheduledSessions.Add(entity);
        _db.ScheduledSessionParticipants.Add(new ScheduledSessionParticipant
        {
            ScheduledSession = entity,
            UserId = organizerUserId,
            JoinedUtc = DateTime.UtcNow,
            CoinsPaid = 0,
            IsOrganizer = true,
        });

        await _db.SaveChangesAsync(cancellationToken);

        AppendAudit(
            entity.Id,
            organizerUserId,
            ScheduledSessionAuditActions.Created,
            null,
            pendingId,
            SerializeDetails(new
            {
                request.HallId,
                request.SessionKindId,
                request.StartUtc,
                request.EndUtc,
                request.MaxParticipants,
                request.MaxAgeYears,
                HasInviteCode = !string.IsNullOrEmpty(invite),
                totalPrice,
                participantPrice,
            }));

        await _organizerRoleService.EnsureOrganizerRoleForUserAsync(organizerUserId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await _notifications.TryPublishManyAsync(
            [SessionNotificationBuilder.SessionCreated(organizerUserId, hall.Name, entity.StartUtc)],
            cancellationToken);

        return await GetByIdAsync(entity.Id, organizerUserId, isAdministrator, cancellationToken);
    }

    public async Task<ScheduledSessionDetailsResponse> UpdateAsync(
        int id,
        UpdateScheduledSessionRequest request,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        EnforceSchedulingPolicy(request.StartUtc, request.EndUtc, isAdministrator);

        var entity = await _db.ScheduledSessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Termin nije pronađen.");

        if (!isAdministrator && entity.OrganizerUserId != userId)
            throw new BusinessRuleException("Nemate pravo izmjene ovog termina.", Err());

        var cancelledId = await LifecycleIdAsync("CANCELLED", cancellationToken);
        var completedId = await LifecycleIdAsync("COMPLETED", cancellationToken);
        if (entity.SessionLifecycleStatusId == cancelledId || entity.SessionLifecycleStatusId == completedId)
            throw new ConflictException("Termin je završen ili otkazan i ne može se mijenjati.");

        var hasPaidParticipants = await HasPaidParticipantsAsync(id, cancellationToken);
        if (hasPaidParticipants
            && (entity.StartUtc != request.StartUtc || entity.EndUtc != request.EndUtc))
        {
            throw new ConflictException(
                "Vrijeme termina ne može se mijenjati nakon što su učesnici platili pridruživanje.");
        }

        var kind = await _db.SessionKinds.AsNoTracking().FirstAsync(k => k.Id == entity.SessionKindId, cancellationToken);
        var platformMax = await GetPlatformIntAsync(MaxParticipantsKey, 22, cancellationToken);
        if (request.MaxParticipants > Math.Min(entity.Hall.CapacityPeople, platformMax))
            throw new BusinessRuleException("Maksimalan broj učesnika premašuje kapacitet dvorane ili platformski limit.", Err());

        var participantCount = await _db.ScheduledSessionParticipants.CountAsync(p => p.ScheduledSessionId == id, cancellationToken);
        if (request.MaxParticipants < participantCount)
            throw new ConflictException("Maksimalan broj učesnika ne sme biti manji od trenutnog broja prijavljenih.");

        decimal? newTotalPrice = null;
        decimal? newParticipantPrice = null;
        if (!hasPaidParticipants)
        {
            var minPrice = await GetPlatformDecimalAsync(MinSessionPriceKey, 0, cancellationToken);
            newTotalPrice = SessionPricing.ComputeTotalPrice(
                entity.Hall.PricePerHourCoins,
                request.StartUtc,
                request.EndUtc);
            if (newTotalPrice < minPrice)
                throw new BusinessRuleException($"Ukupna cijena termina mora biti najmanje {minPrice} koina.", Err());

            newParticipantPrice = SessionPricing.ComputeParticipantJoinPrice(newTotalPrice.Value);
        }

        if (await HasOverlapAsync(entity.HallId, request.StartUtc, request.EndUtc, id, cancellationToken))
            throw new ConflictException("Termin se preklapa s drugim aktivnim terminom u istoj dvorani.");

        entity.StartUtc = request.StartUtc;
        entity.EndUtc = request.EndUtc;
        entity.MaxParticipants = request.MaxParticipants;
        entity.MaxAgeYears = kind.Code == "PUBLIC" ? request.MaxAgeYears : null;
        if (newTotalPrice is not null && newParticipantPrice is not null)
        {
            entity.PriceTotalCoins = newTotalPrice.Value;
            entity.PricePerParticipantCoins = newParticipantPrice.Value;
        }

        AppendAudit(
            id,
            userId,
            ScheduledSessionAuditActions.Updated,
            null,
            null,
            SerializeDetails(new
            {
                request.StartUtc,
                request.EndUtc,
                request.MaxParticipants,
                request.MaxAgeYears,
                priceTotalCoins = entity.PriceTotalCoins,
                pricePerParticipantCoins = entity.PricePerParticipantCoins,
                pricingLocked = hasPaidParticipants,
            }));
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, userId, isAdministrator, cancellationToken);
    }

    public async Task<ScheduledSessionDetailsResponse> ConfirmAsync(
        int id,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ScheduledSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Termin nije pronađen.");
        if (!isAdministrator && entity.OrganizerUserId != userId)
            throw new BusinessRuleException("Nemate pravo potvrde ovog termina.", Err());

        return await ApplyLifecycleTransitionAsync(
            entity,
            id,
            userId,
            isAdministrator,
            ScheduledSessionLifecycleAction.Confirm,
            ScheduledSessionAuditActions.Confirmed,
            cancelReason: null,
            cancellationToken);
    }

    public async Task<ScheduledSessionDetailsResponse> CancelAsync(
        int id,
        CancelScheduledSessionRequest request,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());
        }

        var entity = await _db.ScheduledSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Termin nije pronađen.");
        if (!isAdministrator && entity.OrganizerUserId != userId)
            throw new BusinessRuleException("Nemate pravo otkazivanja ovog termina.", Err());

        var reason = request.Reason.Trim();
        return await ApplyLifecycleTransitionAsync(
            entity,
            id,
            userId,
            isAdministrator,
            ScheduledSessionLifecycleAction.Cancel,
            ScheduledSessionAuditActions.Cancelled,
            cancelReason: reason,
            cancellationToken);
    }

    public async Task<ScheduledSessionDetailsResponse> CompleteAsync(
        int id,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ScheduledSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Termin nije pronađen.");
        if (!isAdministrator && entity.OrganizerUserId != userId)
            throw new BusinessRuleException("Nemate pravo završetka ovog termina.", Err());

        return await ApplyLifecycleTransitionAsync(
            entity,
            id,
            userId,
            isAdministrator,
            ScheduledSessionLifecycleAction.Complete,
            ScheduledSessionAuditActions.Completed,
            cancelReason: null,
            cancellationToken);
    }

    public async Task DeleteAsync(int id, string actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ScheduledSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Termin nije pronađen.");

        await ApplyLifecycleTransitionAsync(
            entity,
            id,
            actorUserId,
            isAdministrator: true,
            ScheduledSessionLifecycleAction.AdminDelete,
            ScheduledSessionAuditActions.Deleted,
            cancelReason: AdminDeleteReason,
            cancellationToken);
    }

    public async Task JoinAsync(
        int id,
        JoinScheduledSessionRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var validation = await _joinValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var session = await _db.ScheduledSessions
            .Include(s => s.Hall)
            .Include(s => s.SessionKind)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
            throw new NotFoundException("Termin nije pronađen.");

        var cancelledId = await LifecycleIdAsync("CANCELLED", cancellationToken);
        var completedId = await LifecycleIdAsync("COMPLETED", cancellationToken);
        if (session.SessionLifecycleStatusId == cancelledId || session.SessionLifecycleStatusId == completedId)
            throw new ConflictException("Termin nije otvoren za prijave.");

        var confirmedId = await LifecycleIdAsync("CONFIRMED", cancellationToken);
        if (session.SessionLifecycleStatusId != confirmedId)
            throw new ConflictException("Prijava je moguća samo na potvrđen termin.");

        if (session.SessionKind.Code == "INVITE")
        {
            var code = request.InviteCode?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(code) || !string.Equals(code, session.InviteCode, StringComparison.Ordinal))
                throw new BusinessRuleException("Nevažeći kod poziva.", Err());
        }

        if (session.SessionKind.Code == "PUBLIC" && session.MaxAgeYears.HasValue)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.DateOfBirth is null)
                throw new BusinessRuleException("Profil mora imati datum rođenja za ovaj termin.", Err());
            var age = GetAgeAt(user.DateOfBirth.Value, session.StartUtc);
            if (age > session.MaxAgeYears.Value)
                throw new BusinessRuleException("Starosna dob premašuje limit termina.", Err());
        }

        if (await _db.ScheduledSessionParticipants.AnyAsync(p => p.ScheduledSessionId == id && p.UserId == userId, cancellationToken))
            throw new ConflictException("Već ste prijavljeni na ovaj termin.");

        var count = await _db.ScheduledSessionParticipants.CountAsync(p => p.ScheduledSessionId == id, cancellationToken);
        if (count >= session.MaxParticipants)
            throw new ConflictException("Termin je popunjen.");

        var cost = session.PricePerParticipantCoins;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var wallet = await _db.UserCoinWallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet is null)
        {
            wallet = new UserCoinWallet
            {
                UserId = userId,
                BalanceCoins = 0,
                UpdatedUtc = DateTime.UtcNow,
            };
            _db.UserCoinWallets.Add(wallet);
        }

        if (wallet.BalanceCoins < cost)
            throw new BusinessRuleException("Nedovoljno koina na računu.", Err());

        wallet.BalanceCoins -= cost;
        wallet.UpdatedUtc = DateTime.UtcNow;
        wallet.LedgerEntries.Add(new CoinLedgerEntry
        {
            AmountCoins = -cost,
            BalanceAfter = wallet.BalanceCoins,
            ReasonCode = CoinLedgerReasonCodes.SessionJoin,
            RelatedScheduledSessionId = id,
            CreatedUtc = DateTime.UtcNow,
        });

        _db.ScheduledSessionParticipants.Add(new ScheduledSessionParticipant
        {
            ScheduledSessionId = id,
            UserId = userId,
            JoinedUtc = DateTime.UtcNow,
            CoinsPaid = cost,
            IsOrganizer = false,
        });

        AppendAudit(
            id,
            userId,
            ScheduledSessionAuditActions.ParticipantJoined,
            null,
            null,
            SerializeDetails(new { participantUserId = userId, coinsPaid = cost }));

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var joinMessages = new List<UserNotificationMessage>
        {
            SessionNotificationBuilder.ParticipantJoinedSelf(userId, session.Hall.Name, session.StartUtc),
        };
        if (!string.Equals(session.OrganizerUserId, userId, StringComparison.Ordinal))
        {
            joinMessages.Add(SessionNotificationBuilder.ParticipantJoinedOrganizer(
                session.OrganizerUserId,
                session.Hall.Name,
                session.StartUtc));
        }

        await _notifications.TryPublishManyAsync(joinMessages, cancellationToken);
    }

    private async Task<ScheduledSessionDetailsResponse> ApplyLifecycleTransitionAsync(
        ScheduledSession entity,
        int sessionId,
        string actorUserId,
        bool isAdministrator,
        ScheduledSessionLifecycleAction action,
        string auditAction,
        string? cancelReason,
        CancellationToken cancellationToken)
    {
        var fromCode = await LifecycleCodeByIdAsync(entity.SessionLifecycleStatusId, cancellationToken);
        var plan = ScheduledSessionLifecycleStateMachine.Plan(
            fromCode,
            action,
            entity.EndUtc,
            DateTime.UtcNow,
            cancelReason);

        var targetId = await LifecycleIdAsync(plan.TargetStatusCode, cancellationToken);
        var prev = entity.SessionLifecycleStatusId;
        var notificationContext = await LoadSessionNotificationContextAsync(sessionId, cancellationToken);
        var participantUserIds = await GetParticipantUserIdsAsync(sessionId, cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        IReadOnlyList<(string UserId, decimal Amount)> refunds = Array.Empty<(string, decimal)>();
        if (plan.RefundParticipants)
            refunds = await RefundParticipantsAsync(sessionId, cancellationToken);

        entity.SessionLifecycleStatusId = targetId;

        string? auditJson = null;
        if (action is ScheduledSessionLifecycleAction.Cancel or ScheduledSessionLifecycleAction.AdminDelete)
        {
            auditJson = SerializeDetails(new
            {
                refundsProcessed = plan.RefundParticipants,
                cancelReason = cancelReason?.Trim(),
            });
        }

        AppendAudit(sessionId, actorUserId, auditAction, prev, targetId, auditJson);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        if (notificationContext is not null)
        {
            await PublishLifecycleNotificationsAsync(
                action,
                notificationContext,
                participantUserIds,
                cancelReason,
                refunds,
                cancellationToken);
        }

        return await GetByIdAsync(sessionId, actorUserId, isAdministrator, cancellationToken);
    }

    private async Task PublishLifecycleNotificationsAsync(
        ScheduledSessionLifecycleAction action,
        SessionNotificationContext context,
        IReadOnlyList<string> participantUserIds,
        string? cancelReason,
        IReadOnlyList<(string UserId, decimal Amount)> refunds,
        CancellationToken cancellationToken)
    {
        var messages = new List<UserNotificationMessage>();

        switch (action)
        {
            case ScheduledSessionLifecycleAction.Confirm:
                foreach (var userId in participantUserIds)
                {
                    messages.Add(SessionNotificationBuilder.SessionConfirmed(
                        userId,
                        context.HallName,
                        context.StartUtc));
                }

                break;

            case ScheduledSessionLifecycleAction.Cancel:
                var reason = string.IsNullOrWhiteSpace(cancelReason) ? "Nije naveden." : cancelReason.Trim();
                foreach (var userId in participantUserIds)
                {
                    messages.Add(SessionNotificationBuilder.SessionCancelled(
                        userId,
                        context.HallName,
                        context.StartUtc,
                        reason));
                }

                AppendRefundNotifications(messages, context.HallName, refunds);
                break;

            case ScheduledSessionLifecycleAction.AdminDelete:
                var deleteReason = string.IsNullOrWhiteSpace(cancelReason) ? "Nije naveden." : cancelReason.Trim();
                foreach (var userId in participantUserIds)
                {
                    messages.Add(SessionNotificationBuilder.SessionDeleted(
                        userId,
                        context.HallName,
                        context.StartUtc,
                        deleteReason));
                }

                AppendRefundNotifications(messages, context.HallName, refunds);
                break;

            case ScheduledSessionLifecycleAction.Complete:
                foreach (var userId in participantUserIds)
                {
                    messages.Add(SessionNotificationBuilder.SessionCompleted(
                        userId,
                        context.HallName,
                        context.StartUtc));
                }

                break;
        }

        if (messages.Count > 0)
            await _notifications.TryPublishManyAsync(messages, cancellationToken);
    }

    private static void AppendRefundNotifications(
        List<UserNotificationMessage> messages,
        string hallName,
        IReadOnlyList<(string UserId, decimal Amount)> refunds)
    {
        foreach (var (userId, amount) in refunds)
        {
            messages.Add(SessionNotificationBuilder.SessionRefund(userId, hallName, amount));
        }
    }

    private async Task<SessionNotificationContext?> LoadSessionNotificationContextAsync(
        int sessionId,
        CancellationToken cancellationToken)
    {
        return await _db.ScheduledSessions.AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new SessionNotificationContext(s.Id, s.Hall.Name, s.StartUtc, s.OrganizerUserId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<List<string>> GetParticipantUserIdsAsync(int sessionId, CancellationToken cancellationToken) =>
        _db.ScheduledSessionParticipants.AsNoTracking()
            .Where(p => p.ScheduledSessionId == sessionId)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<(string UserId, decimal Amount)>> RefundParticipantsAsync(
        int sessionId,
        CancellationToken cancellationToken)
    {
        var participants = await _db.ScheduledSessionParticipants
            .Where(p => p.ScheduledSessionId == sessionId && p.CoinsPaid > 0)
            .ToListAsync(cancellationToken);

        if (participants.Count == 0)
            return Array.Empty<(string, decimal)>();

        var participantUserIds = participants.Select(p => p.UserId).Distinct().ToList();
        var wallets = await _db.UserCoinWallets
            .Where(w => participantUserIds.Contains(w.UserId))
            .ToListAsync(cancellationToken);
        var walletByUserId = wallets.ToDictionary(w => w.UserId);

        var refunds = new List<(string UserId, decimal Amount)>();

        foreach (var p in participants)
        {
            if (!walletByUserId.TryGetValue(p.UserId, out var wallet))
                continue;

            var amount = p.CoinsPaid;
            wallet.BalanceCoins += amount;
            wallet.UpdatedUtc = DateTime.UtcNow;
            wallet.LedgerEntries.Add(new CoinLedgerEntry
            {
                AmountCoins = amount,
                BalanceAfter = wallet.BalanceCoins,
                ReasonCode = CoinLedgerReasonCodes.SessionRefundCancel,
                RelatedScheduledSessionId = sessionId,
                CreatedUtc = DateTime.UtcNow,
            });
            p.CoinsPaid = 0;
            refunds.Add((p.UserId, amount));
        }

        return refunds;
    }

    private sealed record SessionNotificationContext(
        int SessionId,
        string HallName,
        DateTime StartUtc,
        string OrganizerUserId);

    private Task<bool> HasPaidParticipantsAsync(int sessionId, CancellationToken cancellationToken) =>
        _db.ScheduledSessionParticipants.AnyAsync(
            p => p.ScheduledSessionId == sessionId && p.CoinsPaid > 0,
            cancellationToken);

    private async Task ValidateOrganizerUserAsync(string organizerUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizerUserId))
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                new Dictionary<string, string[]>
                {
                    ["organizerUserId"] = ["Organizator nije pronađen."],
                });
        }

        var user = await _userManager.FindByIdAsync(organizerUserId);
        if (user is null)
            throw new NotFoundException("Organizator nije pronađen.");

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new BusinessRuleException(
                "Organizator nije aktivan.",
                new Dictionary<string, string[]> { ["organizerUserId"] = ["Organizator nije aktivan."] });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var canOrganize = roles.Contains(ApplicationRoles.Member)
            || roles.Contains(ApplicationRoles.Organizer)
            || roles.Contains(ApplicationRoles.Administrator);
        if (!canOrganize)
        {
            throw new BusinessRuleException(
                "Organizator mora imati ulogu Member, Organizer ili Administrator.",
                new Dictionary<string, string[]>
                {
                    ["organizerUserId"] = ["Organizator mora imati ulogu Member, Organizer ili Administrator."],
                });
        }
    }

    private static void EnforceSchedulingPolicy(DateTime startUtc, DateTime endUtc, bool isAdministrator)
    {
        var errors = SessionTimeRules.ValidateSchedulingPolicy(
            startUtc,
            endUtc,
            allowHistoricalTimes: isAdministrator,
            DateTime.UtcNow);
        if (errors.Count > 0)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija vremena termina nije prošla.",
                errors.ToDictionary(kv => kv.Key, kv => kv.Value));
        }
    }

    private static int GetAgeAt(DateOnly birth, DateTime atUtc)
    {
        var d = DateOnly.FromDateTime(atUtc.Date);
        var age = d.Year - birth.Year;
        if (d < birth.AddYears(age))
            age--;
        return age;
    }

    private async Task<bool> HasOverlapAsync(
        int hallId,
        DateTime start,
        DateTime end,
        int? excludeSessionId,
        CancellationToken cancellationToken)
    {
        var cancelledId = await LifecycleIdAsync("CANCELLED", cancellationToken);
        var completedId = await LifecycleIdAsync("COMPLETED", cancellationToken);
        var q = _db.ScheduledSessions.Where(s =>
            s.HallId == hallId
            && s.SessionLifecycleStatusId != cancelledId
            && s.SessionLifecycleStatusId != completedId
            && s.StartUtc < end
            && s.EndUtc > start);
        if (excludeSessionId.HasValue)
            q = q.Where(s => s.Id != excludeSessionId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    private async Task<int> LifecycleIdAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    private async Task<string> LifecycleCodeByIdAsync(int statusId, CancellationToken cancellationToken)
    {
        return await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(x => x.Id == statusId)
            .Select(x => x.Code)
            .FirstAsync(cancellationToken);
    }

    private async Task<int> GetPlatformIntAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var v = await _db.PlatformSettingEntries.AsNoTracking()
            .Where(x => x.SettingKey == key)
            .Select(x => x.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
        return int.TryParse(v, out var n) ? n : fallback;
    }

    private async Task<decimal> GetPlatformDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken)
    {
        var v = await _db.PlatformSettingEntries.AsNoTracking()
            .Where(x => x.SettingKey == key)
            .Select(x => x.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
        return decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d)
            ? d
            : fallback;
    }

    private static bool CanViewInviteCode(
        string organizerUserId,
        string? inviteCode,
        string? viewerUserId,
        bool isAdministrator,
        IEnumerable<string> participantUserIds)
    {
        if (string.IsNullOrEmpty(inviteCode))
            return false;

        if (isAdministrator)
            return true;

        if (string.IsNullOrEmpty(viewerUserId))
            return false;

        if (organizerUserId == viewerUserId)
            return true;

        return participantUserIds.Contains(viewerUserId);
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> code = stackalloc char[8];
        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        for (var i = 0; i < code.Length; i++)
            code[i] = chars[randomBytes[i] % chars.Length];

        return new string(code);
    }

    private static IReadOnlyDictionary<string, string[]> Err()
    {
        return new Dictionary<string, string[]>();
    }

    private void AppendAudit(
        int sessionId,
        string actorUserId,
        string action,
        int? fromLifecycleStatusId,
        int? toLifecycleStatusId,
        string? detailsJson)
    {
        _db.ScheduledSessionAuditEntries.Add(new ScheduledSessionAuditEntry
        {
            SessionId = sessionId,
            ActorUserId = actorUserId,
            OccurredUtc = DateTime.UtcNow,
            Action = action,
            FromLifecycleStatusId = fromLifecycleStatusId,
            ToLifecycleStatusId = toLifecycleStatusId,
            DetailsJson = detailsJson,
        });
    }

    private static string? SerializeDetails<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }
}

