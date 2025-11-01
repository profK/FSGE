namespace InputManagerWinRaw

open System
open System.Threading
open RawInputLight
open TDE3ManagerInterfaces.InputDevices


type InputManagerWinRaw() as this=
    [<STAThread>]
    let inputThread() =
        NativeAPI.OpenWindow()
        |> fun (window:NativeAPI.HWND_WRAPPER) ->
            this.RawInputOpt <- Some(RawInput(window))
            NativeAPI.MessagePump(window)
        
    let thread  = Thread(ThreadStart(inputThread)).Start()
    
      
    member val RawInputOpt:RawInput option = None with get,set   
        
    interface InputDeviceInterface with
        
        member val Controllers =                
            NativeAPI.GetDevices()
            |> Seq.map(fun dev ->
                )
        member val StateChanges = failwith "todo"
        
        