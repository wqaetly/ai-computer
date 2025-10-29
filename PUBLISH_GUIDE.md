# AI Computer 发布指南

## 快速发布

### 方法 1: 使用发布脚本（推荐）
```bash
publish.bat
```

### 方法 2: 手动命令
```bash
# Windows x64 (自包含)
dotnet publish -c Release -r win-x64 --self-contained true

# Windows x64 (框架依赖，需要用户安装 .NET 9)
dotnet publish -c Release -r win-x64 --self-contained false

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true
```

## 发布配置说明

当前项目已配置以下优化（仅在 Release 模式生效）：

### ✅ 已启用的优化
- **代码裁剪** (`PublishTrimmed=true`): 移除未使用的代码
- **部分裁剪模式** (`TrimMode=partial`): 更安全，避免破坏反射
- **ReadyToRun 编译**: 加快应用启动速度
- **IL 压缩**: 压缩中间语言代码
- **移除调试符号**: 不包含 .pdb 文件

### 🔒 受保护的程序集
以下程序集不会被裁剪（配置在 `TrimmerRoots.xml`）：
- Avalonia 核心库
- SukiUI 主题库
- ViewModels 和 Views（反射使用）
- Emgu.CV（图像处理）
- ONNX Runtime（机器学习）
- PuppeteerSharp（浏览器自动化）
- 其他关键依赖

## 预期文件大小

| 模式 | 大约体积 | 优点 | 缺点 |
|------|----------|------|------|
| **自包含 + 裁剪** | ~80-120 MB | 无需安装 .NET，体积适中 | 比框架依赖大 |
| 框架依赖 | ~10-20 MB | 体积最小 | 用户需要安装 .NET 9 |

## 进一步优化体积的方法

### 1. 调整裁剪配置
编辑 `TrimmerRoots.xml`，移除不需要保护的程序集：

```xml
<!-- 如果不需要 SukiUI.Dock，可以删除这行 -->
<assembly fullname="SukiUI.Dock" preserve="all" />
```

### 2. 移除未使用的 NuGet 包
检查 `AiComputer.csproj`，移除不使用的包：
- 如果不需要 Markdown 支持，可删除 `LiveMarkdown.Avalonia`
- 如果不需要 YAML 支持，可删除 `YamlDotNet`

### 3. 使用完全裁剪模式（高级）
⚠️ **警告**: 可能导致运行时错误，需要充分测试

修改 `AiComputer.csproj`:
```xml
<TrimMode>full</TrimMode>
```

### 4. 压缩发布输出
使用 7-Zip 或 WinRAR 压缩 publish 目录：
- 通常可压缩到原大小的 40-60%
- 分发时提供压缩包，用户解压后使用

## 测试清单

发布后请务必测试以下功能：

- [ ] 应用正常启动
- [ ] 主题切换正常
- [ ] ViewModels 绑定正常
- [ ] 图标库显示正常
- [ ] OCR 功能正常
- [ ] 浏览器自动化功能正常
- [ ] 京东/拼多多 API 功能正常

## 常见问题

### Q: 发布后应用启动报错？
A: 可能是代码被过度裁剪，检查 `TrimmerRoots.xml` 确保关键程序集已保护

### Q: 如何进一步减小体积？
A:
1. 移除不需要的 NuGet 包
2. 删除 Assets 中不使用的资源
3. 考虑使用框架依赖模式（需要用户安装 .NET 9）

### Q: 是否支持单文件发布？
A: Avalonia 项目**不推荐**使用 `PublishSingleFile`，可能导致资源加载失败

### Q: 如何为不同操作系统发布？
A: 修改 `publish.bat` 中的 `RUNTIME` 变量：
- Windows: `win-x64`, `win-arm64`
- Linux: `linux-x64`, `linux-arm64`
- macOS: `osx-x64`, `osx-arm64`

## 发布前检查

- [ ] 更新版本号（`AiComputer.csproj` 中的 `<Version>`）
- [ ] 测试 Release 构建
- [ ] 清理不需要的文件和注释
- [ ] 更新 README.md
- [ ] 准备发布说明和更新日志

## 混淆代码（可选）

如果需要保护代码，可在发布后使用混淆工具：
- 推荐: **ConfuserEx 2** 或 **Obfuscar**
- 详见混淆工具集成指南

---

**提示**: 首次发布建议先在本地测试，确保所有功能正常后再分发给用户。
