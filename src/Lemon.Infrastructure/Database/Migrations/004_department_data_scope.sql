-- Lemon v1.3：简洁的部门数据权限。
-- 管理员仍然只有一个主部门；一个管理员可作为多个部门的主管。

ALTER TABLE sys_role
    ADD COLUMN data_scope TINYINT UNSIGNED NOT NULL DEFAULT 5 AFTER description;

UPDATE sys_role SET data_scope=5 WHERE data_scope NOT BETWEEN 1 AND 5;

ALTER TABLE sys_department
    ADD COLUMN manager_admin_id BIGINT NULL AFTER parent_id,
    ADD KEY idx_department_manager(manager_admin_id),
    ADD CONSTRAINT fk_department_manager FOREIGN KEY(manager_admin_id) REFERENCES sys_admin_user(id) ON DELETE SET NULL;

-- 旧数据中普通管理员没有部门时，统一归入总部，避免数据权限无法计算。
UPDATE sys_admin_user
SET department_id=1
WHERE role_id IS NOT NULL AND department_id IS NULL;

ALTER TABLE sys_admin_user
    ADD CONSTRAINT chk_admin_department_required
    CHECK (role_id IS NULL OR department_id IS NOT NULL);

ALTER TABLE sys_audit_log
    ADD COLUMN department_id BIGINT NULL AFTER admin_user_id,
    ADD KEY idx_audit_department_time(department_id,created_at);

UPDATE sys_audit_log l
LEFT JOIN sys_admin_user a ON a.id=l.admin_user_id
SET l.department_id=a.department_id
WHERE l.department_id IS NULL;
