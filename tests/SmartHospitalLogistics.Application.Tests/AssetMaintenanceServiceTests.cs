using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class AssetMaintenanceServiceTests
{
    private readonly AssetMaintenanceService _service = new();

    [Fact]
    public void MaintenanceBoardReturnsAssetsPlansDueTasksAndSourceEvidence()
    {
        var board = _service.GetMaintenanceBoard();

        Assert.NotEmpty(board.Assets);
        Assert.NotEmpty(board.Plans);
        Assert.NotEmpty(board.DueTasks);
        Assert.True(board.Kpis.RiskAssets >= 1);
        Assert.True(board.Kpis.DueTasks >= 1);
        Assert.True(board.Kpis.AverageHealthScore < 100);
        Assert.Equal("MT-20260530-0002", board.DueTasks[0].TaskNo);
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.FeatureName == "设备设施资产台账" &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.FeatureName == "巡检保养计划" &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public void AssetDetailIncludesPlansTasksLifecycleAndBimLocation()
    {
        var detail = _service.GetAssetMaintenanceDetail("MEDGAS-IPD-8F");

        Assert.NotNull(detail);
        Assert.Equal("MEDGAS-IPD-8F", detail.Asset.AssetCode);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Asset.Location.BimElementId);
        Assert.NotEmpty(detail.Plans);
        Assert.NotEmpty(detail.Tasks);
        Assert.NotEmpty(detail.Lifecycle);
        Assert.Contains(detail.SourceEvidence, evidence => evidence.Sources.Contains("中科医信"));
    }

    [Fact]
    public void CompletingNormalInspectionRecordsChecklistAndLifecycle()
    {
        var result = _service.CompleteTask(
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

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(result.Task);
        Assert.Equal(MaintenanceTaskStatus.Completed, result.Task.Status);
        Assert.Equal(MaintenanceOutcome.Normal, result.Task.Outcome);
        Assert.Null(result.GeneratedWorkOrder);

        var detail = _service.GetAssetMaintenanceDetail(result.Task.AssetCode);
        Assert.Contains(detail!.Lifecycle, item =>
            item.EventType == "完成巡检" &&
            item.Summary.Contains("过滤器压差"));
    }

    [Fact]
    public void CompletingAbnormalInspectionCanCreateFollowUpWorkOrder()
    {
        var result = _service.CompleteTask(
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

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(result.Task);
        Assert.Equal(MaintenanceTaskStatus.ConvertedToWorkOrder, result.Task.Status);
        Assert.Equal(MaintenanceOutcome.Abnormal, result.Task.Outcome);
        Assert.NotNull(result.GeneratedWorkOrder);
        Assert.StartsWith("WO-MT-", result.GeneratedWorkOrder.WorkOrderNo);
        Assert.Equal(WorkOrderStatus.New, result.GeneratedWorkOrder.Status);
        Assert.Equal("医气维保人员", result.Task.CompletedBy);
    }

    [Fact]
    public void CompletingClosedTaskReturnsFailureWithoutChangingTask()
    {
        _service.CompleteTask(
            "MT-20260530-0004",
            new CompleteMaintenanceTaskCommand(
                "电梯维保组",
                MaintenanceOutcome.Normal,
                "月度巡检已完成",
                [new InspectionChecklistResult("CHK-RUN", "正常", "运行平稳")],
                ConvertToWorkOrder: false));

        var before = _service.GetAssetMaintenanceDetail("ELV-OPD-01")!.Tasks
            .Single(task => task.TaskNo == "MT-20260530-0004");
        var result = _service.CompleteTask(
            "MT-20260530-0004",
            new CompleteMaintenanceTaskCommand(
                "电梯维保组",
                MaintenanceOutcome.Normal,
                "重复提交",
                [new InspectionChecklistResult("CHK-RUN", "正常", "重复提交")],
                ConvertToWorkOrder: false));

        Assert.False(result.Succeeded);
        Assert.Equal(before.Status, result.Task?.Status);
    }

    [Fact]
    public void MissingTaskReturnsNotFound()
    {
        var result = _service.CompleteTask(
            "MT-NOT-FOUND",
            new CompleteMaintenanceTaskCommand(
                "调度员",
                MaintenanceOutcome.Normal,
                "不存在的任务",
                [],
                ConvertToWorkOrder: false));

        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
    }
}
