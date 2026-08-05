# AGENTS.md

## 开发基准

1. 修改代码前先阅读 `docs/DEVELOPMENT.md`；架构变化先更新文档，再修改代码。
2. 后端固定为四项目：`Lemon.Api`、`Lemon.Application`、`Lemon.Domain`、`Lemon.Infrastructure`；不要为少量通用类型再拆项目。
3. 保持模块化单体、显式 Dapper SQL 和显式对象映射；不引入万能仓储、微服务、事件总线或无必要依赖。
4. 管理员只有一个角色和一个主部门；`sys_admin_user.role_id IS NULL` 表示唯一超级管理员。
5. 超管专属接口使用 `[RequireSuperAdmin]`；普通功能权限使用 `[RequirePermission("...")]`。
6. 数据权限由 `IDataScopeService` 解析，并在列表、详情、修改、删除、导入和导出中显式应用；异常角色或部门必须拒绝访问。
7. 一个管理员可主管多个部门，但暂不增加管理员多部门关系表。
8. 账号切换后按目标账号权限和数据范围执行，并保留真实操作者审计信息。
9. 数据库变更只能新增迁移文件，不修改已发布迁移。
10. 前端列表优先复用 `LemonTable`；不要重复实现列拖动、字段显示和导入导出。
11. 新增依赖前检查免费商用许可证，并更新 `THIRD-PARTY-NOTICES.md`。
12. 密钥、密码和 Token 不得写入仓库；生产环境通过环境变量注入。

## 提交前检查

```bash
bash scripts/validate-package.sh
cd lemon-ui && npm ci && npm run build && npm audit --omit=dev
# 安装 .NET 8 SDK 后：
dotnet restore Lemon.sln
dotnet build Lemon.sln -c Release
```
