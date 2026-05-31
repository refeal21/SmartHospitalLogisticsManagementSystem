using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record SafetyEmergencyPersistenceSnapshot(IReadOnlyList<SafetyEmergencyNode> Nodes);

public interface ISafetyEmergencyPersistence
{
    SafetyEmergencyPersistenceSnapshot Load();

    void SaveNode(SafetyEmergencyNode node);
}
