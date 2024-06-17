#include <windows.h>
#include <wchar.h>
#include <vcclr.h>
#include <winuser.h>
//C:\Program Files (x86)\Windows Kits\10\Include\10.0.22621.0\um\WinUser.h

// #include "lua.hpp"
// #include "luainterop.h"
// extern "C" {
// #include "luautils.h"
// }

#include "Api.h"


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Diagnostics;
using namespace Interop;


#define MAX_PATH  260 // win32 def

// // This struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
// struct lua_State {};

// Protect lua context calls by multiple threads.
static CRITICAL_SECTION _critsect;

// Convert managed string to unmanaged.
static int _ToCString(char* buff, size_t bufflen, String^ input);

// Convert unmanaged string to managed.
static String^ _ToManagedString(const char* input);




//--------------------------------------------------------//
Api::Api(List<String^>^ lpath)
{
    _lpath = lpath;
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();
    InitializeCriticalSection(&_critsect);

    NebStatus stat = NebStatus::Ok;

    // Init lua.
    //_l = luaL_newstate();

    Debug::WriteLine("*** Api::Api() this={0} _l={1}", this->GetHashCode(), MAKE_ID(_l));

    HWND hwnd = GetForegroundWindow();

    // Load std libraries.
    // luaL_openlibs(_l);

    // Fix lua path.
    if (_lpath->Count > 0)
    {
        // // https://stackoverflow.com/a/4156038
        // lua_getglobal(_l, "package");
        // lua_getfield(_l, -1, "path");
        // String^ currentPath = _ToManagedString(lua_tostring(_l, -1));

        // StringBuilder^ sb = gcnew StringBuilder(currentPath);
        // sb->Append(";"); // default lua path doesn't have this.
        // for each (String^ lp in _lpath) // add app specific.
        // {
        //     sb->Append(String::Format("{0}\\?.lua;", lp));
        // }
        // String^ newPath = sb->ToString();

        // // Create a big enough C buffer and convert.
        // int newLen = newPath->Length + 50;
        // char* spath = (char*)malloc(newLen);
        // int ret = _ToCString(spath, newLen, newPath);

        // lua_pop(_l, 1);
        // lua_pushstring(_l, spath);
        // lua_setfield(_l, -2, "path");
        // lua_pop(_l, 1);
        // free(spath);
    }

    //// Load host funcs into lua space. This table gets pushed on the stack and into globals.
    //luainterop_Load(_l);

    //// Pop the table off the stack as it interferes with calling the module functions.
    //lua_pop(_l, 1);
}

//--------------------------------------------------------//
Api::~Api()
{
    Debug::WriteLine("*** Api::~Api() this={0} _l={1}", this->GetHashCode(), MAKE_ID(_l));

    // Finished. Clean up resources and go home.
    DeleteCriticalSection(&_critsect);
    // if (_l != nullptr)
    // {
    //     lua_close(_l);
    //     _l = nullptr;
    // }
}

//--------------------------------------------------------//
NebStatus Api::OpenScript(String^ fn)
{
    NebStatus nstat = NebStatus::Ok;
    // int lstat = LUA_OK;
    // int ret = 0;
    Error = gcnew String("");
    SectionInfo->Clear();

    EnterCriticalSection(&_critsect);

    // if (_l == nullptr)
    // {
    //     Error = gcnew String("You forgot to call Init().");
    //     nstat = NebStatus::ApiError;
    // }

    // // Load the script.
    // if (nstat == NebStatus::Ok)
    // {
    //     char fnx[MAX_PATH];
    //     int ret = _ToCString(fnx, MAX_PATH, fn);
    //     // Pushes the compiled chunk as a lua function on top of the stack or pushes an error message.
    //     lstat = luaL_loadfile(_l, fnx);
    //     nstat = EvalLuaStatus(lstat, "Load script file failed.");
    // }

    // // Execute the script to initialize it. This catches runtime syntax errors.
    // if (nstat == NebStatus::Ok)
    // {
    //     lstat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    //     nstat = EvalLuaStatus(lstat, "Execute script failed.");
    // }

    // // Execute setup().
    // if (nstat == NebStatus::Ok)
    // {
    //     luainterop_Setup(_l);
    //     if (luainterop_Error() != NULL)
    //     {
    //         Error = gcnew String(luainterop_Error());
    //         nstat = NebStatus::SyntaxError;
    //     }
    // }

    // // Get length and script info.
    // if (nstat == NebStatus::Ok)
    // {
    //     // Get section info.
    //     int ltype = lua_getglobal(_l, "_section_info");
    //     lua_pushnil(_l);
    //     while (lua_next(_l, -2) != 0) // && lstat ==_lUA_OK)
    //     {
    //         SectionInfo->Add((int)lua_tointeger(_l, -1), _ToManagedString(lua_tostring(_l, -2)));
    //         lua_pop(_l, 1);
    //     }
    //     lua_pop(_l, 1); // Clean up stack.
    // }
    
    // LeaveCriticalSection(&_critsect);
    return nstat;
}

