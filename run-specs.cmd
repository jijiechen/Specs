@ECHO off
@SET WORKDIR=%cd%
@pushd %WORKDIR%

@SET ISSOURCE=0
@IF EXIST "%WORKDIR%\*.csproj" SET ISSOURCE=1
@IF EXIST "%WORKDIR%\*.cs" SET ISSOURCE=1
@IF EXIST "%WORKDIR%\*.vbproj" SET ISSOURCE=1
@IF EXIST "%WORKDIR%\*.vb" SET ISSOURCE=1
@IF %ISSOURCE% == 0 GOTO gen


@SET MSBUILDEXEDIR=%windir%\Microsoft.NET\Framework\v4.0.30319
@IF EXIST "%ProgramFiles(x86)%\MSBuild\12.0\bin" SET MSBUILDEXEDIR=%ProgramFiles(x86)%\MSBuild\12.0\bin
@IF EXIST "%ProgramFiles(x86)%\MSBuild\14.0\bin" SET MSBUILDEXEDIR=%ProgramFiles(x86)%\MSBuild\14.0\bin
@IF EXIST "%ProgramFiles(x86)%\MSBuild\15.0\bin" SET MSBUILDEXEDIR=%ProgramFiles(x86)%\MSBuild\15.0\bin

@CD %WORKDIR%
"%MSBUILDEXEDIR%\MSBuild.exe" /property:Configuration=Debug
@IF ERRORLEVEL 1 GOTO end
@GOTO gen





:gen

@setlocal ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION
@set alfanum=ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz

@SET randname=
	FOR /L %%b IN (0, 1, 8) DO (
		SET /A rnd_num=!RANDOM! * 52 / 32768 + 1
		for /F %%c in ('echo %%alfanum:~!rnd_num!^,1%%') do set randname=!randname!%%c
	)
@CD %~dp0

@SET assembly=%1
@IF "%assembly%" == "" SET assembly=%randname%
@SET PROJ=%WORKDIR%
@IF %ISSOURCE% == 1 SET PROJ=%WORKDIR%\bin\Debug
specs.exe generate "%PROJ%" --assembly-name %assembly% -v
@IF %ERRORLEVEL% NEQ 0 GOTO end




@CD %~dp0\SpecRun.Runner

@SET profile=%2
@IF "%profile%" == "" SET profile=%assembly%
SpecRun.exe run %profile%.srprofile "/baseFolder:%PROJ%" /log:specrun.log %3 %4 %5 %6
@GOTO end




:end
@popd