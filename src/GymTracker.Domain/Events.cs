namespace GymTracker.Domain;

public record RoutineCreated(Guid RoutineId,Guid UserId, string Name, DateTime OccurredOn) : IDomainEvent
{
   public Guid EventId { get; } = Guid.NewGuid();
}
public record SetLogged(Guid SessionId, Guid UserId, Guid ExerciseId, DateTime OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}