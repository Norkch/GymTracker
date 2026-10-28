namespace GymTracker.Domain;

public record SetLog(Guid ExerciseId, int SetNumber, int Reps, Weight weight);

public class WorkoutSession : AggregateRoot
{
    private readonly List<SetLog> _sets = new();

    public Guid UserId { get; private set; }
    public Guid RoutineId { get; private set; }
    public DateTime Date { get; private set; }
    public IReadOnlyList<SetLog> Sets => _sets;

    private WorkoutSession() { }

    public static WorkoutSession Start(Guid userId, Guid routineId)
    {
        if (userId == Guid.Empty) throw new DomainException("La sesión necesita un usuario.");
        return new WorkoutSession { Id = Guid.NewGuid(), UserId = userId, RoutineId = routineId, Date = DateTime.UtcNow };
    }

    
    public static WorkoutSession Rehydrate(Guid id, Guid userId, Guid routineId, DateTime date, IEnumerable<SetLog> sets)
    {
        var s = new WorkoutSession { Id = id, UserId = userId, RoutineId = routineId, Date = date };
        s._sets.AddRange(sets);
        return s;
    }

    public void LogSet(Guid exerciseId, int reps, Weight weight)
    {
        if (reps is <= 0 or > 1000) throw new DomainException("Las repeticiones deben estar entre 1 y 1000.");

        var number = _sets.Count(s => s.ExerciseId == exerciseId) + 1;
        _sets.Add(new SetLog(exerciseId, number, reps, weight));
        RaiseEvent(new SetLogged(Id, UserId, exerciseId, DateTime.UtcNow));
    }
}