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

echo [提示] 此版本为自包含模式
echo        无需用户安装 .NET 9 运行时，开箱即用
echo.

echo [1/3] 清理旧的发布文件...
if exist publish rmdir /s /q publish

echo [2/3] 开始发布（自包含模式）...
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
echo [3/3] 重命名可执行文件...
ren "%OUTPUT_DIR%\AiComputer.exe" "nkgtoolkit.exe"
if %ERRORLEVEL% NEQ 0 (
    echo [警告] 重命名失败，但发布成功
) else (
    echo 已将 AiComputer.exe 重命名为 nkgtoolkit.exe
)

echo.
echo [4/4] 发布完成！
echo 输出目录: %OUTPUT_DIR%
echo.

:: 显示输出文件大小统计
echo 文件大小统计:
for %%F in ("%OUTPUT_DIR%\nkgtoolkit.exe") do echo   主程序: %%~zF 字节
for %%F in ("%OUTPUT_DIR%\AiComputer.dll") do echo   主库: %%~zF 字节

echo.
echo ========================================
echo   发布成功！
echo.
echo   发布目录: %CD%\%OUTPUT_DIR%
echo   程序名称: nkgtoolkit.exe
echo   发布模式: 自包含（无需安装 .NET 9）
echo   优化: 移除大文件嵌入
echo ========================================
echo.

:: 询问是否打开输出目录
set /p OPEN_DIR="是否打开输出目录？(Y/N): "
if /i "%OPEN_DIR%"=="Y" explorer "%OUTPUT_DIR%"

pause
