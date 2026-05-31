using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record AssetMaintenancePersistenceSnapshot(
    IReadOnlyList<AssetLedgerItem> Assets,
    IReadOnlyList<MaintenancePlan> Plans,
    IReadOnlyList<MaintenanceTask> Tasks,
    IReadOnlyList<AssetLifecycleEvent> LifecycleEvents);

public interface IAssetMaintenancePersistence
{
    AssetMaintenancePersistenceSnapshot Load();

    void SaveAsset(AssetLedgerItem asset);

    void SavePlan(MaintenancePlan plan);

    void SaveTask(MaintenanceTask task);

    void SaveLifecycleEvent(AssetLifecycleEvent lifecycleEvent);

    void SaveTaskCompletion(MaintenanceTask task, AssetLifecycleEvent lifecycleEvent);
}
