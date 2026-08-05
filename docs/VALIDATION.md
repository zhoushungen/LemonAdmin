# 验证说明

提交前执行：

```bash
bash scripts/validate-package.sh
cd lemon-ui
npm ci
npm run build
npm audit --omit=dev
```

安装 .NET 8 SDK 后补充：

```bash
dotnet restore Lemon.sln
dotnet build Lemon.sln -c Release
```

重点回归：

1. 超级管理员拥有全部功能与数据权限。
2. 普通管理员绑定角色或主部门缺失、禁用时拒绝登录、刷新和访问。
3. `Self` 只能看到本人及相关审计日志。
4. 部门、部门及下级、主管部门及下级范围正确。
5. 管理员详情和部门写操作直接输入越权 ID 时返回 403。
6. 账号切换目标的账号、角色、主部门必须启用；切换后按目标权限执行。
7. 关闭账号切换开关或结束会话后，旧模拟 Token 失效。
8. 未知 `Storage:Provider` 阻止启动，不回退到 Local。
9. 损坏的 localStorage 配置不会阻止前端启动。
