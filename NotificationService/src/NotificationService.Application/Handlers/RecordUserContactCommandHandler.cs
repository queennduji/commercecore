using NotificationService.Application.Commands;
using NotificationService.Application.Common;
using NotificationService.Application.Interfaces;
using MediatR;

namespace NotificationService.Application.Handlers;

public class RecordUserContactCommandHandler : IRequestHandler<RecordUserContactCommand, ServiceResult<bool>>
{
    private readonly IUserContactRepository _userContactRepository;

    public RecordUserContactCommandHandler(IUserContactRepository userContactRepository)
    {
        _userContactRepository = userContactRepository;
    }

    public async Task<ServiceResult<bool>> Handle(RecordUserContactCommand request, CancellationToken cancellationToken)
    {
        await _userContactRepository.UpsertAsync(request.UserId, request.Email, request.PhoneNumber, cancellationToken);
        await _userContactRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }
}
