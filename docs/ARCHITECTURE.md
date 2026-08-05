# Lemon 架构摘要

完整开发基准见 `DEVELOPMENT.md`。

Lemon 使用模块化单体和简化 Clean Architecture，仅保留四个后端项目：

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

- Api：HTTP、统一响应、认证授权、中间件、审计过滤器、Swagger。
- Application：用例、DTO、校验、错误码、分页、仓储接口、数据权限。
- Domain：实体、枚举和核心身份规则。
- Infrastructure：Dapper、MySQL、Redis、JWT、Quartz、迁移和对象存储。

权限分两层：

```text
功能权限：管理员 -> 单角色 -> 权限码
数据权限：角色范围 -> 主部门 / 主管部门 / 本人
```

管理员只有一个主部门；一个管理员可以主管多个部门。`role_id IS NULL` 唯一识别超级管理员。普通管理员的角色或主部门异常时采用 fail-closed，拒绝登录和访问。
