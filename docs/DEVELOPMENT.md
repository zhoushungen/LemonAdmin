# Lemon 开发文档

> 本文档是 Lemon 后续开发和结构性改动的唯一基准。代码与本文冲突时，先评审并更新本文，再修改代码。

## 1. 定位与原则

Lemon 面向中小型管理后台，采用：

```text
模块化单体 + 简化 Clean Architecture + 显式 Dapper SQL
```

原则：

- 一个服务、一次部署，暂不拆微服务。
- 保留清晰模块边界，但不为形式增加项目或抽象层。
- SQL、权限和数据范围保持显式，方便排查。
- 小规模对象映射使用构造函数或专用映射方法，不引入映射框架。
- 默认拒绝优于宽松兜底；异常角色、部门、存储配置必须失败。

## 2. 技术栈

后端：.NET 8、ASP.NET Core Web API、Dapper、MySQL 8、StackExchange.Redis、JWT、FluentValidation、Serilog、Quartz.NET、Swagger、DbUp。

前端：Vue 3、TypeScript、Vite、Pinia、Vue Router、Axios、Element Plus。

对象存储：Local、七牛云 Kodo；腾讯云 COS、阿里云 OSS 通过 `IObjectStorage` 扩展。

## 3. 后端目录与依赖

```text
src/
├─ Lemon.Api
├─ Lemon.Application
├─ Lemon.Domain
└─ Lemon.Infrastructure
```

依赖方向：

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

职责：

- `Lemon.Api`：Controller、统一响应、授权、过滤器、中间件、Swagger。
- `Lemon.Application`：业务用例、DTO、验证器、错误码、分页、仓储接口、数据范围。
- `Lemon.Domain`：实体和核心枚举。
- `Lemon.Infrastructure`：Dapper 仓储、MySQL、Redis、JWT、Quartz、对象存储和迁移。

不再建立 `Shared` 项目。只有多个层真正共同依赖且不能归属现有层的类型，才考虑新增项目。

## 4. 管理员、角色和部门

### 4.1 单角色、单主部门

```text
sys_admin_user.role_id
sys_admin_user.department_id
```

- `role_id IS NULL`：唯一超级管理员。
- 普通管理员必须绑定一个启用角色和一个启用主部门。
- 一个角色可被多个管理员复用。
- 角色名称和编码不参与超级管理员判断。
- 数据库限制最多一个空角色管理员，并约束普通管理员必须有部门。

统一判断：

```csharp
admin.IsSuperAdmin
currentUser.IsSuperAdmin
adminRepository.IsSuperAdminAsync(adminId)
```

### 4.2 部门主管

```text
sys_department.manager_admin_id
```

- 一个部门最多一个主管。
- 一个管理员可以主管多个部门。
- 主管关系不等于所属关系，管理员仍只有一个主部门。
- 主管必须是启用的普通管理员。
- 暂不增加管理员多部门中间表。

## 5. 功能权限与数据权限

功能权限决定“能否执行”，数据权限决定“可操作哪些数据”，两者必须同时通过。

### 5.1 功能权限

```text
管理员 -> role_id -> sys_role_permission -> sys_permission
```

普通接口使用：

```csharp
[RequirePermission("system.role.update")]
```

超级管理员专属接口使用：

```csharp
[RequireSuperAdmin]
```

JWT 只保存身份和账号切换所需声明，不保存权限集合。权限由 `IPermissionService` 从数据库读取并通过 Redis 短期缓存，角色变更后清理缓存。

### 5.2 数据范围

`sys_role.data_scope`：

| 值 | 枚举 | 范围 |
|---|---|---|
| 1 | `All` | 全部数据 |
| 2 | `DepartmentAndChildren` | 主部门及下级 |
| 3 | `Department` | 仅主部门 |
| 4 | `ManagedDepartments` | 主管部门及下级 |
| 5 | `Self` | 仅本人 |

