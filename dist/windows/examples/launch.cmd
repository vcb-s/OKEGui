:: This script launches OKEGui.exe and then elevates privilege to
:: disable power throattling of bundled vspipe.exe and x265.exe.
::
:: Use this to launch OKEGui when you're using Intel hybrid cores
:: and don't want Windows to put those processes into the efficient
:: cores when the OKEGui window is in the background.
::
:: Also note this scripts requires that you're not running it as
:: Administrator.

cd %~dp0
net file 1>NUL 2>NUL
if not '%errorlevel%' == '0' (
    start "" OKEGui.exe
    powershell Start-Process -FilePath "%0" -verb runas >NUL 2>&1
    exit /b
)
powercfg /powerthrottling disable /PATH %~dp0\tools\x26x\x265.exe
powercfg /powerthrottling disable /PATH %~dp0\tools\x26x\x264.exe
powercfg /powerthrottling disable /PATH %~dp0\tools\vapoursynth\vspipe.exe
