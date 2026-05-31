using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class WorkOrderDispatchServiceTests
{
    private readonly WorkOrderDispatchService _service = new();

    [Fact]
    public void DispatchBoardOrdersWorkOrdersByPriorityAndSlaRisk()
    {
        var board = _service.GetDispatchBoard();

        Assert.NotEmpty(board.WorkOrders);
        Assert.Equal("WO-20260530-0001", board.WorkOrders[0].WorkOrderNo);
        Assert.Equal(Priority.Critical, board.WorkOrders[0].Priority);
        Assert.Equal("High", board.SlaRisk.HighestRiskLevel);
        Assert.Contains(board.Recommendations, recommendation =>
            recommendation.WorkOrderNo == "WO-20260530-0001" &&
            recommendation.RecommendedTeam == "环境监管班组");
    }

    [Fact]
    public void DispatchAssignsTeamChangesStatusAndAddsTimelineEntry()
    {
        var result = _service.Dispatch(
            "WO-20260530-0001",
            new DispatchWorkOrderCommand("环境监管班组", "调度员", "医废负压风险优先派工"));

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(result.Detail);
        Assert.Equal(WorkOrderStatus.Dispatched, result.Detail.WorkOrder.Status);
        Assert.Equal("环境监管班组", result.Detail.WorkOrder.ResponsibleTeam);
        Assert.Contains(result.Detail.Timeline, entry =>
            entry.Action == "派工" &&
            entry.ToStatus == WorkOrderStatus.Dispatched &&
            entry.Operator == "调度员");
    }

    [Fact]
    public void WorkOrderLifecycleSupportsCoreOneStopServiceTransitions()
    {
        _service.Dispatch(
            "WO-20260530-0001",
            new DispatchWorkOrderCommand("环境监管班组", "调度员", "医废负压风险优先派工"));

        var accepted = _service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Accept, "环境监管班组", "已到现场"));
        var suspended = _service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Suspend, "环境监管班组", "等待备件"));
        var transferred = _service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Transfer, "调度员", "需要电工协同", "综合维修班"));
        var completed = _service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Complete, "综合维修班", "负压恢复正常"));
        var acceptedCompletion = _service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.AcceptCompletion, "总务处管理者", "验收通过"));
        var evaluated = _service.Transition(
            "WO-20260530-0001",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Evaluate, "护士站", "响应及时", Rating: 5));

        Assert.Equal(WorkOrderStatus.Accepted, accepted.Detail?.WorkOrder.Status);
        Assert.Equal(WorkOrderStatus.Suspended, suspended.Detail?.WorkOrder.Status);
        Assert.Equal(WorkOrderStatus.Transferred, transferred.Detail?.WorkOrder.Status);
        Assert.Equal(WorkOrderStatus.PendingAcceptance, completed.Detail?.WorkOrder.Status);
        Assert.Equal(WorkOrderStatus.PendingEvaluation, acceptedCompletion.Detail?.WorkOrder.Status);
        Assert.Equal(WorkOrderStatus.Closed, evaluated.Detail?.WorkOrder.Status);
        Assert.Contains(evaluated.Detail!.Timeline, entry => entry.Action == "评价" && entry.Rating == 5);
    }

    [Fact]
    public void EvaluationIsAllowedOnlyAfterAcceptanceReview()
    {
        var beforeAcceptance = _service.Transition(
            "WO-20260530-0004",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Evaluate, "护士站", "未验收不能评价", Rating: 3));

        var acceptedCompletion = _service.Transition(
            "WO-20260530-0004",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.AcceptCompletion, "总务处管理者", "验收通过"));

        var evaluated = _service.Transition(
            "WO-20260530-0004",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Evaluate, "护士站", "效果满意", Rating: 5));

        Assert.False(beforeAcceptance.Succeeded);
        Assert.Equal(WorkOrderStatus.PendingEvaluation, acceptedCompletion.Detail?.WorkOrder.Status);
        Assert.True(evaluated.Succeeded, evaluated.ErrorMessage);
        Assert.Equal(WorkOrderStatus.Closed, evaluated.Detail?.WorkOrder.Status);
    }

    [Fact]
    public void InvalidTransitionReturnsFailureAndDoesNotModifyWorkOrder()
    {
        var before = _service.GetWorkOrderDetail("WO-20260530-0002");

        var result = _service.Transition(
            "WO-20260530-0002",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Evaluate, "护士站", "不能提前评价", Rating: 3));

        var after = _service.GetWorkOrderDetail("WO-20260530-0002");

        Assert.False(result.Succeeded);
        Assert.Equal(before?.WorkOrder.Status, after?.WorkOrder.Status);
        Assert.DoesNotContain(after!.Timeline, entry => entry.Action == "评价");
    }

    [Fact]
    public void WorkOrderDetailKeepsSourceEvidenceTraceableToNonAiMaterials()
    {
        var detail = _service.GetWorkOrderDetail("WO-20260530-0001");

        Assert.NotNull(detail);
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.FeatureName == "工单全流程管理" &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
        Assert.DoesNotContain(detail.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 && evidence.Sources.Contains("AI补充"));
    }
}
