# Lemon 通用前后端框架 v1.4

Lemon 是一个面向中小型管理后台的轻量通用框架，强调简洁、高效、稳定和可扩展。

## 技术栈

- 后端：.NET 8、ASP.NET Core Web API、Dapper、MySQL 8、Redis、JWT
- 通用能力：FluentValidation、Serilog、Quartz.NET、Swagger、DbUp
- 前端：`lemon-ui`，Vue 3、TypeScript、Vite、Pinia、Element Plus
- 对象存储：Local、七牛云 Kodo；可按 `IObjectStorage` 扩展 COS、OSS

## v1.4 瘦身结果

- 后端从 5 个项目精简为 4 个：`Api / Application / Domain / Infrastructure`。
- 删除仅用于转发类型的 `Lemon.Shared`，统一响应留在 Api，错误码、异常和分页放入 Application。
- 删除 Mapster，当前映射规模直接使用显式构造，减少依赖和隐藏行为。
- JWT 不再重复保存权限集合；权限始终通过数据库与 Redis 校验。
- 删除无调用的服务和仓储方法，整理依赖所属项目与重复授权注册。
- 普通管理员角色或主部门缺失、禁用时一律拒绝访问，不再使用宽松默认值。
- 未知对象存储 Provider 启动即报错，避免生产环境误落到本地存储。
- 加固账号切换、菜单树、密码哈希、Correlation ID 和前端本地配置读取。

原有通用能力继续保留：单角色、主部门、部门主管、五档数据权限、超级管理员账号切换、系统开关、双重身份审计、通用表格、列拖动、字段显示、导入导出、字号和主题切换。

## 启动

```bash
docker compose up -d --build
```

开发地址：

- 前端：http://localhost:5173
- Swagger：http://localhost:5080/swagger

开发初始化账号：

```text
账号：admin
密码：由环境变量 LEMON_ADMIN_PASSWORD 注入（未配置时使用 Bootstrap 阶段生成的随机密码，记录在启动日志）
```

该账号只用于开发环境。生产环境必须关闭自动迁移、BootstrapAdmin 和公开 Swagger，并使用环境变量注入随机 JWT 密钥，禁止在仓库中写入任何明文密码或 Token。

分开启动：

```bash
dotnet restore Lemon.sln
dotnet run --project src/Lemon.Api

cd lemon-ui
npm ci
npm run dev
```

开始修改前请先阅读 `AGENTS.md` 和 `docs/DEVELOPMENT.md`。
