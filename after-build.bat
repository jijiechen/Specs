
@echo off
@pushd %cd%

@cd %~dp0\..\packages\SpecRun.Runner.*\tools
xcopy * %~dp0\bin\%1\SpecRun.Runner\ /e /Y

@popd