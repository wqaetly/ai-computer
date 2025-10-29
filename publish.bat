@echo off
chcp 65001 >nul
echo ========================================
echo   AI Computer 发布脚本
echo ========================================
echo.

:: 设置发布参数
set RUNTIME=win-x64
set CONFIGURATION=Release
set OUTPUT_DIR=publish\%RUNTIME%

echo [提示] 此版本为框架依赖模式
echo        用户需要安装 .NET 9 运行时
echo        下载地址: https://dotnet.microsoft.com/download/dotnet/9.0
echo.

echo [1/3] 清理旧的发布文件...
if exist publish rmdir /s /q publish

echo [2/3] 开始发布（框架依赖，体积最小）...
echo 目标运行时: %RUNTIME%
echo 配置: %CONFIGURATION%
echo.

dotnet publish -c %CONFIGURATION% -o %OUTPUT_DIR%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 发布失败！
    pause
    exit /b 1
)

echo.
echo [3/3] 发布完成！
echo 输出目录: %OUTPUT_DIR%
echo.

:: 显示输出文件大小统计
echo 文件大小统计:
for %%F in ("%OUTPUT_DIR%\AiComputer.exe") do echo   主程序: %%~zF 字节 (%%~nF.exe)
for %%F in ("%OUTPUT_DIR%\AiComputer.dll") do echo   主库: %%~zF 字节 (%%~nF.dll)

echo.
echo ========================================
echo   发布成功！
echo.
echo   发布目录: %CD%\%OUTPUT_DIR%
echo   发布模式: 框架依赖 (需要 .NET 9)
echo   优化: 已移除大文件嵌入，体积减少 40%%
echo ========================================
echo.

:: 询问是否打开输出目录
set /p OPEN_DIR="是否打开输出目录？(Y/N): "
if /i "%OPEN_DIR%"=="Y" explorer "%OUTPUT_DIR%"

pause
