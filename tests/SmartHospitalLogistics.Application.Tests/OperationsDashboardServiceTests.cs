using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class OperationsDashboardServiceTests
{
    private readonly OperationsDashboardService _service = new();

    [Fact]
    public void DashboardReflectsFourHospitalLogisticsDomains()
    {
        var dashboard = _service.GetDashboard();

        Assert.Contains(dashboard.Modules, module => module.Domain == OperationDomain.Foundation);
        Assert.Contains(dashboard.Modules, module => module.Domain == OperationDomain.LogisticsService);
        Assert.Contains(dashboard.Modules, module => module.Domain == OperationDomain.Environment);
        Assert.Contains(dashboard.Modules, module => module.Domain == OperationDomain.IntegratedManagement);
    }

    [Fact]
    public void ActiveWorkOrdersPrioritizeCriticalEscalationsBeforeRoutineWork()
    {
        var activeOrders = _service.GetActiveWorkOrders();

        Assert.NotEmpty(activeOrders);
        Assert.Equal(Priority.Critical, activeOrders[0].Priority);
        Assert.Equal(WorkOrderStatus.Escalated, activeOrders[0].Status);
    }

    [Fact]
    public void RiskAssetsOnlyIncludesWarningOrFaultAssets()
    {
        var riskAssets = _service.GetRiskAssets();

        Assert.All(riskAssets, asset =>
        {
            Assert.True(asset.Status is FacilityStatus.Warning or FacilityStatus.Fault);
        });
    }

    [Fact]
    public void LogisticsBlueprintExposesFunctionOrientedMenusInsteadOfPptOnlyBoards()
    {
        var blueprint = _service.GetLogisticsBlueprint();

        Assert.Contains(blueprint.NavigationGroups, group => group.Name == "一站式服务" && group.Items.Contains("工单调度"));
        Assert.Contains(blueprint.NavigationGroups, group => group.Name == "设备设施" && group.Items.Contains("设备台账"));
        Assert.Contains(blueprint.NavigationGroups, group => group.Name == "环境监管" && group.Items.Contains("预警池"));
        Assert.Contains(blueprint.NavigationGroups, group => group.Name == "综合管理" && group.Items.Contains("合同管理"));
        Assert.True(blueprint.NavigationGroups.Count >= 7);
    }

    [Fact]
    public void LogisticsBlueprintPreservesPptModuleScale()
    {
        var blueprint = _service.GetLogisticsBlueprint();

        Assert.Equal(29, blueprint.PptReportedPrimaryModuleTotal);
        Assert.Equal(156, blueprint.PptReportedSecondaryItemTotal);
        Assert.Equal(29, blueprint.FeatureGroups.Sum(group => group.PrimaryModuleCount));
        Assert.Equal(152, blueprint.FeatureGroups.Sum(group => group.SecondaryItemCount));
        Assert.Contains(blueprint.FeatureGroups, group => group.Name == "基础运行" && group.PrimaryModuleCount == 10);
        Assert.Contains(blueprint.FeatureGroups, group => group.Name == "后勤服务" && group.PrimaryModuleCount == 8);
        Assert.Contains(blueprint.FeatureGroups, group => group.Name == "环境监管" && group.PrimaryModuleCount == 3);
        Assert.Contains(blueprint.FeatureGroups, group => group.Name == "综合管理" && group.PrimaryModuleCount == 8);
    }

    [Fact]
    public void LogisticsBlueprintDefinesOperationalClosedLoops()
    {
        var blueprint = _service.GetLogisticsBlueprint();

        Assert.Contains(blueprint.Workflows, workflow => workflow.Name == "服务闭环" && workflow.Steps.Contains("验收回访"));
        Assert.Contains(blueprint.Workflows, workflow => workflow.Name == "设施运行闭环" && workflow.Steps.Contains("生命周期记录"));
        Assert.Contains(blueprint.Workflows, workflow => workflow.Name == "环境监管闭环" && workflow.Steps.Contains("预警池"));
        Assert.Contains(blueprint.Workflows, workflow => workflow.Name == "质量成本闭环" && workflow.Steps.Contains("考核结算"));
    }
}
