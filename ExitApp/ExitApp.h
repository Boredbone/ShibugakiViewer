extern "C"
{

#ifdef EXITAPP_EXPORTS
#define EXITAPP_API __declspec(dllexport)
#else
#define EXITAPP_API __declspec(dllimport)
#endif

	EXITAPP_API int fnExitApp(void);
}
