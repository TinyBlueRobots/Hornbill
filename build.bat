@echo off
cls
if not exist .paket\paket.exe .paket\paket.bootstrapper.exe
.paket\paket restore
packages\FAKE\tools\Fake %*