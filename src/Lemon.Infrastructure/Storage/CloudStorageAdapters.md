# COS / OSS 适配说明

框架通过 `IObjectStorage` 隔离厂商 SDK，当前默认提供 `LocalObjectStorage`，便于本地开发。

生产接入时新增：

- `TencentCosObjectStorage`：引用腾讯云 COS .NET SDK，实现上传、删除、临时签名 URL。
- `AliyunOssObjectStorage`：引用阿里云 OSS .NET SDK，实现相同接口。

业务模块只依赖 `IObjectStorage`，切换厂商无需修改业务代码。
