namespace Lemon.Domain.System;

/// <summary>角色数据范围。超级管理员不读取此字段，始终拥有全部数据。</summary>
public enum DataScopeType : byte
{
    All = 1,
    DepartmentAndChildren = 2,
    Department = 3,
    ManagedDepartments = 4,
    Self = 5
}
