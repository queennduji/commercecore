using AuthenticationService.Domain.Events;

namespace AuthenticationService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishUserRegisteredAsync(UserRegisteredEvent evt, CancellationToken cancellationToken = default);

    Task PublishUserLoggedInAsync(UserLoggedInEvent evt, CancellationToken cancellationToken = default);
}
