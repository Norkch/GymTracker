using System;
using System.Linq;
using GymTracker.Domain;
using Xunit;

namespace GymTracker.Tests;

public class WeightTests
{
    [Fact]
    public void Pounds_are_converted_to_kilograms()
        => Assert.Equal(45.359m, Weight.From(100, "lb").Kg);

    [Fact]
    public void Kilograms_are_kept_as_is()
        => Assert.Equal(62.5m, Weight.From(62.5m, "kg").Kg);

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Out_of_range_is_rejected(decimal kg)
        => Assert.Throws<DomainException>(() => Weight.FromKg(kg));

    [Fact]
    public void Unknown_unit_is_rejected()
        => Assert.Throws<DomainException>(() => Weight.From(10, "stone"));
}

public class RoutineTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static RoutineExercise Ex(string name = "Press banca") => new(Guid.NewGuid(), name, 4, 8, Weight.FromKg(50), DateTime.UtcNow);

    [Fact]
    public void Create_raises_RoutineCreated_with_the_owner()
    {
        var r = Routine.Create(User, "Push day", new[] { Ex() });
        var ev = Assert.IsType<RoutineCreated>(Assert.Single(r.Events));
        Assert.Equal(User, ev.UserId);
        Assert.Equal(r.Id, ev.RoutineId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string name)
        => Assert.Throws<DomainException>(() => Routine.Create(User, name, new[] { Ex() }));

    [Fact]
    public void Create_rejects_routine_without_exercises()
        => Assert.Throws<DomainException>(() => Routine.Create(User, "Vacía", Array.Empty<RoutineExercise>()));

    [Fact]
    public void Create_rejects_missing_owner()
        => Assert.Throws<DomainException>(() => Routine.Create(Guid.Empty, "Push", new[] { Ex() }));

    [Fact]
    public void Create_rejects_more_than_the_maximum_of_exercises()
    {
        var tooMany = Enumerable.Range(0, Routine.MaxExercises + 1).Select(i => Ex($"Ejercicio {i}"));
        Assert.Throws<DomainException>(() => Routine.Create(User, "Enorme", tooMany));
    }

    [Fact]
    public void Exercise_needs_positive_sets_and_reps()
        => Assert.Throws<DomainException>(() =>
            Routine.Create(User, "Push", new[] { new RoutineExercise(Guid.NewGuid(), "Press", 0, 8, Weight.FromKg(50), DateTime.UtcNow) }));
}

public class WorkoutSessionTests
{
    private static readonly Guid User = Guid.NewGuid();

    [Fact]
    public void LogSet_numbers_sets_per_exercise()
    {
        var s = WorkoutSession.Start(User, Guid.NewGuid());
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        s.LogSet(a, 8, Weight.FromKg(60));
        s.LogSet(a, 8, Weight.FromKg(62.5m));
        s.LogSet(b, 10, Weight.FromKg(20));

        Assert.Equal(new[] { 1, 2 }, s.Sets.Where(x => x.ExerciseId == a).Select(x => x.SetNumber));
        Assert.Equal(1, s.Sets.Single(x => x.ExerciseId == b).SetNumber);
    }

    [Fact]
    public void LogSet_raises_SetLogged_for_the_owner()
    {
        var s = WorkoutSession.Start(User, Guid.NewGuid());
        var ex = Guid.NewGuid();
        s.LogSet(ex, 5, Weight.FromKg(100));
        var ev = Assert.IsType<SetLogged>(Assert.Single(s.Events));
        Assert.Equal(User, ev.UserId);
        Assert.Equal(ex, ev.ExerciseId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void LogSet_rejects_invalid_reps(int reps)
        => Assert.Throws<DomainException>(() =>
            WorkoutSession.Start(User, Guid.NewGuid()).LogSet(Guid.NewGuid(), reps, Weight.FromKg(50)));

    [Fact]
    public void Rehydrate_restores_state_without_raising_events()
    {
        var sets = new[] { new SetLog(Guid.NewGuid(), 1, 8, Weight.FromKg(60)) };
        var s = WorkoutSession.Rehydrate(Guid.NewGuid(), User, Guid.NewGuid(), DateTime.UtcNow, sets);
        Assert.Single(s.Sets);
        Assert.Empty(s.Events);
    }
}

public class BodyMeasurementTests
{
    [Fact]
    public void Rejects_body_weight_out_of_range()
        => Assert.Throws<DomainException>(() => BodyMeasurement.Create(Guid.NewGuid(), 900, null, null, null));

    [Fact]
    public void Rejects_negative_centimeters()
        => Assert.Throws<DomainException>(() => BodyMeasurement.Create(Guid.NewGuid(), 80, -5, null, null));

    [Fact]
    public void Accepts_partial_measurements()
    {
        var m = BodyMeasurement.Create(Guid.NewGuid(), 80, 85, null, null);
        Assert.Equal(85, m.WaistCm);
        Assert.Null(m.ChestCm);
    }
}