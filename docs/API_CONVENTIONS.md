# API 约定速查

- 管理端路由：`/api/admin/v1`
- 认证：`Authorization: Bearer <token>`
- 权限码：`模块.资源.动作`，例如 `system.department.read`
- 分页参数：`pageIndex`、`pageSize`
- 时间：UTC ISO 8601
- 成功业务码：`0`
- 所有写接口保留审计日志，敏感字段不入日志。
