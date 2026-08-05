# v1.1 升级到 v1.2

1. 先完整备份 MySQL 数据库。
2. 在测试环境执行 DbUp 迁移 `003_single_role_and_impersonation.sql`。
3. 检查 `sys_admin_user.role_id`：只能有一个账号为空。
4. 检查旧多角色管理员：迁移后只保留一个普通角色。
5. 登录超级管理员，在系统设置中按需开启：
   - `security.account_switch_enabled`
   - `ui.theme_switch_enabled`
   - `ui.font_size_switch_enabled`
6. 清理旧权限缓存并重新登录。

迁移不会把普通无角色账号自动提升为超级管理员；它们会绑定 `basic_admin`，默认无权限。
