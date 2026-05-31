using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record PlatformGovernancePersistenceSnapshot(
    IReadOnlyList<PlatformGovernanceControl> Controls,
    IReadOnlyList<PlatformGovernanceAction> Actions,
    IReadOnlyList<PlatformGovernanceAuditEntry> AuditTrail);

public interface IPlatformGovernancePersistence
{
    PlatformGovernancePersistenceSnapshot Load();

    void SaveControl(PlatformGovernanceControl control);

    void SaveAction(PlatformGovernanceAction action);

    void SaveAuditEntry(PlatformGovernanceAuditEntry entry);
}
