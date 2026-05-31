using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record PowerDistributionPersistenceSnapshot(
    IReadOnlyList<PowerDistributionCircuit> Circuits);

public interface IPowerDistributionPersistence
{
    PowerDistributionPersistenceSnapshot Load();

    void SaveCircuit(PowerDistributionCircuit circuit);
}
