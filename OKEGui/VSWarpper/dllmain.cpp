// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include <Windows.h>
#include <map>
#include "VSynthScript.h"

std::map<int, VSHelper::VSynthScript*> *vsHandle = nullptr;

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call) {
	case DLL_PROCESS_ATTACH:
		vsHandle = new std::map<int, VSHelper::VSynthScript*>();
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		delete vsHandle;
		vsHandle = nullptr;
		break;
	}
	return TRUE;
}
