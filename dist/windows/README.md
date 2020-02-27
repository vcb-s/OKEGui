PACKAGERS:

You will need NSIS and upx to make the installer. You need a unicode version of NSIS.

1. Open the options.nsi file in an editor and change line that contains
   "!define PROG_VERSION "6.5.0.0"" to the version of OKEGui you just built.
2. Extract the plugins found in the folder "nsis plugins" into your
   NSIS's unicode Plugin directory(usually C:\Program Files\NSIS\Plugins\x86-unicode).
   Only the *.dll files are needed. Use the unicode version of the dlls.
3. The script you need to compile is "okegui.nsi". It includes all other necessary scripts.
4. The script expects the following file tree:

The installer script expects the following file tree:

```
Root:
installer-translations\
	english.nsi
	simpchinese.nsi
    ...
	(all the .nsi files found here in every source release)
samples\
    (all sample sciprt or config files found here)
tools\
    (all required tools found here)
installer.nsi
options.nsi
okegui.nsi
translations.nsi
UAC.nsh
uninstaller.nsi
```

5. Make sure a relese build has been performed.
6. "`OKEGui_{VERSION}_setup.exe`" is the compiled binary file.