超级管理员始终是全部数据。普通管理员角色缺失/禁用、主部门缺失/禁用时拒绝登录和访问，禁止默认成 `All`。

统一服务：

```csharp
IDataScopeService
```

实现规则：

- 每个 HTTP 请求解析一次。
- 不使用全局 SQL 拦截器。
- 仓储显式追加范围条件。
- 列表、详情、修改、删除、批量、导入和导出必须同时检查。
- 部门树较小时一次读取，在内存计算下级部门。

当前管理员、部门和审计日志已接入数据范围。以后带 `department_id` 或 `created_by` 的业务表按同样规则处理。

## 6. 账号切换

账号切换是超级管理员临时模拟普通管理员，不读取目标密码。

配置键：

```text
security.account_switch_enabled
```

要求：

- 目标账号、角色和主部门都必须启用。
- 模拟 Token 最长 15 分钟，不提供刷新令牌。
- 功能权限和数据权限完全按目标账号执行。
- Redis 保存会话；切回、退出、过期或关闭开关后失效。
- 审计同时记录当前身份与真实操作者。
- 前端持续显示模拟状态条。

## 7. 系统设置与前端开关

内置开关：

```text
security.account_switch_enabled
ui.theme_switch_enabled
ui.font_size_switch_enabled
```

系统设置只允许超级管理员修改。设置值必须按 `string/int/bool/decimal/json` 类型验证；设置键只允许受控字符。密钥不得保存到普通设置表。

## 8. 数据访问与迁移

- 使用 Dapper 参数化 SQL，禁止拼接用户输入。
- 不实现万能 `Repository<TEntity>`。
- 多表写操作使用事务。
- 分页上限 100。
- 时间统一 UTC。
- 已发布迁移不可修改；新迁移按编号递增。
- 生产环境由独立命令执行迁移，API 启动时禁止自动迁移。

当前迁移：

```text
001_system_schema.sql
002_seed_system.sql
003_single_role_and_impersonation.sql
004_department_data_scope.sql
```

v1.4 不修改数据库结构。

## 9. 缓存、Token 与任务

- Redis 缓存权限、系统开关和账号切换会话，不保存不可丢失主数据。
- Refresh Token 只保存 SHA-256 Hash，刷新后轮换。
- Access Token 不保存权限数组，减少 Token 体积和权限陈旧窗口。
- Quartz 当前只负责 Refresh Token 清理；新增任务必须幂等。
- 无 Redis 时开发环境可使用内存缓存；生产多实例必须启用 Redis。

## 10. 对象存储与文件

业务只依赖 `IObjectStorage`。当前 Provider：

```text
Local
Qiniu
```

未知 Provider 必须在启动时抛错，不能静默回退到 Local。数据库应保存对象键而不是只保存完整 URL。上传使用随机对象键、20MB 上限和扩展名白名单；高安全场景可进一步增加文件头检测和恶意内容扫描。

## 11. 前端通用能力

优先复用：

- `LemonTable`
- `LemonColumnSettings`
- `LemonImportDialog`
- `LemonSearchForm`
- `LemonPageHeader`
- `LemonThemeDrawer`

列表支持列拖动、字段显示、配置持久化、CSV 导入、按当前可见字段与顺序导出。CSV 导出必须防公式注入。localStorage 读取必须容错，损坏数据回退到默认值。

## 12. 审计与生产安全

- 写操作由 `AdminAuditActionFilter` 统一记录。
- 记录当前身份、真实操作者、部门快照、路径、方法、状态、IP、耗时和 TraceId。
- 密码、Token、密钥不得写日志。
- 客户端 Correlation ID 只接受有限长度和安全字符。
- 生产环境关闭 BootstrapAdmin、自动迁移和公开 Swagger。
- JWT 密钥至少 32 字节，通过环境变量注入。
- 超级管理员不能禁用或绑定普通角色。
- 普通管理员异常角色或部门一律 fail-closed。
