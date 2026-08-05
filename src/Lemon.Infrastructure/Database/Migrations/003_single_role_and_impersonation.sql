-- 将 v1.1 的“管理员-多角色”收紧为“管理员-单角色”。
-- 只有原 super_admin 角色成员会迁移为 role_id=NULL；其他无角色账号会自动绑定 basic_admin，避免意外提权。

INSERT IGNORE INTO sys_role(code,name,description,is_system,is_super_admin,is_enabled,created_at)
VALUES('basic_admin','基础管理员','迁移兜底角色，默认不含任何权限',1,0,1,UTC_TIMESTAMP());

ALTER TABLE sys_admin_user ADD COLUMN role_id BIGINT NULL AFTER department_id;

SET @basic_role_id = (SELECT id FROM sys_role WHERE code='basic_admin' LIMIT 1);

SET @super_admin_id = (
    SELECT MIN(ar.admin_user_id)
    FROM sys_admin_user_role ar
    JOIN sys_role r ON r.id=ar.role_id
    WHERE r.is_super_admin=1
);

UPDATE sys_admin_user a
LEFT JOIN (
    SELECT ar.admin_user_id,
           MIN(CASE WHEN r.is_super_admin=0 THEN ar.role_id END) AS ordinary_role_id
    FROM sys_admin_user_role ar
    JOIN sys_role r ON r.id=ar.role_id
    GROUP BY ar.admin_user_id
) old_roles ON old_roles.admin_user_id=a.id
SET a.role_id = CASE
    WHEN a.id=@super_admin_id THEN NULL
    ELSE COALESCE(old_roles.ordinary_role_id,@basic_role_id)
END;

DELETE rp FROM sys_role_permission rp
JOIN sys_role r ON r.id=rp.role_id
WHERE r.is_super_admin=1;

DROP TABLE sys_admin_user_role;
DELETE FROM sys_role WHERE is_super_admin=1;

ALTER TABLE sys_role DROP INDEX idx_role_super;
ALTER TABLE sys_role DROP COLUMN is_super_admin;

ALTER TABLE sys_admin_user
    ADD COLUMN super_admin_guard TINYINT GENERATED ALWAYS AS (CASE WHEN role_id IS NULL THEN 1 ELSE NULL END) STORED,
    ADD UNIQUE KEY uk_single_super_admin(super_admin_guard),
    ADD KEY idx_admin_role(role_id),
    ADD CONSTRAINT fk_admin_role FOREIGN KEY(role_id) REFERENCES sys_role(id) ON DELETE RESTRICT;


DELETE rp FROM sys_role_permission rp
JOIN sys_permission p ON p.id=rp.permission_id
WHERE p.code IN ('system.admin.create','system.admin.update','system.setting.update');
DELETE FROM sys_permission
WHERE code IN ('system.admin.create','system.admin.update','system.setting.update');

ALTER TABLE sys_audit_log
    ADD COLUMN actor_admin_user_id BIGINT NULL AFTER admin_user_id,
    ADD COLUMN is_impersonating TINYINT(1) NOT NULL DEFAULT 0 AFTER actor_admin_user_id,
    ADD KEY idx_audit_actor_time(actor_admin_user_id,created_at);

INSERT IGNORE INTO sys_setting(setting_group,setting_key,setting_value,value_type,description,is_public)
VALUES
('security','security.account_switch_enabled','false','bool','是否允许超级管理员切换到其他后台账号',1),
('ui','ui.theme_switch_enabled','true','bool','是否允许用户切换后台配色方案',1),
('ui','ui.font_size_switch_enabled','true','bool','是否允许用户切换后台整体字号',1);

