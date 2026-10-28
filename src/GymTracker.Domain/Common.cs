namespace GymTracker.Domain;

public interface IDomainEvent
{
    Guid EventId { get; } 
    DateTime OccurredOn { get; }
}

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _events = new();
    public Guid Id { get; protected set; }
    public IReadOnlyCollection<IDomainEvent> Events => _events.AsReadOnly();

    protected void RaiseEvent(IDomainEvent @event)
    {
        _events.Add(@event);
    }
}

public class DomainException(string message): Exception(message);

public sealed record Weight
{
    private const decimal PoundsToKg = 0.453592m;
    public decimal Kg { get;}
    private Weight(decimal kg) => this.Kg = kg;

    public static Weight FromKg(decimal kg)
    {
        if (kg < 0) throw new DomainException("El peso no puede ser negativo");
        if (kg > 1000) throw new DomainException("El peso no puede ser mayor a 1000 kg");
        return new Weight(kg);
    }

    public static Weight From(decimal value, string unit) => unit?.Trim().ToLowerInvariant() switch
    {
        "kg" => FromKg(value),
        "lb" => FromKg(decimal.Round(value * PoundsToKg, 3)),
        _ => throw new DomainException("Unidad de peso no soportada")
    };


}


