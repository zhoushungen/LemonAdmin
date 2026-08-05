# Lemon v1.4 升级说明

v1.4 是代码结构和安全默认值的精简版本，**没有数据库结构变更，也没有新增 SQL 迁移**。

升级步骤：

1. 备份现有配置和数据库。
2. 替换后端与 `lemon-ui` 源码。
3. 删除旧构建缓存后重新还原依赖并构建。
4. 检查 `Storage:Provider`，只允许 `Local` 或 `Qiniu`；错误值将阻止应用启动。
5. 多实例生产环境确认 Redis 已启用。
6. 验证普通管理员所绑定角色和主部门均存在且启用；异常账号在 v1.4 会被拒绝登录。

代码引用变化：

- `Lemon.Shared.*` 已移除。
- `AppException`、`ErrorCodes`、`PagedResult` 位于 `Lemon.Application.Common`。
- `ApiResponse` 位于 `Lemon.Api.Contracts`。
- Mapster 已移除，使用显式映射。
