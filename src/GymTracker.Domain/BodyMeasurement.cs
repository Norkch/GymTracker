namespace GymTracker.Domain;

public sealed class BodyMeasurement
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime Date { get; } = DateTime.UtcNow;
    public Guid UserId { get; private init; }
    public decimal WeightKg { get; private init; }
    public decimal? WaistCm { get; private init; }
    public decimal? ChestCm { get; private init; }
    public decimal? ArmCm { get; private init; }

    private BodyMeasurement() { }

    public static BodyMeasurement Create(Guid userId, decimal weightKg, decimal? waistCm, decimal? chestCm, decimal? armCm)
    {
        if (userId == Guid.Empty) throw new DomainException("La medida necesita un usuario.");
        if (weightKg is <= 0 or > 500) throw new DomainException("El peso corporal debe estar entre 0 y 500 kg.");
        CheckCentimeters("cintura", waistCm);
        CheckCentimeters("pecho", chestCm);
        CheckCentimeters("brazo", armCm);

        return new BodyMeasurement { UserId = userId, WeightKg = weightKg, WaistCm = waistCm, ChestCm = chestCm, ArmCm = armCm };
    }

    private static void CheckCentimeters(string part, decimal? cm)
    {
        if (cm is <= 0 or > 300) throw new DomainException($"La medida de {part} debe estar entre 0 y 300 cm.");
    }
}