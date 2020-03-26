Section "un.$(remove_files_str)" ;"un.Remove files"
  SectionIn RO

  ; Remove files and uninstaller
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\*.xml"
  Delete "$INSTDIR\OKEGui.exe"
  Delete "$INSTDIR\LICENSE"
  Delete "$INSTDIR\uninst.exe"
  RMDIr /r "$INSTDIR\examples"
  RMDIr /r "$INSTDIR\x86"
  RMDIr /r "$INSTDIR\x64"

  ; Remove directories used
  RMDir "$INSTDIR"
SectionEnd


Section /o "un.$(remove_config_str)" ;"un.Remove config file"
  Delete "$INSTDIR\OKEGuiConfig.json"
  RMDir "$INSTDIR"
SectionEnd


Section /o "un.$(remove_logs_str)" ;"un.Remove log files"
  RMDIr /r "$INSTDIR\log"
  RMDir "$INSTDIR"
SectionEnd


Section /o "un.$(remove_tools_str)" ;"un.Remove external tools"
  RMDIr /r "$INSTDIR\tools"
  RMDir "$INSTDIR"
SectionEnd


Section "un.$(remove_shortcuts_str)" ;"un.Remove shortcuts"
  SectionIn RO
; Remove shortcuts, if any
  RMDir /r "$SMPROGRAMS\OKEGui"
  Delete "$DESKTOP\OKEGui.lnk"
SectionEnd


Section "un.$(remove_registry_str)" ;"un.Remove registry keys"
  SectionIn RO
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OKEGui"
  DeleteRegKey HKLM "Software\OKEGui"
  DeleteRegKey HKLM "Software\Classes\OKEGui"

  System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


;--------------------------------
;Uninstaller Functions

Function un.onInit

  !insertmacro Init "uninstaller"
  !insertmacro MUI_UNGETLANGUAGE

FunctionEnd

Function un.onUninstSuccess
  SetErrorLevel 0
FunctionEnd
