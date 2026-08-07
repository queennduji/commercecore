using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Application.Queries;
using MediatR;

namespace InventoryService.Application.Handlers;

public class GetReservationQueryHandler : IRequestHandler<GetReservationQuery, ServiceResult<StockReservationDto>>
{
    private readonly IStockReservationRepository _stockReservationRepository;

    public GetReservationQueryHandler(IStockReservationRepository stockReservationRepository)
    {
        _stockReservationRepository = stockReservationRepository;
    }

    public async Task<ServiceResult<StockReservationDto>> Handle(GetReservationQuery request, CancellationToken cancellationToken)
    {
        var reservation = await _stockReservationRepository.GetByIdAsync(request.Id, cancellationToken);
        return reservation is null
            ? ServiceResult<StockReservationDto>.Failure("Reservation not found.")
            : ServiceResult<StockReservationDto>.Success(reservation.ToDto());
    }
}
