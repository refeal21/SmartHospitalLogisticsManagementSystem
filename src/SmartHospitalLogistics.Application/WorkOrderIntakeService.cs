using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record CreateExternalWorkOrderCommand(
    string WorkOrderNo,
    string Title,
    string ServiceType,
    Priority Priority,
    SpatialLocation Location,
    string ResponsibleTeam,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt,
    string CreatedBy,
    string Remark);

public sealed record ExternalWorkOrderCreationResult(
    bool Succeeded,
    string? ErrorMessage,
    WorkOrderDetail? Detail);

public interface IWorkOrderIntakeService
{
    ExternalWorkOrderCreationResult CreateExternalWorkOrder(CreateExternalWorkOrderCommand command);
}
