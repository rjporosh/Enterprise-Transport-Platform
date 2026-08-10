namespace RouteService.Domain.Exceptions;

public class ScheduleNotFoundException : DomainException
{
    public ScheduleNotFoundException(Guid scheduleId)
        : base($"Schedule with id '{scheduleId}' was not found.") { }
}