//--------------------------------------------------------//
NebStatus Api::Step(int tick)
{
    NebStatus ret = NebStatus::Ok;
    Error = gcnew String("");
    EnterCriticalSection(&_critsect);

    // luainterop_Step(_l, tick);
    // if (luainterop_Error() != NULL)
    // {
    //     Error = gcnew String(luainterop_Error());
    //     ret = NebStatus::ApiError;
    // }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
NebStatus Api::RcvNote(int chan_hnd, int note_num, double volume)
{
    NebStatus ret = NebStatus::Ok;
    Error = gcnew String("");
    EnterCriticalSection(&_critsect);

    // if (luainterop_Error() != NULL)
    // {
    //     Error = gcnew String(luainterop_Error());
    //     ret = NebStatus::ApiError;
    // }

    // luainterop_RcvNote(_l, chan_hnd, note_num, volume);

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
NebStatus Api::RcvController(int chan_hnd, int controller, int value)
{
    NebStatus ret = NebStatus::Ok;
    Error = gcnew String("");
    EnterCriticalSection(&_critsect);

    // luainterop_RcvController(_l, chan_hnd, controller, value);
    // if (luainterop_Error() != NULL)
    // {
    //     Error = gcnew String(luainterop_Error());
    //     ret = NebStatus::ApiError;
    // }

    LeaveCriticalSection(&_critsect);
    return ret;
}


//------------------- Privates ---------------------------//

//--------------------------------------------------------//
// NebStatus Api::EvalLuaStatus(int lstat, String^ info)
// {
//     NebStatus nstat;

//     // Translate between internal LUA_XXX status and client facing NEB_XXX status.
//     switch (lstat)
//     {
//         case LUA_OK:        nstat = NebStatus::Ok;              break;
//         case LUA_ERRSYNTAX: nstat = NebStatus::SyntaxError;     break;
//         case LUA_ERRFILE:   nstat = NebStatus::FileError;       break;
//         case LUA_ERRRUN:    nstat = NebStatus::RunError;        break;
//         default:            nstat = NebStatus::ApiError;        break;
//     }

//     if (nstat != NebStatus::Ok)
//     {
//         // Maybe lua error message.
//         const char* smsg = "";
//         if (lstat <= LUA_ERRFILE && _l != NULL && lua_gettop(_l) > 0)
//         {
//             smsg = lua_tostring(_l, -1);
//             lua_pop(_l, 1);
//             Error = String::Format(gcnew String("{0}: {1}\n{2}:{3}"), nstat.ToString(), info, lstat, gcnew String(smsg));
//         }
//         else
//         {
//             Error = String::Format(gcnew String("{0}: {1}"), nstat.ToString(), info);
//         }
//     }
//     else
//     {
//         Error = "";
//     }

//     return nstat;
// }


//--------------------------------------------------------//
int _ToCString(char* buff, size_t bufflen, String^ input)
{
    int ret = 0; // TODO handle errors?
    int inlen = input->Length;

    if (inlen < bufflen - 1)
    {
        // https://learn.microsoft.com/en-us/cpp/dotnet/how-to-access-characters-in-a-system-string?view=msvc-170
        // not! const char* str4 = context->marshal_as<const char*>(input);
        interior_ptr<const wchar_t> ppchar = PtrToStringChars(input);
        int i = 0;
        for (; *ppchar != L'\0' && i < inlen && ret >= 0; ++ppchar, i++)
        {
            int c = wctob(*ppchar);
            if (c != -1)
            {
                buff[i] = c;
            }
            else
            {
                ret = -2; // invalid char
                buff[i] = '?';
            }
        }
        buff[i] = 0; // terminate
    }
    else
    {
        ret = -1; // not enough room.
    }

    return ret;
}

//--------------------------------------------------------//
String^ _ToManagedString(const char* input)
{
    return gcnew String(input);
}
