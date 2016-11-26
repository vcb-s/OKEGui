#pragma once

#ifndef _UNICODE
#define  _UNICODE
#endif

extern "C" {
#ifdef VSWARPPER_EXPORTS
#define VSWARPPER_API extern "C" __declspec(dllexport)
#else
#define VSWARPPER_API extern "C" __declspec(dllimport)
#endif
}

typedef int VSHANDLE;
typedef void* PVIDEOINFO;

VSWARPPER_API VSHANDLE InitVSLibrary(const wchar_t* dllPath);
VSWARPPER_API bool IsVSLibraryInit(VSHANDLE vsLib);
VSWARPPER_API bool LoadScript(VSHANDLE vsLib, const wchar_t* script, const wchar_t* scriptName);
VSWARPPER_API bool LoadScriptFile(VSHANDLE vsLib, const wchar_t* scriptPath);
VSWARPPER_API PVIDEOINFO GetVideoInfo(VSHANDLE vsLib);
VSWARPPER_API void UnloadScript(VSHANDLE vsLib);
VSWARPPER_API void CloseVSLibrary(VSHANDLE vsLib);
