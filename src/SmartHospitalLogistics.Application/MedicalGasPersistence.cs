using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record MedicalGasPersistenceSnapshot(
    IReadOnlyList<MedicalGasZone> Zones);

public interface IMedicalGasPersistence
{
    MedicalGasPersistenceSnapshot Load();

    void SaveZone(MedicalGasZone zone);
}
