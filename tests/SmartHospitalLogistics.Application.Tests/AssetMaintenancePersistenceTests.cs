using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class AssetMaintenancePersistenceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-assets-{Guid.NewGuid():N}.db");

    [Fact]
    public void SqlitePersistenceRestoresCompletedInspectionAcrossServiceInstances()
    {
        var service = new AssetMaintenanceService(new SqliteAssetMaintenancePersistence(_dbPath));
        var result = service.CompleteTask(
            "MT-20260530-0003",
            new CompleteMaintenanceTaskCommand(
                "暖通班工作人员",
                MaintenanceOutcome.Normal,
                "过滤器压差与运行电流均在阈值内",
                [
                    new InspectionChecklistResult("CHK-PRESSURE", "正常", "压差 28Pa"),
                    new InspectionChecklistResult("CHK-CURRENT", "正常", "电流 12A")
                ],
                ConvertToWorkOrder: false));

        var reloaded = new AssetMaintenanceService(new SqliteAssetMaintenancePersistence(_dbPath));
        var detail = reloaded.GetAssetMaintenanceDetail("CHW-B1-02");
        var task = detail!.Tasks.Single(item => item.TaskNo == result.Task!.TaskNo);

        Assert.Equal(MaintenanceTaskStatus.Completed, task.Status);
        Assert.Equal(MaintenanceOutcome.Normal, task.Outcome);
        Assert.Equal("暖通班工作人员", task.CompletedBy);
        Assert.Contains(task.ChecklistResults, item => item.Code == "CHK-CURRENT" && item.Result == "正常");
        Assert.Contains(detail.Lifecycle, item => item.EventType == "完成巡检" && item.Summary.Contains("过滤器压差"));
    }

    [Fact]
    public void SqlitePersistenceRestoresAbnormalInspectionConvertedToWorkOrder()
    {
        var service = new AssetMaintenanceService(new SqliteAssetMaintenancePersistence(_dbPath));
        var result = service.CompleteTask(
            "MT-20260530-0002",
            new CompleteMaintenanceTaskCommand(
                "医气维保人员",
                MaintenanceOutcome.Abnormal,
                "8F 医气分区阀箱压力波动，需要现场维修",
                [
                    new InspectionChecklistResult("CHK-PRESSURE", "异常", "氧气压力低于下限"),
                    new InspectionChecklistResult("CHK-VALVE", "正常", "阀门状态正常")
                ],
                ConvertToWorkOrder: true));

        var reloaded = new AssetMaintenanceService(new SqliteAssetMaintenancePersistence(_dbPath));
        var detail = reloaded.GetAssetMaintenanceDetail("MEDGAS-IPD-8F");
        var task = detail!.Tasks.Single(item => item.TaskNo == result.Task!.TaskNo);
        var board = reloaded.GetMaintenanceBoard();

        Assert.Equal(MaintenanceTaskStatus.ConvertedToWorkOrder, task.Status);
        Assert.Equal(MaintenanceOutcome.Abnormal, task.Outcome);
        Assert.StartsWith("WO-MT-", task.WorkOrderNo);
        Assert.DoesNotContain(board.DueTasks, item => item.TaskNo == task.TaskNo);
        Assert.Contains(detail.Lifecycle, item => item.EventType == "异常转工单" && item.Summary.Contains("压力波动"));
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
