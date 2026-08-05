namespace Lemon.Application.Modules.Departments;

public sealed record DepartmentDto(
    long Id,
    long? ParentId,
    long? ManagerAdminId,
    string? ManagerName,
    string Name,
    string Code,
    string? Phone,
    string? Email,
    int Sort,
    bool IsEnabled);

public sealed record SaveDepartmentRequest(
    long? ParentId,
    long? ManagerAdminId,
    string Name,
    string Code,
    string? Phone,
    string? Email,
    int Sort,
    bool IsEnabled);
