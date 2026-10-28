using System.Collections.Generic;

namespace GymTracker.Domain;

public record RoutineExercise(Guid ExerciseId, string Name, int Sets, int Reps, Weight Weight, DateTime OccurredOn);

public class Routine: AggregateRoot
{
    public const int MaxExercises = 30;

    private readonly List<RoutineExercise> _exercises = new();

    public Guid UserId {get; set;}
    public string Name {get; private set;} = "";
    public IReadOnlyList<RoutineExercise> Excercises => _exercises;

    private Routine(){}

    public static Routine Create(Guid userId, string name, IEnumerable<RoutineExercise> exercises)
    {
        if (userId == Guid.Empty) throw new DomainException("La rutina necesita un usuario");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("La rutina necesita un nombre");

        var routine = new Routine {Id =  Guid.NewGuid(), UserId = userId, Name = name.Trim()};
        foreach (var exercise in exercises)
        {
            routine.AddExercise(exercise);
        }
        if (routine._exercises.Count == 0) throw new DomainException("La rutina necesita al menos un ejercicio.");

        routine.RaiseEvent(new RoutineCreated(routine.Id, userId, name, DateTime.UtcNow));
        return routine;
    }
    public void AddExercise(RoutineExercise e)
    {
        if (string.IsNullOrWhiteSpace(e.Name)) throw new DomainException("El ejercicio necesita nombre.");
        if (e.Sets <= 0 || e.Reps <= 0) throw new DomainException("Series y repeticiones deben ser mayores a 0.");
        if (_exercises.Count >= MaxExercises) throw new DomainException($"Una rutina admite hasta {MaxExercises} ejercicios.");
        _exercises.Add(e);
    }
}