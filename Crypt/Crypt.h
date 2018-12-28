#pragma once
#pragma pack(1)

#include <stdint.h>

#define DLL_VERSION "1.4.1"

#define DLLFUNCTION __declspec(dllexport)
#define DLLVAR DLLFUNCTION

#ifdef _DEBUG
//#define LOGGING
#endif

enum IError
{
	SUCCESS,
	NO_UOWND,
	NO_TID,
	NO_HOOK,
	NO_SHAREMEM,
	LIB_DISABLED,
	NO_PATCH,
	NO_COPY,
	INVALID_PARAMS,

	UNKNOWN,
};

enum UONET_MESSAGE
{
	SEND = 1,
	RECV = 2,
	READY = 3,
	NOT_READY = 4,
	CONNECT = 5,
	DISCONNECT = 6,
	KEYDOWN = 7,
	MOUSE = 8,

	ACTIVATE = 9,
	FOCUS = 10,

	CLOSE = 11,
	NOTO_HUE = 13,
	DLL_ERROR = 14,

	SETWNDSIZE = 19,

	FINDDATA = 20,

	SMART_CPU = 21,
	SET_MAP_HWND = 23
};

enum class UONET_MESSAGE_COPYDATA
{
	POSITION = 1,
};

//#define SHARED_BUFF_SIZE 0x80000 // Client's buffers are 500k
#define SHARED_BUFF_SIZE 524288 // 262144 // 250k
struct Buffer
{
	int Length;
	int Start;
	BYTE Buff[SHARED_BUFF_SIZE];
};

#pragma pack(1)
struct Position {
	uint16_t x;
	uint16_t y;
	uint16_t z;
};
static_assert(sizeof(struct Position) == 6, "Incorrect size\n");

struct SharedMemory
{
	// Do *not* mess with this struct.  Really.  I mean it.
	Buffer InRecv;
	Buffer OutRecv;
	Buffer InSend;
	Buffer OutSend;

	bool ForceDisconn;
	unsigned int TotalSend;
	unsigned int TotalRecv;
	unsigned short PacketTable[256];
	unsigned int ServerIP;
	unsigned short ServerPort;
	char UOVersion[16];
};

#define WM_PROCREADY WM_USER
#define WM_UONETEVENT WM_USER+1

#ifndef WM_XBUTTONDOWN
#define WM_XBUTTONDOWN                  0x020B
#endif

extern HINSTANCE hInstance;
extern SharedMemory *pShared;
extern HANDLE CommMutex;

DLLFUNCTION int InstallLibrary(HWND RazorWindow, HWND UOWindow, int flags);
DLLFUNCTION void Shutdown();
DLLFUNCTION void *GetSharedAddress();
DLLFUNCTION int GetPacketLength( unsigned char *data, int len );
DLLFUNCTION bool IsDynLength( unsigned char packet );
DLLFUNCTION HANDLE GetCommMutex();
DLLFUNCTION unsigned int TotalIn();
DLLFUNCTION unsigned int TotalOut();
DLLFUNCTION void CalibratePosition(uint16_t x, uint16_t y, uint16_t z);
DLLFUNCTION void OnAttach(void *params, int paramsLen);
DLLFUNCTION void SetServer(unsigned int addr, unsigned short port);
DLLFUNCTION const char *GetUOVersion();


LRESULT CALLBACK UOAWndProc( HWND, UINT, WPARAM, LPARAM );
void Log( const char *format, ... );
void MemoryPatch( unsigned long, unsigned long );
void MemoryPatch( unsigned long, int, int );
void MemoryPatch( unsigned long, const void *, int );

//#define PACKET_TBL_STR "Got Logout OK packet!\0\0\0"
//#define PACKET_TS_LEN 24
#define PACKET_TBL_STR "\x07\0\0\0\x03\0\0\0"
#define PACKET_TS_LEN 8
#define PACKET_TBL_OFFSET (0-(8+12+12))
