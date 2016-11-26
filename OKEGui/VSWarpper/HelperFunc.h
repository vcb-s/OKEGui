#pragma once

#include <string>

#include <Windows.h>

inline std::string WstringToString(const std::wstring &wsToMatch)
{
	size_t iWLen = WideCharToMultiByte(CP_ACP, 0, wsToMatch.c_str(), wsToMatch.size(), 0, 0, NULL, NULL);

	char *lpwsz = new char[iWLen + 1];
	WideCharToMultiByte(CP_ACP, 0, wsToMatch.c_str(), wsToMatch.size(), lpwsz, iWLen, NULL, NULL);

	lpwsz[iWLen] = 0;
	std::string sToMatch(lpwsz);
	delete[] lpwsz;

	return sToMatch;
}

inline bool FileExists(const std::wstring& dirName_in)
{
	DWORD ftyp = GetFileAttributes(dirName_in.c_str());

	return (ftyp != INVALID_FILE_ATTRIBUTES) && !(ftyp & FILE_ATTRIBUTE_DIRECTORY);
}
