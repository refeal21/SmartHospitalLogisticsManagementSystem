using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record WorkOrderPersistenceSnapshot(
    IReadOnlyList<WorkOrder> WorkOrders,
    IReadOnlyList<ServiceRequest> ServiceRequests,
    IReadOnlyDictionary<string, IReadOnlyList<WorkOrderTimelineEntry>> Timeline);

public interface IWorkOrderPersistence
{
    WorkOrderPersistenceSnapshot Load();

    void SaveServiceRequest(ServiceRequest request);

    void SaveWorkOrder(WorkOrder workOrder);

    void SaveTimelineEntry(string workOrderNo, WorkOrderTimelineEntry entry);

    void SaveWorkOrderTransition(WorkOrder workOrder, WorkOrderTimelineEntry entry);

    void SaveServiceRequestConversion(ServiceRequest request, WorkOrder workOrder, WorkOrderTimelineEntry entry);
}
