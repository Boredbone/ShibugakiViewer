#include "stdafx.h"
#include "ExitApp.h"
#include <iostream>
#include <windows.h>
using namespace std;

EXITAPP_API int fnExitApp(void)
{
	try
	{
		TCHAR  szData [] = TEXT("?exit");
		DWORD  dwResult;

		string pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";
		string pipeName = "\\\\.\\pipe\\" + pipeId;

		auto hPipe = CreateFile(TEXT(pipeName.c_str()),
			GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

		if (hPipe != INVALID_HANDLE_VALUE)
		{
			WriteFile(hPipe, szData, sizeof(szData), &dwResult, NULL);
			CloseHandle(hPipe);
		}
	}
	catch (...)
	{

	}
	return 0;
}

