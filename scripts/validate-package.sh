#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

for file in \
  AGENTS.md README.md Lemon.sln framework-manifest.json \
  docs/DEVELOPMENT.md docs/CODE_REVIEW.md docs/MIGRATION_V1.4.md \
  lemon-ui/package.json; do
  test -f "$file"
done

test -d lemon-ui
test ! -d src/Lemon.Shared

python - <<'PYCODE'
import json
import pathlib
import re
import xml.etree.ElementTree as ET

root = pathlib.Path('.')
manifest = json.loads((root / 'framework-manifest.json').read_text())
if manifest.get('version') != '1.4.0':
    raise SystemExit('framework-manifest 版本不是 1.4.0')

for path in [
    root / 'src/Lemon.Api/appsettings.json',
    root / 'src/Lemon.Api/appsettings.Development.json',
    root / 'lemon-ui/package.json',
    root / 'lemon-ui/package-lock.json',
]:
    json.loads(path.read_text())

projects = list(root.joinpath('src').glob('*/*.csproj'))
if len(projects) != 4:
    raise SystemExit(f'后端应保持四项目，实际为 {len(projects)}')
for path in projects:
    tree = ET.parse(path)
    for reference in tree.findall('.//ProjectReference'):
        target = (path.parent / reference.attrib['Include'].replace('\\', '/')).resolve()
        if not target.exists():
            raise SystemExit(f'项目引用不存在: {path} -> {target}')

required = [
    root / 'src/Lemon.Infrastructure/Database/Migrations/003_single_role_and_impersonation.sql',
    root / 'src/Lemon.Infrastructure/Database/Migrations/004_department_data_scope.sql',
    root / 'src/Lemon.Api/Authorization/RequireSuperAdminAttribute.cs',
    root / 'src/Lemon.Application/Modules/Impersonation/ImpersonationService.cs',
    root / 'src/Lemon.Application/Modules/DataScopes/DataScopeService.cs',
    root / 'src/Lemon.Application/Abstractions/Security/IDataScopeService.cs',
    root / 'src/Lemon.Domain/System/DataScopeType.cs',
    root / 'lemon-ui/src/utils/storage.ts',
]
missing = [str(path) for path in required if not path.exists()]
if missing:
    raise SystemExit(f'缺少必要文件: {missing}')

if list((root / 'lemon-ui/src').rglob('*.js')):
    raise SystemExit('lemon-ui/src 存在误生成 JavaScript 文件')

login_page = (root / 'lemon-ui/src/views/login/index.vue').read_text()
if 'ChangeMe_123456' in login_page or "username: 'admin'" in login_page:
    raise SystemExit('登录页仍硬编码开发账号或密码')

all_text = '\n'.join(
    path.read_text(errors='ignore')
    for path in root.rglob('*')
    if path.is_file() and path.suffix.lower() in {'.cs', '.csproj', '.props'}
)
for forbidden in ['Lemon.Shared', 'Mapster']:
    if forbidden in all_text:
        raise SystemExit(f'发现已移除依赖或项目引用: {forbidden}')

admin_repo = (root / 'src/Lemon.Infrastructure/Repositories/AdminRepository.cs').read_text()
for token in ['COALESCE(r.data_scope,5)', 'role_is_enabled', 'department_is_enabled', 'DataScopeType.Self']:
    if token not in admin_repo:
        raise SystemExit(f'管理员 fail-closed / 数据范围实现缺少: {token}')

scope_service = (root / 'src/Lemon.Application/Modules/DataScopes/DataScopeService.cs').read_text()
for token in ['!profile.RoleIsEnabled', '!profile.DepartmentIsEnabled', 'DataScopeContext.SuperAdmin']:
    if token not in scope_service:
        raise SystemExit(f'数据范围安全校验缺少: {token}')

impersonation = (root / 'src/Lemon.Application/Modules/Impersonation/ImpersonationService.cs').read_text()
for token in ['目标管理员角色不存在或已禁用', '目标管理员主部门不存在或已禁用']:
    if token not in impersonation:
        raise SystemExit(f'账号切换目标校验缺少: {token}')

jwt = (root / 'src/Lemon.Infrastructure/Security/JwtTokenService.cs').read_text()
if 'permission' in jwt.lower():
    raise SystemExit('JWT 中仍存在权限集合声明')

refresh_repo = (root / 'src/Lemon.Infrastructure/Repositories/RefreshTokenRepository.cs').read_text()
for token in ['revoked_at IS NULL', 'expires_at>UTC_TIMESTAMP()', 'updated != 1']:
    if token not in refresh_repo:
        raise SystemExit(f'Refresh Token 并发轮换保护缺少: {token}')

permission_service = (root / 'src/Lemon.Application/Modules/Permissions/PermissionService.cs').read_text()
for token in ['GetAccessProfileAsync', '!profile.RoleIsEnabled', '!profile.DepartmentIsEnabled']:
    if token not in permission_service:
        raise SystemExit(f'权限入口账号状态校验缺少: {token}')

storage_di = (root / 'src/Lemon.Infrastructure/DependencyInjection.cs').read_text()
if '不支持的对象存储 Provider' not in storage_di:
    raise SystemExit('对象存储 Provider 未采用 fail-fast')

migration = (root / 'src/Lemon.Infrastructure/Database/Migrations/004_department_data_scope.sql').read_text()
for token in ['data_scope', 'manager_admin_id', 'department_id', 'chk_admin_department_required']:
    if token not in migration:
        raise SystemExit(f'数据权限迁移缺少: {token}')

for cs in root.joinpath('src').rglob('*.cs'):
    text = cs.read_text(errors='ignore')
    if 'sys_admin_user_role' in text or 'GetRoleIdsAsync' in text:
        raise SystemExit(f'发现旧多角色代码: {cs}')

    stripped = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    stripped = re.sub(r'//.*', '', stripped)
    stripped = re.sub(r'""".*?"""', '""', stripped, flags=re.S)
    stripped = re.sub(r'@?"(?:""|\\.|[^"\\])*"', '""', stripped, flags=re.S)
    for left, right in [('(', ')'), ('{', '}'), ('[', ']')]:
        if stripped.count(left) != stripped.count(right):
            raise SystemExit(f'C# 括号不平衡 {left}{right}: {cs}')

migrations = sorted(path.name for path in (root / 'src/Lemon.Infrastructure/Database/Migrations').glob('*.sql'))
prefixes = [name.split('_', 1)[0] for name in migrations]
if len(prefixes) != len(set(prefixes)):
    raise SystemExit('数据库迁移编号重复')

print('四项目结构、JSON/XML、权限、数据范围、迁移和 C# 结构校验通过')
PYCODE

if command -v dotnet >/dev/null; then
  dotnet restore Lemon.sln
  dotnet build Lemon.sln -c Release --no-restore
else
  echo '跳过 dotnet build：未安装 .NET SDK'
fi

echo 'Lemon 包结构校验完成'
