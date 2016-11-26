#include "VSynthScript.h"

#include <stdexcept>
#include <Windows.h>
#include <string>

namespace VSHelper {
VSynthScript::VSynthScript()
	: isInit(false),
	vsVideoInfo(nullptr),
	vsOutNode(nullptr)
{
	if (!Init()) {
		MessageBox(NULL, L"VS 初始化错误", NULL, MB_OK | MB_ICONERROR);
		return;
	}

	isInit = true;
}

VSynthScript::~VSynthScript()
{
	Finalize();
}

bool VSynthScript::Init()
{
	if (!LoadVSLibrary()) {
		return false;
	}

	if (VSSInit() == 0) {
		// VSLibrary 初始化失败
		return false;
	}

	vsVSAPI = VSSGetVSApi();
	if (!vsVSAPI) {
		// 获取VSAPI失败
		return false;
	}

	return true;
}

bool VSynthScript::Finalize()
{
	if (vsOutNode) {
		vsVSAPI->freeNode(vsOutNode);
		vsOutNode = nullptr;
	}

	if (vsScript) {
		VSSFreeScript(vsScript);
		vsScript = nullptr;
	}

	vsVSAPI = nullptr;

	VSSFinalize();

	return true;
}

bool VSynthScript::LoadVSLibrary()
{
#ifndef _WIN64
#error "Only Support x64"
#endif
	if (dllPath.empty()) {
		BYTE szPath[MAX_PATH] = { 0 };
		DWORD dwSize = MAX_PATH;
		if (RegGetValue(HKEY_LOCAL_MACHINE, L"SOFTWARE\\VapourSynth\\", L"VSScriptDLL", RRF_RT_ANY, NULL, szPath, &dwSize) != ERROR_SUCCESS) {
			return false;
		}
		dllPath = (wchar_t *)szPath;
	}

	if (!FileExists(dllPath)) {
		return false;
	}

	hVSS = LoadLibrary(dllPath.c_str());
	if (hVSS == NULL) {
		return false;
	}

	VSSInit = (vssInit)GetProcAddress(hVSS, "vsscript_init");
	VSSGetVSApi = (vssGetVSApi)GetProcAddress(hVSS, "vsscript_getVSApi");
	VSSEvaluateScript = (vssEvaluateScript)GetProcAddress(hVSS, "vsscript_evaluateScript");
	VSSEvaluateFile = (vssEvaluateFile)GetProcAddress(hVSS, "vsscript_evaluateFile");
	VSSGetError = (vssGetError)GetProcAddress(hVSS, "vsscript_getError");
	VSSGetCore = (vssGetCore)GetProcAddress(hVSS, "vsscript_getCore");
	VSSGetOutput = (vssGetOutput)GetProcAddress(hVSS, "vsscript_getOutput");
	VSSFreeScript = (vssFreeScript)GetProcAddress(hVSS, "vsscript_freeScript");
	VSSFinalize = (vssFinalize)GetProcAddress(hVSS, "vsscript_finalize");

	return true;
}

const VSCoreInfo* VSynthScript::GetVSCoreInfo() const
{
	throw "Not impl";
	return new VSCoreInfo{ 0 };
}

bool VSynthScript::LoadScript(const std::wstring &src, const std::wstring &name)
{
	if (isLoaded) {
		return false;
	}

	if (VSSEvaluateScript(&vsScript, WstringToString(src).c_str(), WstringToString(name).c_str(), efSetWorkingDir)) {
		const char *vsError = VSSGetError(vsScript);
		MessageBoxA(NULL, vsError, "VS Error", MB_OK | MB_ICONERROR);
		UnloadScript();
		return false;
	}

	//if (vsVSAPI->getCoreInfo(VSSGetCore(vsScript))->core < 29) {
	//	MessageBox(NULL, L"Failed to get the script output node.", L"VS Error", MB_OK | MB_ICONERROR);
	//	return false;
	//}

	vsOutNode = VSSGetOutput(vsScript, 0);
	if (!vsOutNode) {
		MessageBox(NULL, L"Failed to get the script output node.", L"VS Error", MB_OK | MB_ICONERROR);
		UnloadScript();
		return false;
	}

	vsVideoInfo = vsVSAPI->getVideoInfo(vsOutNode);

	if (vsVideoInfo) {
		isLoaded = true;
	}

	return isLoaded;
}

bool VSynthScript::LoadScriptFile(const std::wstring &path)
{
	if (isLoaded || !FileExists(path)) {
		return false;
	}

	if (VSSEvaluateFile(&vsScript, WstringToString(path).c_str(), efSetWorkingDir)) {
		const char *vsError = VSSGetError(vsScript);
		MessageBoxA(NULL, vsError, "VS Error", MB_OK | MB_ICONERROR);
		UnloadScript();
		return false;
	}

	//if (vsVSAPI->getCoreInfo(VSSGetCore(vsScript))->core < 29) {
	//	MessageBox(NULL, L"Failed to get the script output node.", L"VS Error", MB_OK | MB_ICONERROR);
	//	return false;
	//}

	vsOutNode = VSSGetOutput(vsScript, 0);
	if (!vsOutNode) {
		MessageBox(NULL, L"Failed to get the script output node.", L"VS Error", MB_OK | MB_ICONERROR);
		UnloadScript();
		return false;
	}

	vsVideoInfo = vsVSAPI->getVideoInfo(vsOutNode);

	if (vsVideoInfo) {
		isLoaded = true;
	}

	return isLoaded;
}

void VSynthScript::UnloadScript()
{
	if (!isLoaded) {
		return;
	}

	vsVideoInfo = nullptr;

	if (vsOutNode) {
		vsVSAPI->freeNode(vsOutNode);
		vsOutNode = nullptr;
	}

	if (vsScript) {
		VSSFreeScript(vsScript);
		vsScript = nullptr;
	}

	isLoaded = false;
}

int VSynthScript::GetFramesNumber() const
{
	if (!isLoaded) {
		return 0;
	}

	return vsVideoInfo->numFrames;
}

double VSynthScript::GetFPS() const
{
	if (!isLoaded) {
		return 0;
	}

	return (double)vsVideoInfo->fpsNum / (double)vsVideoInfo->fpsDen;
}

int64_t VSynthScript::GetFPSNum() const
{
	if (!isLoaded) {
		return 0;
	}

	return vsVideoInfo->fpsNum;
}

int64_t VSynthScript::GetFPSDen() const
{
	if (!isLoaded) {
		return 0;
	}

	return vsVideoInfo->fpsDen;
}

int VSynthScript::GetWidth() const
{
	if (!isLoaded) {
		return 0;
	}

	return vsVideoInfo->width;
}

int VSynthScript::GetHeight() const
{
	if (!isLoaded) {
		return 0;
	}

	return vsVideoInfo->height;
}

const VSVideoInfo* VSynthScript::GetVideoInfo() const
{
	if (!isLoaded) {
		return 0;
	}

	return vsVideoInfo;
}

bool VSynthScript::IsInit() const
{
	return isInit;
}
}
