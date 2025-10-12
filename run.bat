@echo off
REM 禁用 Intel CET Shadow Stack 功能以避免 CLR 断言失败
set DOTNET_EnableWriteXorExecute=0
set COMPlus_EnableWriteXorExecute=0

REM 运行应用程序
"%~dp0bin\Debug\net9.0\AiComputer.exe" %*
