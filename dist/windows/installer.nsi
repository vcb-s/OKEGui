Var uninstallerPath

Section "-hidden"

    ;Search if OKEGui is already installed.
    FindFirst $0 $1 "$uninstallerPath\uninst.exe"
    FindClose $0
    StrCmp $1 "" done

    ;Run the uninstaller of the previous install.
    DetailPrint $(inst_unist_str)
    ExecWait '"$uninstallerPath\uninst.exe" /S _?=$uninstallerPath'
    Delete "$uninstallerPath\uninst.exe"
    RMDir "$uninstallerPath"

    done:

SectionEnd


Section $(inst_req_str) ;"OKEGui (required)"
  SectionIn RO

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  ; Put file there
  File "..\..\OKEGui\OKEGui\bin\Release\*.dll"
  File "..\..\OKEGui\OKEGui\bin\Release\*.xml"
  File "..\..\OKEGui\OKEGui\bin\Release\OKEGui.exe"
  File "..\..\LICENSE"

  SetOutPath $INSTDIR\x86
  File /r "..\..\OKEGui\OKEGui\bin\Release\x86\*"

  SetOutPath $INSTDIR\x64
  File /r "..\..\OKEGui\OKEGui\bin\Release\x64\*"

  ; Write the installation path into the registry
  WriteRegStr HKLM "Software\OKEGui" "InstallLocation" "$INSTDIR"

  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "DisplayName" "OKEGui ${PROG_VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "UninstallString" '"$INSTDIR\uninst.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "DisplayIcon" '"$INSTDIR\OKEGui.exe",0'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "Publisher" "VCB-Studio"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "URLInfoAbout" "https://github.com/vcb-s/OKEGui"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "DisplayVersion" "${PROG_VERSION}"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "NoRepair" 1
  WriteUninstaller "uninst.exe"
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui" "EstimatedSize" "$0"

SectionEnd

Section $(inst_tools_str) ;"External tools (recommend)"
  CreateDirectory "$INSTDIR\tools"
  SetOutPath "$INSTDIR\tools"
  File /r "tools\*"
SectionEnd

Section /o $(inst_samples_str) ;"Example files (optional)"
  CreateDirectory "$INSTDIR\examples"
  SetOutPath "$INSTDIR\examples"
  File /r "examples\*"
SectionEnd

; Optional section (can be disabled by the user)
Section /o $(inst_dekstop_str) ;"Create Desktop Shortcut"

  SetOutPath "$INSTDIR"
  CreateShortCut "$DESKTOP\OKEGui.lnk" "$INSTDIR\OKEGui.exe"

SectionEnd

Section $(inst_startmenu_str) ;"Create Start Menu Shortcut"

  CreateDirectory "$SMPROGRAMS\OKEGui"
  SetOutPath "$INSTDIR"
  CreateShortCut "$SMPROGRAMS\OKEGui\OKEGui.lnk" "$INSTDIR\OKEGui.exe"
  CreateShortCut "$SMPROGRAMS\OKEGui\Uninstall.lnk" "$INSTDIR\uninst.exe"

SectionEnd


;--------------------------------

Function .onInit

  !insertmacro Init "installer"
  !insertmacro MUI_LANGDLL_DISPLAY

  ;Search if OKEGui is already installed.
  FindFirst $0 $1 "$INSTDIR\uninst.exe"
  FindClose $0
  StrCmp $1 "" done

  ;Copy old value to var so we can call the correct uninstaller
  StrCpy $uninstallerPath $INSTDIR

  ;Inform the user
  MessageBox MB_OKCANCEL|MB_ICONINFORMATION $(inst_uninstall_question_str) /SD IDOK IDOK done
  Quit

  done:

FunctionEnd


Function PageFinishRun

  !insertmacro UAC_AsUser_ExecShell "" "$INSTDIR\OKEGui.exe" "" "$INSTDIR" ""

FunctionEnd

Function .onInstSuccess
  SetErrorLevel 0
FunctionEnd
