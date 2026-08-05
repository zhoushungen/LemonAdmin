# Lemon v1.3 数据权限迁移

## 变更内容

- `sys_role.data_scope`：角色数据范围，取值：
  - `1` 全部数据
  - `2` 主部门及下级部门
  - `3` 仅主部门
  - `4` 主管部门及下级部门
  - `5` 仅本人
- `sys_department.manager_admin_id`：部门主管，一个管理员可以主管多个部门。
- `sys_audit_log.department_id`：记录操作发生时当前生效账号的部门快照。
- 普通管理员必须具有主部门；超级管理员仍由 `sys_admin_user.role_id IS NULL` 判断。

## 迁移策略

旧数据中 `role_id IS NOT NULL` 但 `department_id IS NULL` 的账号，会自动归入部门 ID `1`（总部）。迁移完成后由数据库检查约束保证普通管理员必须有主部门。

## 注意事项

- 不增加管理员多部门关系表。
- 管理多个部门通过 `sys_department.manager_admin_id` 实现。
- 数据权限不会替代功能权限，两者必须同时通过。
- Dapper 查询显式应用数据范围，不使用全局 SQL 拦截器。
