module InputManagerWinRawInput

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open ManagerRegistry
open RawInputLight
open Devices
open Windows.Win32.Devices.HumanInterfaceDevice
open Windows.Win32.Foundation
open DeviceValueCollector
 
type DeviceContext =
    {
        rawInput: RawInput
    }
    interface Devices.DeviceContext

let MouseNode  (devInfo:DeviceInfo, parentPath:string) =
           {
               Name = devInfo.Names.Product
               Type = DeviceType.Mouse
               Children = None
               Path = parentPath + "." + devInfo.Names.devPath
           }
           
let KeyboardDeviceNode (devInfo:DeviceInfo, parentPath:string) =
           {
               Name = devInfo.Names.Product
               Type = DeviceType.Keyboard
               Children = None
               Path = parentPath + "." + devInfo.Names.devPath
           }
let JoystickDeviceNode (devInfo:DeviceInfo, parentPath:string) =
           {
               Name = devInfo.Names.Product
               Type = DeviceType.Joystick
               Children = None
               Path = parentPath + "." + devInfo.Names.devPath
           }
           


[<Manager("Input interface for windows Raw Input",
          supportedSystems.Windows )>]
type InputManagerWinRawInput() as this =
       let mutable context: DeviceContext option = None
       let mutable oldStateMap = Map.empty
       let axisStateCollector = DeviceValueCollector()
           
       let doKbEvent (devh:HANDLE) (asc:uint16) (keystate:KeyState):unit =
            let devInfo:Nullable<DeviceInfo> = NativeAPI.GetDeviceInfo(devh)
            if devInfo.HasValue then
                axisStateCollector.SetKeyboardAxis(
                    devInfo.Value.Names.Product, char asc,keystate)
                |> ignore
       
       let doMouseEvent (devh:HANDLE) (dx:int) (dy:int) (buttons:UInt32)
                        (dWheel:int):unit =
        try 
            let devInfo:Nullable<DeviceInfo> = NativeAPI.GetDeviceInfo(devh)
            if devInfo.HasValue then
                axisStateCollector.SetAnalogAxis(devInfo.Value.Names.Product+".deltaX", dx) |> ignore
                axisStateCollector.SetAnalogAxis(devInfo.Value.Names.Product+".deltaY", dx) |> ignore
                [0..3]
                |> Seq.iter (fun (buttonNum:int) ->
                        let bitVal:UInt32 = uint32 1<<<(buttonNum*2)
                        if  (bitVal &&& buttons) = bitVal then
                            axisStateCollector.SetDigitalAxis(
                                           devInfo.Value.Names.Product+".button"+
                                           buttonNum.ToString(),true) |> ignore
                        else
                            axisStateCollector.SetDigitalAxis(
                                           devInfo.Value.Names.Product+".button"+
                                           buttonNum.ToString(),false) |> ignore
                    )
                if (buttons &&& 0x0400ul ) = 0x0400ul then
                    axisStateCollector.SetAnalogAxis(
                            devInfo.Value.Names.Product+ ".deltaWheel",
                                    dWheel)
                    |> ignore
            else
                ()
        with 
            | ex -> Console.WriteLine(ex.Message)  // dirty but I can't find a win32 call to check devid
            
       let uint32ToHidUsage  (usage:uint32):HIDDesktopUsages =
           let hid:HIDDesktopUsages =
               LanguagePrimitives.EnumOfValue usage
           hid
       let doButtonDownEvent (devh:HANDLE) (usageBase:UInt32) (values:bool[]) =
            let devInfo:Nullable<DeviceInfo> = NativeAPI.GetDeviceInfo(devh)
            if devInfo.HasValue then
                [0..values.Length-1]
                |> Seq.iter (fun (index:int) ->
                        let usage:HIDDesktopUsages =
                            uint32ToHidUsage (usageBase + uint32 index) 
                        let name = devInfo.Value.Names.Product + "." + usage.ToString()
                        axisStateCollector.SetDigitalAxis(name,values[index]) |> ignore
                    )
            else
                ()
                
       let doAxisChangeEvent(devh:HANDLE) (usages:uint32[]) (values:uint32[]) =
            let devInfo:Nullable<DeviceInfo> = NativeAPI.GetDeviceInfo(devh)
            if devInfo.HasValue then
                [0..usages.Length-1]
                |> Seq.iter (fun index ->
                        let hidUsage:HIDDesktopUsages =
                            LanguagePrimitives.EnumOfValue usages[index]
                        let name = devInfo.Value.Names.Product+"."+
                                   hidUsage.ToString()
                        axisStateCollector.SetAnalogAxis(
                            name,float values[index])
                        |> ignore       
                    )
       let startedEvent = new ManualResetEvent(false)
       let messagePump():unit =
           let wrapper = NativeAPI.OpenWindow()
           let rawInput = RawInput(wrapper)
           context <- Some { rawInput = rawInput }
           rawInput.add_KeyStateChangeEvent (
                Action<HANDLE,uint16, KeyState>(doKbEvent))
           rawInput.add_MouseStateChangeEvent(
               Action<HANDLE,int,int,UInt32,int>(doMouseEvent))
           rawInput.add_ButtonDownEvent(
               Action<HANDLE, UInt32, bool[]>(doButtonDownEvent))
           rawInput.add_AxisEvent(
               Action<HANDLE,uint32[], uint32[]>(doAxisChangeEvent))
           startedEvent.Set()
           NativeAPI.MessagePump(wrapper)
       let messagePumpThread =
           Thread(ThreadStart(messagePump))
 
       do
           messagePumpThread.Start()
           startedEvent.WaitOne() |> ignore
       
       //member val RawInput = rawInput with get
       member val PumpThread = messagePumpThread with get
       
       interface IDeviceManager with
            member this.tryGetDeviceContext window : Devices.DeviceContext option =
                match context with
                | Some ctx -> Some (ctx :> Devices.DeviceContext)
                | None -> None
                
            member this.PollDevices context : unit =
                () // polling is done in the message pump thread
            member this.tryGetDeviceValue context string : DeviceValue option =
                let stateMap = axisStateCollector.GetState()
                match stateMap.TryFind string with
                | Some value -> Some value
                | None -> None
            member this.GetDeviceTree context : DeviceNode seq =
                    match this.Controllers() with
                    | Some devices -> devices
                    | None -> Seq.empty
               
            member this.MapPlatformScanCodeToHID incode : uint32 =
                // In this case its the identity function
                incode
            member this.MapHIDToPlatformScanCode incode : uint32 =
                // In this case its the identity function
                incode
            member this.GetDeviceValuesMap context : Map<string,DeviceValue> =
                axisStateCollector.GetState()
            member this.CloseDeviceContext context : unit =
                () // nothing to close
           
       member this.Controllers() =
           NativeAPI.RefreshDeviceInfo()
          
           NativeAPI.GetDevices()
           |> Array.fold(fun state (devInfo:DeviceInfo) ->
                   // Console.WriteLine(devInfo.Names.Product+":"+
                    //                  devInfo.DeviceCaps.Usage.ToString())
                    let usage:HIDDesktopUsages =
                        Microsoft.FSharp.Core.LanguagePrimitives.
                            EnumOfValue<uint, HIDDesktopUsages>(
                                ((uint devInfo.DeviceCaps.UsagePage)<<<16)|||
                                 uint devInfo.DeviceCaps.Usage)
                    match usage  with
                    | HIDDesktopUsages.GenericDesktopMouse ->
                        MouseNode(devInfo,""):: state
                    | HIDDesktopUsages.GenericDesktopKeyboard ->
                        KeyboardDeviceNode(devInfo,""):: state
                    | HIDDesktopUsages.GenericDesktopJoystick ->
                        JoystickDeviceNode(devInfo,""):: state
                    | _ -> state
               ) List.Empty
            |> Some
              
      
       member this.PollState() = 
           axisStateCollector.GetState()
              
           
