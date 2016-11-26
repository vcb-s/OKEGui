#pragma once

#include "vapoursynth/VSScript.h"

#include "HelperFunc.h"

#include <string>
#include <Windows.h>

namespace VSHelper {
class VSynthScript
{
public:
	VSynthScript();
	VSynthScript(const std::wstring &vsDllPath) : dllPath(vsDllPath) { VSynthScript(); }
	~VSynthScript();

	bool LoadScript(const std::wstring &src, const std::wstring &name);
	bool LoadScriptFile(const std::wstring &path);
	void UnloadScript();

	int GetFramesNumber() const;
	double GetFPS() const;
	int64_t GetFPSNum() const;
	int64_t GetFPSDen() const;
	int GetWidth() const;
	int GetHeight() const;

	bool IsInit() const;

	const VSVideoInfo* GetVideoInfo() const;
	const VSCoreInfo* GetVSCoreInfo() const;

private:
	typedef int (VS_CC *vssInit)(void);
	typedef const VSAPI * (VS_CC *vssGetVSApi)(void);
	typedef int (VS_CC *vssEvaluateScript)(VSScript ** a_handle, const char * a_script, const char * a_scriptFilename, int a_flags);
	typedef int (VS_CC *vssEvaluateFile)(VSScript **handle, const char *scriptFilename, int flags);
	typedef const char * (VS_CC *vssGetError)(VSScript * a_handle);
	typedef VSCore * (VS_CC *vssGetCore)(VSScript * a_handle);
	typedef VSNodeRef * (VS_CC *vssGetOutput)(VSScript * a_handle, int a_index);
	typedef void (VS_CC *vssFreeScript)(VSScript * a_handle);
	typedef int (VS_CC *vssFinalize)(void);

	std::wstring scriptPath;

	std::wstring dllPath;

	const VSCoreInfo *vsCoreInfo;

	const VSAPI *vsVSAPI;
	VSScript *vsScript;
	VSNodeRef *vsOutNode;
	const VSVideoInfo *vsVideoInfo;

	HMODULE hVSS;

	bool isInit;
	bool isLoaded;

	vssInit VSSInit;
	vssGetVSApi VSSGetVSApi;
	vssEvaluateScript VSSEvaluateScript;
	vssEvaluateFile VSSEvaluateFile;
	vssGetError VSSGetError;
	vssGetCore VSSGetCore;
	vssGetOutput VSSGetOutput;
	vssFreeScript VSSFreeScript;
	vssFinalize VSSFinalize;

	bool LoadVSLibrary();
	bool Init();
	bool Finalize();
};
}
