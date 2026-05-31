using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record EnergyPerformancePersistenceSnapshot(IReadOnlyList<EnergyPerformanceArea> Areas);

public interface IEnergyPerformancePersistence
{
    EnergyPerformancePersistenceSnapshot Load();

    void SaveArea(EnergyPerformanceArea area);
}
