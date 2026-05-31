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
        Assert.Contains("医废暂存间", activeOrders[0].Title);
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

    [Fact]
    public void LogisticsBlueprintKeepsFeatureEvidenceTraceableToSourceMaterials()
    {
        var blueprint = _service.GetLogisticsBlueprint();

        Assert.Contains(blueprint.FeatureEvidence, item =>
            item.FeatureName == "工单全流程管理" &&
            item.Sources.Contains("中科医信") &&
            item.Sources.Contains("PPT"));
        Assert.Contains(blueprint.FeatureEvidence, item =>
            item.FeatureName == "供配电监测" &&
            item.Sources.Contains("北建院") &&
            item.Sources.Contains("中科医信"));
        Assert.DoesNotContain(blueprint.FeatureEvidence, item => item.Sources.Contains("AI补充") && item.Sources.Count == 1);
    }

    [Fact]
    public void LogisticsBlueprintIncludesBeijianyuanCustomerDataCatalog()
    {
        var blueprint = _service.GetLogisticsBlueprint();

        Assert.Equal(8, blueprint.CustomerDataSystems.Count);
        Assert.Contains(blueprint.CustomerDataSystems, system =>
            system.Major == "强电系统" &&
            system.Subsystems.Contains("变电室智能配电系统") &&
            system.DataFields.Any(field => field.Contains("电压")));
        Assert.Contains(blueprint.CustomerDataSystems, system =>
            system.Major == "供暖空调系统" &&
            system.Subsystems.Contains("冷源及空调水系统") &&
            system.Roles.Contains("暖通班工作人员"));
        Assert.Contains(blueprint.CustomerDataSystems, system =>
            system.Major == "其他物联设备" &&
            system.Subsystems.Contains("智慧卫生间"));
    }

    [Fact]
    public void LogisticsBlueprintIncludesCompetitorFeatureCatalogAndExecutionSequence()
    {
        var blueprint = _service.GetLogisticsBlueprint();

        Assert.True(blueprint.CompetitorModules.Count >= 20);
        Assert.Contains(blueprint.CompetitorModules, module =>
            module.Name == "智慧医院运行保障系统基础服务模块" &&
            module.FeatureAreas.Contains("个人工作台") &&
            module.FeatureAreas.Contains("工单管理"));
        Assert.Contains(blueprint.CompetitorModules, module =>
            module.Name == "医疗废弃物综合管理系统" &&
            module.FeatureAreas.Contains("医废全生命周期监管"));
        Assert.Equal("基础平台与工作台", blueprint.ImplementationPhases[0].Name);
        Assert.Equal("综合管理", blueprint.ImplementationPhases[^1].Name);
    }
}
