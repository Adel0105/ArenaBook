using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Application.Abstractions.Halls;

public interface IHallEquipmentService
{
    Task<PagedListResponse<HallEquipmentResponse>> GetPagedAsync(
        int hallId,
        PageRequest page,
        int? equipmentTypeId,
        CancellationToken cancellationToken = default);

    Task<HallEquipmentResponse> GetByIdAsync(int hallId, int linkId, CancellationToken cancellationToken = default);

    Task<HallEquipmentResponse> CreateAsync(int hallId, CreateHallEquipmentRequest request, CancellationToken cancellationToken = default);

    Task<HallEquipmentResponse> UpdateAsync(int hallId, int linkId, UpdateHallEquipmentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int hallId, int linkId, CancellationToken cancellationToken = default);
}


