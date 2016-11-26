#include "VSWarpper.h"
#include "VSynthScript.h"
#include <stdlib.h>
#include <map>

extern std::map<int, VSHelper::VSynthScript*> *vsHandle;

inline bool CheckHandleList()
{
	return vsHandle == nullptr;
}

inline VSHelper::VSynthScript* GetVSLibrary(VSHANDLE vsLib)
{
	try {
		return vsHandle->at(vsLib);
	} catch (const std::out_of_range& oor) {
		return nullptr;
	}
}

VSWARPPER_API VSHANDLE InitVSLibrary(const wchar_t * dllPath)
{
	if (CheckHandleList()) {
		return (VSHANDLE)nullptr;
	}

	VSHelper::VSynthScript *vs;

	if (dllPath == nullptr) {
		vs = new VSHelper::VSynthScript();
	} else {
		vs = new VSHelper::VSynthScript(dllPath);
	}

	int id;
	do {
		id = rand();
	} while (vsHandle->count(id) != 0);

	vsHandle->insert({ id, vs });

	return id;
}

VSWARPPER_API bool IsVSLibraryInit(VSHANDLE vsLib)
{
	if (CheckHandleList()) {
		return false;
	}

	VSHelper::VSynthScript *vs = GetVSLibrary(vsLib);
	if (vs == nullptr) {
		return false;
	}

	return vs->IsInit();
}

VSWARPPER_API bool LoadScript(VSHANDLE vsLib, const wchar_t * script, const wchar_t * scriptName)
{
	if (CheckHandleList()) {
		return false;
	}

	VSHelper::VSynthScript *vs = GetVSLibrary(vsLib);
	if (vs == nullptr) {
		return false;
	}

	return vs->LoadScript(script, scriptName);
}

VSWARPPER_API bool LoadScriptFile(VSHANDLE vsLib, const wchar_t * scriptPath)
{
	if (CheckHandleList()) {
		return false;
	}

	VSHelper::VSynthScript *vs = GetVSLibrary(vsLib);
	if (vs == nullptr) {
		return false;
	}

	return vs->LoadScriptFile(scriptPath);
}

VSWARPPER_API void* GetVideoInfo(VSHANDLE vsLib)
{
	if (CheckHandleList()) {
		return nullptr;
	}

	VSHelper::VSynthScript *vs = GetVSLibrary(vsLib);
	if (vs == nullptr) {
		return nullptr;
	}

	return (void *)vs->GetVideoInfo();
}

VSWARPPER_API void UnloadScript(VSHANDLE vsLib)
{
	if (CheckHandleList()) {
		return;
	}

	VSHelper::VSynthScript *vs = GetVSLibrary(vsLib);
	if (vs == nullptr) {
		return;
	}

	vs->UnloadScript();
}

VSWARPPER_API void CloseVSLibrary(VSHANDLE vsLib)
{
	if (CheckHandleList()) {
		return;
	}

	VSHelper::VSynthScript *vs = GetVSLibrary(vsLib);
	if (vs == nullptr) {
		return;
	}

	delete vs;
	vsHandle->erase(vsLib);
}
