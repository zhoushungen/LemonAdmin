INSERT IGNORE INTO sys_department(id,parent_id,name,code,sort,is_enabled) VALUES (1,NULL,'总部','HQ',0,1);
INSERT IGNORE INTO sys_role(code,name,description,is_system,is_super_admin,is_enabled) VALUES ('super_admin','超级管理员','系统内置且不可修改，拥有全部菜单和接口权限',1,1,1);
INSERT IGNORE INTO sys_permission(code,name,module) VALUES
('system.dashboard.read','查看工作台','system'),
('system.admin.read','查看管理员','system'),('system.admin.create','新增管理员','system'),('system.admin.update','修改管理员','system'),
('system.department.read','查看部门','system'),('system.department.create','新增部门','system'),('system.department.update','修改部门','system'),('system.department.delete','删除部门','system'),
('system.role.read','查看角色权限','system'),('system.role.create','新增角色','system'),('system.role.update','修改角色权限','system'),
('system.menu.read','查看菜单','system'),('system.menu.create','新增菜单','system'),('system.menu.update','修改菜单','system'),('system.menu.delete','删除菜单','system'),
('system.setting.read','查看系统设置','system'),('system.setting.update','修改系统设置','system'),('system.audit.read','查看审计日志','system'),('system.file.upload','上传文件','system');
INSERT IGNORE INTO sys_menu(id,parent_id,name,menu_type,route_name,route_path,component,icon,permission_code,sort) VALUES
(1,NULL,'工作台','page','dashboard','/dashboard','views/dashboard/index.vue','HomeFilled','system.dashboard.read',10),
(10,NULL,'系统管理','directory','system','/system',NULL,'Setting',NULL,100),
(11,10,'管理员管理','page','admins','/system/admins','views/system/admins/index.vue','User','system.admin.read',110),
(12,10,'部门管理','page','departments','/system/departments','views/system/departments/index.vue','OfficeBuilding','system.department.read',120),
(13,10,'角色权限','page','roles','/system/roles','views/system/roles/index.vue','Key','system.role.read',130),
(14,10,'菜单管理','page','menus','/system/menus','views/system/menus/index.vue','Menu','system.menu.read',140),
(15,10,'系统设置','page','settings','/system/settings','views/system/settings/index.vue','Tools','system.setting.read',150),
(16,10,'审计日志','page','audit-logs','/system/audit-logs','views/system/audit-logs/index.vue','Document','system.audit.read',160);
INSERT IGNORE INTO sys_setting(setting_group,setting_key,setting_value,value_type,description,is_public) VALUES
('system','system.site_name','Lemon 管理后台','string','后台系统名称',1),('system','system.logo_text','Lemon','string','后台 Logo 文字',1),('system','system.registration_enabled','false','bool','是否开放注册',0),('storage','storage.max_file_size_mb','20','int','上传文件最大尺寸',0);
