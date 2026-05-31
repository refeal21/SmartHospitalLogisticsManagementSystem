using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class PlatformGovernancePersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresControlsActionsAndAuditTrail()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-governance-{Guid.NewGuid():N}.db");
        try
        {
            var first = new PlatformGovernanceService(new SqlitePlatformGovernancePersistence(dbPath));
            var actionResult = first.RecordAction(
                "GOV-SAFETY-EMERGENCY",
                new CreateGovernanceActionCommand(
                    "补齐消防安防联动复盘记录",
                    "消防值班人员",
                    "后勤管理部"));
            Assert.True(actionResult.Succeeded);

            var second = new PlatformGovernanceService(new SqlitePlatformGovernancePersistence(dbPath));
            var reloaded = second.GetControlDetail("GOV-SAFETY-EMERGENCY");

            Assert.NotNull(reloaded);
            Assert.Equal("GOV-SAFETY-EMERGENCY", reloaded.Control.ControlCode);
            Assert.Contains(reloaded.Actions, action => action.Title.Contains("复盘记录"));
            Assert.Contains(reloaded.AuditTrail, entry => entry.Operation == "RecordAction");
            Assert.Contains(reloaded.SourceEvidence, evidence =>
                evidence.Sources.Contains("PPT") &&
                evidence.Sources.Count >= 2);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
