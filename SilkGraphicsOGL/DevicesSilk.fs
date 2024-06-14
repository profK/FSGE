module SilkDevices

open System
open System.Text.RegularExpressions
open Devices
open ManagerRegistry
open Silk.NET.Input
open SilkGraphicsOGL.WindowGL
open FSharp.Collections
open SilkScanCodeConversion
module Seq =
    let foldi func state inseq =
        let result =
            inseq
            |> Seq.fold (fun stateTuple mbr ->
                         (func (fst stateTuple) mbr (snd stateTuple)),
                            (snd stateTuple)+1
                         )
                         (state,0)
        fst result
        
type SilkDeviceContext(silkInputContext:IInputContext) =
    interface DeviceContext
    member val Context = silkInputContext with get
    member val Values:Map<string,DeviceValue> = Map.empty with get,set
   
 
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkInputManager() =
        
    let makeMouseButtonNodeList parentPath (mouseButtons:MouseButton seq) =
        mouseButtons
        |> Seq.fold (fun state mouseButton  ->
                        let name = mouseButton.ToString()
                        let path = parentPath + $".{name}"
                        {Name = name
                         Type = DeviceType.Button
                         Children = None
                         Path=path}::state) List.Empty
    let makeButtonIndexNodeList parentPath (buttons:Button seq) =
        buttons
        |> Seq.fold (fun state button  ->
                        let name = $"Button{button.Index}"
                        { Name = name
                          Type = DeviceType.Button
                          Children = None
                          Path= parentPath + $".{name}"
                        }::state) List.Empty
          
    let makeButtonNameNodeList parentPath (buttons:Button seq) =
        buttons
        |> Seq.fold (fun state button ->
                         let name = button.Name.ToString()
                         {
                             Name=name
                             Type=DeviceType.Button
                             Children = None
                             Path = parentPath+ $".{name}"
                         }::state) List.Empty    
    let makeScrollWheelNodeList parentPath scrollWheels =
        scrollWheels
        |> Seq.foldi (fun state mouseButton i ->
                        let name = $"scrollwheel{i}"
                        {Name = name
                         Type = DeviceType.Collection
                         Children = Some [
                             {Name = "x"; Type = DeviceType.Axis; Children = None;Path=parentPath + $".{name}.x"}
                             {Name = "y"; Type = DeviceType.Axis; Children = None;Path=parentPath + $".{name}.y"}
                         ]
                         Path=parentPath+name}::state) List.Empty
        
    let makeMouseNode state (mouse:IMouse) i =
        let name = $"Mouse{i}"
        let mouseButtons =
            mouse.SupportedButtons
            |> makeMouseButtonNodeList name
        let scrollWheels =
            mouse.ScrollWheels
            |> makeScrollWheelNodeList name
        let position = [{Name="position"
                         Type=Collection
                         Children=
                             Some [
                                {Name="x";Type=DeviceType.Axis;Children=None;Path=name+".position.x"}
                                {Name="y";Type=DeviceType.Axis;Children=None;Path=name+".position.y"}
                             ]
                         Path=name+".position"
                      }]  
        let children = [mouseButtons;scrollWheels;position] |>List.concat
        {Name=name;Type=Collection;Children=Some children;Path=name}::state
        
    let makeThumbstickNodeList parentPath (thumbSticks:Thumbstick seq)=
        thumbSticks
        |> Seq.fold (fun state thumbstick ->
                        let name = $"thumbStick{thumbstick.Index}"
                        let path = parentPath + $".{name}"
                        {Name = name
                         Type = DeviceType.Collection
                         Children = Some [
                             {Name = "x"; Type = DeviceType.Axis; Children = None; Path = path+".x"}
                             {Name = "y"; Type = DeviceType.Axis; Children = None; Path = path+".y" }
                         ]
                         Path=path
                         }::state) List.Empty    
                  
    let makeTriggerNodeList parentPath (triggers:Trigger seq) =
        triggers
        |> Seq.fold (fun state (trigger:Trigger)  ->
                        let name = $"trigger{trigger.Index}"
                        {Name =name
                         Type = DeviceType.Button
                         Children = None
                         Path = parentPath + $".{name}"
                         }::state) List.Empty    
            
    let makeControllerNode state (ctlr:IGamepad) i =
        let name = $"Gamepad{i}"
        let ctlrButtons =
            ctlr.Buttons
            |> makeButtonNameNodeList name
        let ctlrThumbsticks =
            ctlr.Thumbsticks
            |> makeThumbstickNodeList name
        let ctlrTriggers = 
            ctlr.Triggers
            |> makeTriggerNodeList name
            
      
        let children = [ctlrButtons;ctlrThumbsticks;ctlrTriggers] |>List.concat
        {Name=name;Type=Collection;Children=Some children; Path=name}::state    
    let makeJoystickNode state (joystick:IJoystick) i =
        let name = $"Joystick{i}"
        let buttons =
            joystick.Buttons
            |> makeButtonIndexNodeList name
        let axes =
            joystick.Axes
            |> Seq.foldi (fun state axis i ->
                            {Name = $"axis{i}"
                             Type = DeviceType.Axis
                             Children = None
                             Path=name}::state) List.Empty
        let children = [buttons;axes] |> List.concat
        {Name=name;Type=Collection;Children=Some children;Path=name}::state
   
    let getInputContext (window:Graphics2D.Window) =
            //could be wasteful, measure and buffer if necc
            (window :?> SilkWindow).SilkWindow.CreateInput()
            
    let splitOrdinal id =
        Regex.Match(id, @"([a-zA-Z]+)(\d+)")
    let addKeyboardCallbacks (ctxt:SilkDeviceContext) (kb:IKeyboard) name=
         kb.add_KeyDown(
            fun kb key  i ->
                ctxt.Values <-
                    ctxt.Values.Change( name, fun valueOpt ->
                        match valueOpt with
                        | Some deviceNode ->
                            match deviceNode with
                            | KeyboardValue(keyList) ->
                                let newList = keyList |> Array.append [|uint32 key|]
                                Some(KeyboardValue(newList))
                            | _ ->
                                failwith "Attempt to add key to non-keyboard list"
                        | None ->
                            Some(KeyboardValue([|uint32 key|]))
                    )
         )
         kb.add_KeyUp(
            fun kb key  i ->
                ctxt.Values <-
                    ctxt.Values.Change( name, fun valueOpt ->
                        match valueOpt with
                        | Some deviceNode ->
                            match deviceNode with
                            | KeyboardValue(keyList) ->
                                let newList = keyList |> Array.filter (fun k -> k <> uint32 key)
                                Some(KeyboardValue(newList))
                            | _ ->
                                failwith "Attempt to remove key from non-keyboard list"
                        | None ->
                            Some(KeyboardValue([||]))
                    )
         )
         
    let addGamepadCallbacks (ctxt:SilkDeviceContext) (ctlr:IGamepad) name=
        ctlr.add_ButtonDown(
            fun ctlr (button:Button) ->
                let bname = name + $".{button.Name}"
                ctxt.Values <- ctxt.Values.Change (bname, fun devValue ->
                    Some (ButtonValue true))      
         )
         
        ctlr.add_ButtonUp(
             fun ctlr (button:Button) ->
                let bname = name + $".{button.Name}"
                ctxt.Values <- ctxt.Values.Change (bname, fun devValue ->
                    Some (ButtonValue false)  )   
         )
    let addJoystickCallbacks (ctxt:SilkDeviceContext) (joystick:IJoystick) =
        joystick.add_ButtonDown ( fun joystick (button:Button) ->
            let bname = $"Joystick{joystick.Index}.Button{button.Index}"
            ctxt.Values <- ctxt.Values.Change(bname,fun v -> Some (ButtonValue true))
        )
        joystick.add_ButtonUp ( fun joystick (button:Button) ->
            let bname = $"Joystick{joystick.Index}.Button{button.Index}"
            ctxt.Values <- ctxt.Values.Change(bname,fun v -> Some (ButtonValue false))
        )
            
    interface IDeviceManager with
        member this.tryGetDeviceContext window =
            let inputContext = getInputContext window
            Some (SilkDeviceContext(inputContext))
        member this.GetDeviceTree deviceContext  =
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            let ctxt = silkCtxt.Context
            let kbNodelist =
                ctxt.Keyboards  
                |> Seq.foldi (fun state (kb:IKeyboard) i ->
                                let name = $"Keyboard{i}"
                                addKeyboardCallbacks silkCtxt kb name
                                let newkb =
                                    {Name=name; Type= Keyboard; Children=None; Path=name}
                                newkb::state ) List.Empty
            let mouseList =
                ctxt.Mice
                |> Seq.foldi makeMouseNode List.Empty
            let controllerList =
                ctxt.Gamepads
                |> Seq.foldi (fun state (ctlr:IGamepad) i ->
                                let name = $"Gamepad{i}"
                                let ctlrButtons =
                                    ctlr.Buttons
                                    |> makeButtonNameNodeList name
                                let ctlrThumbsticks =
                                    ctlr.Thumbsticks
                                    |> makeThumbstickNodeList name
                                let ctlrTriggers = 
                                    ctlr.Triggers
                                    |> makeTriggerNodeList name
                                let children = [ctlrButtons;ctlrThumbsticks;ctlrTriggers] |>List.concat
                                addGamepadCallbacks silkCtxt ctlr name
                                {Name=name;Type=Collection;Children=Some children; Path=name}::state) List.Empty
                                
            let joystickList =
                ctxt.Joysticks
                |> Seq.foldi (fun state (joystick:IJoystick) i ->
                        let name = $"Joystick{i}"
                        let buttons =
                            joystick.Buttons
                            |> makeButtonIndexNodeList name
                        let axes =
                            joystick.Axes
                            |> Seq.foldi (fun state axis i ->
                                            {Name = $"axis{i}"
                                             Type = DeviceType.Axis
                                             Children = None
                                             Path=name}::state) List.Empty
                        let children = [buttons;axes] |> List.concat
                        addJoystickCallbacks silkCtxt joystick
                        {Name=name;Type=Collection;Children=Some children;Path=name}::state
                    )List.Empty    
            
            [kbNodelist;mouseList;controllerList]
            |>List.concat
            
        member this.tryGetDeviceValue deviceContext path =
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            let valueOpt = Map.tryFind path silkCtxt.Values
            valueOpt

        member this.MapPlatformScanCodeToHID var0 =
            mapSilkToHID var0
            
        member this.GetDeviceValuesMap deviceContext  =    
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            silkCtxt.Values
       
            