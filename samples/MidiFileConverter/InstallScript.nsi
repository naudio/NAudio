; MIDI File Converter install script
; originally based on example2 that comes with nsis
;
; This script is based on example1.nsi, but it remember the directory, 
; has uninstall support and (optionally) installs start menu shortcuts.
;
; It will install example2.nsi into a directory that the user selects,

;--------------------------------
!include WordFunc.nsh
!insertmacro VersionCompare
 
!include LogicLib.nsh
RequestExecutionLevel highest
!define VERSION "0.3"
!define FULLVERSION "0.3.9.0"
!define PRODUCT_NAME "MIDI File Converter"
!define EXECUTABLE_NAME "MIDI File Converter.exe"

; The name of the installer
Name "MIDI File Converter"

; The file to write
; OutFile "${PRODUCT_NAME} v${VERSION} Install.exe"
OutFile "midi_file_converter_0_3_install.exe"

; The default installation directory
InstallDir "$PROGRAMFILES\Mark Heath\${PRODUCT_NAME}"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\${PRODUCT_NAME}" "Install_Dir"

VIAddVersionKey /LANG=1033-English "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey /LANG=1033-English "Comments" ""
VIAddVersionKey /LANG=1033-English "CompanyName" "Mark Heath"
VIAddVersionKey /LANG=1033-English "LegalCopyright" "© 2007 Mark Heath"
VIAddVersionKey /LANG=1033-English "FileDescription" "${PRODUCT_NAME} Installer"
VIAddVersionKey /LANG=1033-English "FileVersion" "${VERSION}"
VIProductVersion "${FULLVERSION}"



;--------------------------------

; Utils

Function .onInit
  Call GetDotNETVersion
  Pop $0
  ${If} $0 == "not found"
    MessageBox MB_OK|MB_ICONSTOP ".NET runtime library v2.0 or newer is required."					
    ExecShell "open" "http://msdn.microsoft.com/netframework/downloads/updates/default.aspx"
    Abort
  ${EndIf}
 
  StrCpy $0 $0 "" 1 # skip "v"
 
  ${VersionCompare} $0 "2.0" $1
  ${If} $1 == 2
    MessageBox MB_OK|MB_ICONSTOP ".NET runtime library v2.0 or newer is required. You have $0."
    ExecShell "open" "http://msdn.microsoft.com/netframework/downloads/updates/default.aspx"
    Abort
  ${EndIf}
FunctionEnd
 
Function GetDotNETVersion
  Push $0
  Push $1
 
  System::Call "mscoree::GetCORVersion(w .r0, i ${NSIS_MAX_STRLEN}, *i) i .r1"
  StrCmp $1 "error" 0 +2
    StrCpy $0 "not found"
 
  Pop $1
  Exch $0
FunctionEnd

;--------------------------------

; Pages

Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "Program Files (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "MIDI File Converter.exe"
  File "MIDI File Converter.exe.config"
  File "NAudio.dll"
  File "midi_file_converter.html"
  File "NamingRules.xml"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "SOFTWARE\${PRODUCT_NAME}" "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\${EXECUTABLE_NAME}" "" "$INSTDIR\${EXECUTABLE_NAME}" 0
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
  DeleteRegKey HKLM "SOFTWARE\${PRODUCT_NAME}"

  ; Remove files and uninstaller
  Delete "$INSTDIR\uninstall.exe"
  Delete "$INSTDIR\MIDI File Converter.exe"
  Delete "$INSTDIR\MIDI File Converter.exe.config"
  Delete "$INSTDIR\NAudio.dll"
  Delete "$INSTDIR\midi_file_converter.html"
  Delete "$INSTDIR\NamingRules.xml"

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"

  ; Remove directories used
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  RMDir "$INSTDIR"

SectionEnd
