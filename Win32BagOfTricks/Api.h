#pragma once

using namespace System;
using namespace System::Collections::Generic;

// This is crude but should be ok for nw.
#define MAKE_ID(l)  9999
//#define MAKE_ID(l)  ((int)((long long)l & 0X00000000FFFFFFFF))


namespace Interop
{
    /// <summary>Nebulua status. App errors start after internal lua errors so they can be handled consistently.</summary>
    public enum class NebStatus
    {
        Ok = 0,
        // Api returns these:
        SyntaxError = 10, RunError = 11, ApiError = 12, FileError = 13,
        // App level errors:
        AppInternalError = 20,
    };

    //#pragma region Forward refs
    //ref class CreateChannelArgs;
    //ref class SendArgs;
    //ref class PropertyArgs;
    //ref class LogArgs;
    //#pragma endregion

    public ref class Api
    {
    #pragma region Fields
    private:
        // The lua thread.
        //lua_State* _l = nullptr;

        // The LUA_PATH parts.
        List<String^>^ _lpath;
    #pragma endregion

    #pragma region Properties
    public:
        /// <summary>If an API or lua function failed this contains info.</summary>
        property String^ Error;

        /// <summary>What's in the script.</summary>
        property Dictionary<int, String^>^ SectionInfo;

        /// <summary>Unique opaque id.</summary>
        property int Id { int get() { return MAKE_ID(_l); }}
    #pragma endregion

    #pragma region Lifecycle
    public:
        /// <summary>Initialize everything.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        Api(List<String^>^ lpath);

        /// <summary>Clean up resources.</summary>
        ~Api();
    #pragma endregion

    #pragma region Call lua functions from host
    public:
        /// <summary>Load and process.</summary>
        /// <param name="fn">Full file path</param>
        /// <returns>Neb Status</returns>
        NebStatus OpenScript(String^ fn);

        /// <summary>Called every fast timer increment aka tick.</summary>
        /// <param name="tick">Current tick 0 => N</param>
        /// <returns>Neb Status</returns>
        NebStatus Step(int tick);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="note_num">Note number 0 => 127</param>
        /// <param name="volume">Volume 0.0 => 1.0</param>
        /// <returns>Neb Status</returns>
        NebStatus RcvNote(int chan_hnd, int note_num, double volume);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="controller">Specific controller id 0 => 127</param>
        /// <param name="value">Payload 0 => 127</param>
        /// <returns>Neb Status</returns>
        NebStatus RcvController(int chan_hnd, int controller, int value);
    #pragma endregion

    // #pragma region Event hooks
    // public:
    //     static event EventHandler<CreateChannelArgs^>^ CreateChannel;
    //     static void NotifyCreateChannel(CreateChannelArgs^ args) { CreateChannel(nullptr, args); }

    //     static event EventHandler<SendArgs^>^ Send;
    //     static void NotifySend(SendArgs^ args) { Send(nullptr, args); }

    //     static event EventHandler<LogArgs^>^ Log;
    //     static void NotifyLog(LogArgs^ args) { Log(nullptr, args); }

    //     static event EventHandler<PropertyArgs^>^ PropertyChange;
    //     static void NotifyPropertyChange(PropertyArgs^ args) { PropertyChange(nullptr, args); }
    // #pragma endregion

    #pragma region Private functions
    private:
        /// <summary>Checks lua status and converts to neb status. Stores an error message if it failed.</summary>
        //NebStatus EvalLuaStatus(int stat, String^ msg);
    #pragma endregion

    //public:
    //    Api() = default;

    };

    // #pragma region Script callback data (events)
    // /// <summary>Common elements.</summary>
    // public ref class BaseArgs : public EventArgs
    // {
    // public:
    //     property int Sender;    // unique/opaque id or 0 for generic
    //     property int Ret;       // handler return value
    // };

    // /// <summary>Script creates a channel.</summary>
    // public ref class CreateChannelArgs : public BaseArgs
    // {
    // public:
    //     property String^ DevName;
    //     property int ChanNum;
    //     property bool IsOutput; // else input
    //     property int Patch;     // output only
    // };

    // /// <summary>Script wants to send a midi event.</summary>
    // public ref class SendArgs : public BaseArgs
    // {
    // public:
    //     property int ChanHnd;
    //     property bool IsNote;   // else controller
    //     property int What;      // note number or controller id
    //     property int Value;     // note velocity or controller payload
    // };

    // /// <summary>Script has something to say to host.</summary>
    // public ref class PropertyArgs : public BaseArgs
    // {
    // public:
    //     property int Bpm;       // Tempo - optional
    // };

    // /// <summary>Script wants to log something.</summary>
    // public ref class LogArgs : public BaseArgs
    // {
    // public:
    //     property int LogLevel;
    //     property String^ Msg;
    // };
    // #pragma endregion
}
