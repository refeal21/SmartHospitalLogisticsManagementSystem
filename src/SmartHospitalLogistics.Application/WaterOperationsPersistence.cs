using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record WaterOperationsPersistenceSnapshot(IReadOnlyList<WaterOperationsUnit> Units);

public interface IWaterOperationsPersistence
{
    WaterOperationsPersistenceSnapshot Load();

    void SaveUnit(WaterOperationsUnit unit);
}
