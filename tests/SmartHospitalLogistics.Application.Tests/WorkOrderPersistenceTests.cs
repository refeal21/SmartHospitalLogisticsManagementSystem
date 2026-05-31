using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class WorkOrderPersistenceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-logistics-{Guid.NewGuid():N}.db");

    [Fact]
    public void SqlitePersistenceRestoresConvertedWorkOrderAcrossServiceInstances()
    {
        var service = new WorkOrderDispatchService(new SqliteWorkOrderPersistence(_dbPath));
        var request = service.CreateServiceRequest(new CreateServiceRequestCommand(
            "Manual",
            "门诊护士站",
            "门诊部",
            "综合维修",
            Priority.High,
            "门诊大厅空调异常，候诊区温度偏高",
            new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "共享大厅", "BIM-OPD-F1-HALL")));
        var converted = service.ConvertServiceRequest(
            request.RequestNo,
            new ConvertServiceRequestCommand("一站式受理员", "信息完整，生成待派工单"));

        var reloaded = new WorkOrderDispatchService(new SqliteWorkOrderPersistence(_dbPath));
        var detail = reloaded.GetWorkOrderDetail(converted.Detail!.WorkOrder.WorkOrderNo);

        Assert.NotNull(detail);
        Assert.Equal(WorkOrderStatus.New, detail.WorkOrder.Status);
        Assert.Equal("门诊医技楼", detail.Location.Building);
        Assert.Contains(detail.Timeline, entry => entry.Action == "受理建单");
    }

    [Fact]
    public void SqlitePersistenceRestoresLifecycleTransitionsAcrossServiceInstances()
    {
        var service = new WorkOrderDispatchService(new SqliteWorkOrderPersistence(_dbPath));
        service.Dispatch(
            "WO-20260530-0001",
            new DispatchWorkOrderCommand("环境监管班组", "调度员", "医废负压风险优先派工"));
        service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Accept, "环境监管班组", "已到现场"));
        service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Complete, "环境监管班组", "负压恢复正常"));
        service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.AcceptCompletion, "总务处管理者", "验收通过"));
        service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Evaluate, "护士站", "响应及时", Rating: 5));

        var reloaded = new WorkOrderDispatchService(new SqliteWorkOrderPersistence(_dbPath));
        var detail = reloaded.GetWorkOrderDetail("WO-20260530-0001");

        Assert.NotNull(detail);
        Assert.Equal(WorkOrderStatus.Closed, detail.WorkOrder.Status);
        Assert.Contains(detail.Timeline, entry => entry.Action == "评价" && entry.Rating == 5);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
