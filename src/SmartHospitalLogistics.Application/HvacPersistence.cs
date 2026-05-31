using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record HvacPersistenceSnapshot(
    IReadOnlyList<HvacLoop> Loops);

public interface IHvacPersistence
{
    HvacPersistenceSnapshot Load();

    void SaveLoop(HvacLoop loop);
}
