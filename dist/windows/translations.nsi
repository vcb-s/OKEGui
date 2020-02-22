;Nsis translations

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "SimpChinese"

;Installer/Uninstaller translations
!addincludedir installer-translations

;The languages should be in alphabetical order
!include english.nsi
!include simpchinese.nsi
