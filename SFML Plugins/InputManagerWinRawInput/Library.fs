module InputManagerWinRawInput

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open ManagerRegistry
open RawInputLight
open TDE3ManagerInterfaces.InputDevices
open TwoDEngine3.ManagerInterfaces.InputManager
open Windows.Win32.Devices.HumanInterfaceDevice
open Windows.Win32.Foundation
open AxisStateCollector
 

 
type AxisNode(parent:Node,name) =
        interface Node with
            member val Name:string = name with get
            member val Parent: Node option = Some(parent) with get
            member this.Path:string=parent.Path+"."+name 
            member val Value= Axis(Analog) // value is ignored
            
type ButtonNode(parent:Node,name) =
     interface Node with
        member val Name:string = name with get
        member val Parent: Node option = Some(parent) with get
        member this.Path:string=parent.Path+"."+name 
        member val Value= Axis(Digital) // value is ignored
                
type MouseNode(devInfo:DeviceInfo) as this=
    let name = devInfo.Names.Product
    
    interface Node with
        member val Name:string = name with get
        member val Parent: Node option = None with get
        member this.Path:string=name 
        member val Value= Children(
                [AxisNode(this,"deltaX");AxisNode(this,"deltaY");AxisNode(this,"deltaWheel")
                 ButtonNode(this,"button0");ButtonNode(this,"button1");ButtonNode(this,"button2")
                 ButtonNode(this,"button3")]
            ) with get
        
type KeyboardNode(devInfo:DeviceInfo) as this =
    let name = devInfo.Names.Product
    
    interface Node with
        member val Name:string = name with get
        member val Parent: Node option = None with get
        member this.Path:string=name
        member val Value= Axis(Keyboard)
            
type JoystickNode(devInfo:DeviceInfo) as this =
    let MakeUsage(usage) =
         Microsoft.FSharp.Core.LanguagePrimitives.
            EnumOfValue<uint, HIDDesktopUsages>(uint usage)
    let MakeButtonNodes (buttonCaps:HIDP_BUTTON_CAPS array) =
        buttonCaps
        |> Array.fold(fun state (buttonCap:HIDP_BUTTON_CAPS) ->
            
            match buttonCap.IsRange.Value with
            | 0uy -> //FALSE
                let name = MakeUsage(
                    (uint32 buttonCap.UsagePage<<<16) |||
                    (uint32 buttonCap.Anonymous.NotRange.Usage))
                ButtonNode(this, name.ToString()):> Node ::state // false
            | _ -> // TRUE
                [buttonCap.Anonymous.Range.UsageMin..buttonCap.Anonymous.Range.UsageMax]
                |> List.fold(fun state usageNum ->
                        let name = MakeUsage(
                            (uint32 buttonCap.UsagePage<<<16) ||| uint32 usageNum)
                        ButtonNode(this,name.ToString()):>Node ::state
                    ) state
            ) List.Empty
        
    let MakeValueNodes (valueCaps:HIDP_VALUE_CAPS array) =
        valueCaps
        |> Array.fold(fun state (valueCap:HIDP_VALUE_CAPS) ->
            match valueCap.IsRange.Value with
            | 0uy -> //FALSE
                let name = MakeUsage(
                    (uint32 valueCap.UsagePage<<<16) |||
                    (uint32 valueCap.Anonymous.NotRange.Usage))
                AxisNode(this, name.ToString()):> Node ::state // false
            | _ -> // TRUE
                [valueCap.Anonymous.Range.UsageMin..valueCap.Anonymous.Range.UsageMax]
                |> List.fold(fun state usageNum ->
                        let name = MakeUsage(
                            (uint32 valueCap.UsagePage<<<16) ||| uint32 usageNum)
                        AxisNode(this,name.ToString()):>Node ::state
                    ) state
            ) List.Empty
        
    let name = devInfo.Names.Product
    
    interface Node with
        member val Name:string = name with get
        member val Parent: Node option = None with get
        member this.Path:string=name 
        member val Value= Children(
            [
                devInfo.ButtonCaps
                |> MakeButtonNodes
                devInfo.ValueCaps
                |> MakeValueNodes
            ]
            |> List.concat
            )

[<Manager("Input interface for windows Raw Input",
          supportedSystems.Windows )>]
type InputManagerWinRawInput() as this =
       let mutable rawInput: RawInput option = None
       let mutable oldStateMap = Map.empty
       let  axisStateCollector = AxisStateCollector()
           
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
                axisStateCollector.DeltaAnalogAxis(devInfo.Value.Names.Product+".deltaX", dx) |> ignore
                axisStateCollector.DeltaAnalogAxis(devInfo.Value.Names.Product+".deltaY", dx) |> ignore
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
                    axisStateCollector.DeltaAnalogAxis(
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
       let messagePump():unit =
           NativeAPI.OpenWindow()
           |> fun wrapper ->
               rawInput <- Some(RawInput(wrapper))
               rawInput.Value.add_KeyStateChangeEvent (
                    Action<HANDLE,uint16, KeyState>(doKbEvent))
               rawInput.Value.add_MouseStateChangeEvent(
                   Action<HANDLE,int,int,UInt32,int>(doMouseEvent))
               rawInput.Value.add_ButtonDownEvent(
                   Action<HANDLE, UInt32, bool[]>(doButtonDownEvent))
               rawInput.Value.add_AxisEvent(
                   Action<HANDLE,uint32[], uint32[]>(doAxisChangeEvent))
               NativeAPI.MessagePump(wrapper)
       let messagePumpThread =
           Thread(ThreadStart(messagePump))
 
       do messagePumpThread.Start()
       
       member val RawInput = rawInput with get
       member val PumpThread = messagePumpThread with get
       
       interface InputDeviceInterface with
           member this.Controllers() =
               NativeAPI.RefreshDeviceInfo()
               NativeAPI.LastError
               |> function
                   |0u ->
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
                                    MouseNode(devInfo):>Node :: state
                                | HIDDesktopUsages.GenericDesktopKeyboard ->
                                    KeyboardNode(devInfo):>Node :: state
                                | HIDDesktopUsages.GenericDesktopJoystick ->
                                    JoystickNode(devInfo):>Node :: state
                                | _ -> state
                           ) List.Empty
                        |> Some
                   | _  ->
                       printfn $"Error code in input: {NativeAPI.LastError} "
                       None
          
           member this.PollState() = 
               axisStateCollector.GetState()
              
           
