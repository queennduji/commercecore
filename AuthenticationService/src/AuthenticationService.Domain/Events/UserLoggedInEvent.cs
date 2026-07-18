namespace AuthenticationService.Domain.Events;

public class UserLoggedInEvent
{
    public Guid UserId { get; set; }
    public DateTime LoggedInAt { get; set; }
}
