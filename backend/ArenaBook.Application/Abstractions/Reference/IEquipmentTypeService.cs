using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface IEquipmentTypeService
{
    Task<PagedListResponse<EquipmentTypeResponse>> GetPagedAsync(PageRequest page, string? q, CancellationToken cancellationToken = default);

    Task<EquipmentTypeResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<EquipmentTypeResponse> CreateAsync(CreateEquipmentTypeRequest request, CancellationToken cancellationToken = default);

    Task<EquipmentTypeResponse> UpdateAsync(int id, UpdateEquipmentTypeRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


