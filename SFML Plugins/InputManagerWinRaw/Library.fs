namespace InputManagerWinRaw

open System
open System.Threading
open RawInputLight
open Devices

type WinRawDeviceContext =
        {
            WindowHandle: NativeAPI.HWND_WRAPPER
        }
        with interface DeviceContext
    

        
type InputManagerWinRaw() as this=
   

    [<STAThread>]
    let inputThread() =
        NativeAPI.OpenWindow()
        |> fun (window:NativeAPI.HWND_WRAPPER) ->
            this.windowHandle <- Some ({WindowHandle = window} :> DeviceContext)
            this.RawInputOpt <- Some(RawInput(window))
            NativeAPI.MessagePump(window)
        
    let thread  = Thread(ThreadStart(inputThread)).Start()
    
    member val windowHandle : DeviceContext option = None
        with get, set
    member val RawInputOpt:RawInput option = None with get,set   
        
    interface IDeviceManager with
        member this.CloseDeviceContext(var0) =
            ()
        member this.GetDeviceTree(var0) =
            NativeAPI.GetDevices()
            |> Seq.map (fun d ->
                {
                    Name = d.Names.devPath
                    Type =
                        match d.
                        | RawInputLight.DeviceType.Keyboard -> DeviceType.Keyboard
                        | RawInputLight.DeviceType.Mouse -> DeviceType.Mouse
                        | RawInputLight.DeviceType.HID -> DeviceType.Collection
                    Children = None
                    Path = d.Path
                }
            )
        member this.GetDeviceValuesMap(var0) = failwith "todo"
        member this.MapHIDToPlatformScanCode(var0) = failwith "todo"
        member this.MapPlatformScanCodeToHID(var0) = failwith "todo"
        member this.PollDevices(var0) = failwith "todo"
        member this.tryGetDeviceContext(var0) =
            this.windowHandle
        member this.tryGetDeviceValue var0 var1 = failwith "todo"
        
        
        
        