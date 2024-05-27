using System;
using System.Windows.Forms;


namespace Splunk.Test
{

// - Explorer
// keyboard shortcuts (incl explorer):
// https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec

// - powershell
// https://learn.microsoft.com/en-us/powershell/?view=powershell-7.2
// https://ss64.com/ps/powershell.html
// open a new explorer instance:
// powershell.exe -command Invoke-Item C:\Temp -> not
// Easy enough from PowerShell using the shell.application com object. 
//
// HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Directory\background\shell\Powershell\command:
// powershell.exe -noexit -command Set-Location -literalPath '%V'



//https://www.geoffchappell.com/studies/windows/shell/explorer/index.htm?tx=28
//A typical occasion is when a shell namespace object is to be opened by calling the
//SHELL32 function ShellExecuteEx and the database of file associations names EXPLORER.EXE
//as the program to run for applying the requested verb to the object. 
//
// A typical occasion is when a shell namespace object is to be opened by calling the SHELL32 function
// ShellExecuteEx and the database of file associations names EXPLORER.EXE as the program to run for applying
// the requested verb to the object. 
//
// The Windows Explorer Command Line
// The EXPLORER command line is a sequence of fields with commas and equals signs serving as separators. To allow commas
// and equals signs within a field, there is a facility for enclosure by double-quotes. The double-quotes are otherwise
// ignored, except that two consecutive double-quotes in the command line pass into the extracted field as one
// literal double-quote. White space is ignored at the start and end of each field.

// Each argument for EXPLORER is one or more fields, shown below as if separated only by commas and without the complications
// of white space or quoting. Where the first field is a command-line switch, necessarily beginning with the forward
// slash, it is case-insensitive.
//
// /e   
// /idlist,:handle:process
//     specifies object as ITEMIDLIST in shared memory block with given handle in context of given process
//
// /n
//     redundant in Windows Vista
// /root,/idlist,:handle:process
// /root,clsid
// /root,clsid,path
// /root,path
//     specifies object as root
//
// /select
//     show object as selected item in parent folder
//
// /separate
//     show in separate EXPLORER process
//
// path
//     specifies object;
//     ignored if object already specified;
//     overridden by specification in later /idlist or /root argument
//
// The overall aim of the command line is to specify a shell namespace object and a way in which EXPLORER is to show that object.
    
    public class ExplorerStuff
    {
        public void Do()
        {
            int l = (int)MouseButtons.Left; // 0x00100000
            int m = (int)MouseButtons.Middle; // 0x00400000
            int r = (int)MouseButtons.Right; // 0x00200000

            // (explorer middle button?) ctrl-T opens selected in new tab
        }

        public void DoThisMaybe()
        {
            // In my code, I am using VirtualKeyCode.VK_4 because the File Explorer shortcut is the fourth item after
            // the standard Windows icons. Change the VK_4 value to match the position of File Explorer on your taskbar
            // (note: File Explorer needs to be pinned to the taskbar, otherwise it won't work).
            // On my notebook, a delay of at least 250 milliseconds is required (sim.Keyboard.Sleep(250);) before
            // simulating CTRL+T to add a new tab, change this value according to the power of your processor.
            // The application is of the Console type, using C# .NET 6, with the InputSimulator NuGet to detect
            // the pressed keys

            //Simulate LWIN+4
            //var sim = new InputSimulator();
            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_4);
            //sim.Keyboard.Sleep(250);
            ////Simulate CTRL+T
            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_T);
        }
    }
}