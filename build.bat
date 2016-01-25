@echo off
cls
.paket\paket.bootstrapper.exe
.paket\paket restore
packages\FAKE\tools\Fake %*