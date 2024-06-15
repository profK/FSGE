namespace SilkDevices

open System
open System.Text.RegularExpressions
open Devices
open ManagerRegistry
open Silk.NET.Input
open SilkGraphicsOGL.WindowGL
open FSharp.Collections
open SilkScanCodeConversion

type SilkDeviceContext(silkInputContext:IInputContext) =

        interface DeviceContext
        member val Context = silkInputContext with get
        member val  Values:Map<string,DeviceValue> = Map.empty with get,set
        member val  Devices:DeviceNode seq = []  with get,set  

module SilkDevices =
    let makeMouseButtonNodeList parentPath (mouseButtons:MouseButton seq) =
        mouseButtons
        |> Seq.map (fun mouseButton  ->
                        let name = mouseButton.ToString()
                        let path = parentPath + $".{name}"
                        {Name = name
                         Type = DeviceType.Button
                         Children = None
                         Path=path})
    let makeButtonIndexNodeList parentPath (buttons:Button seq) =
        buttons
        |> Seq.map (fun (button:Button)  ->
                        let name = $"Button{button.Index}"
                        { Name = name
                          Type = DeviceType.Button
                          Children = None
                          Path= parentPath + $".{name}"
                        })
          
    let makeButtonNameNodeList parentPath (buttons:Button seq) =
        buttons
        |> Seq.map (fun button ->
                         let name = button.Name.ToString()
                         {
                             Name=name
                             Type=DeviceType.Button
                             Children = None
                             Path = parentPath+ $".{name}"
                         })  
    let makeScrollWheelNodeList parentPath scrollWheels =
        scrollWheels
        |> Seq.mapi (fun mouseButton i ->
                        let name = $"scrollwheel{i}"
                        {Name = name
                         Type = DeviceType.Collection
                         Children = Some [
                             {Name = "x"; Type = DeviceType.Axis; Children = None;Path=parentPath + $".{name}.x"}
                             {Name = "y"; Type = DeviceType.Axis; Children = None;Path=parentPath + $".{name}.y"}
                         ]
                         Path=parentPath+name})
        
    
        
    let makeThumbstickNodeList parentPath (thumbSticks:Thumbstick seq)=
        thumbSticks
        |> Seq.map (fun thumbstick ->
                        let name = $"thumbStick{thumbstick.Index}"
                        let path = parentPath + $".{name}"
                        {Name = name
                         Type = DeviceType.Collection
                         Children = Some [
                             {Name = "x"; Type = DeviceType.Axis; Children = None; Path = path+".x"}
                             {Name = "y"; Type = DeviceType.Axis; Children = None; Path = path+".y" }
                         ]
                         Path=path
                         })
                  
    let makeTriggerNodeList parentPath (triggers:Trigger seq) =
        triggers
        |> Seq.map (fun (trigger:Trigger)  ->
                        let name = $"trigger{trigger.Index}"
                        {Name =name
                         Type = DeviceType.Button
                         Children = None
                         Path = parentPath + $".{name}"
                         })  
            

    let makeJoystickNode i (joystick:IJoystick)  =
        let name = $"Joystick{i}"
        let buttons =
            joystick.Buttons
            |> makeButtonIndexNodeList name
        let axes =
            joystick.Axes
            |> Seq.mapi (fun i axis ->
                            {Name = $"axis{i}"
                             Type = DeviceType.Axis
                             Children = None
                             Path=name})
        let children = [buttons;axes] |> Seq.concat
        {Name=name;Type=Collection;Children=Some children;Path=name}
   
    
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
        ctlr.add_ThumbstickMoved(fun (pad:IGamepad)(thumbstick:Thumbstick ) ->
            let xname = name +  $"thumbStick{thumbstick.Index}.x"
            let yname = name + $"thumbStick{thumbstick.Index}.y"
            ctxt.Values <- ctxt.Values.Change(xname,fun devValue -> Some (AxisValue (float thumbstick.X)))
            ctxt.Values <- ctxt.Values.Change(yname,fun devValue -> Some (AxisValue (float thumbstick.Y)))
        )
        ctlr.add_TriggerMoved(fun (pad:IGamepad)(trigger:Trigger) ->
            let tname = name + $"trigger{trigger.Index}"
            ctxt.Values <- ctxt.Values.Change(tname,fun devValue -> Some (AxisValue (float trigger.Position)))
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
        
    let addMouseCallbacks (ctxt:SilkDeviceContext) (mouse:IMouse) name =
        mouse.add_MouseDown(
            fun mouse (button:MouseButton) ->
                let bname = name + $"{button.ToString()}"
                ctxt.Values <- ctxt.Values.Change(bname,fun v -> Some (ButtonValue true))
        )
        mouse.add_MouseUp(
            fun mouse (button:MouseButton) ->
                let bname = name + $"{button.ToString()}"
                ctxt.Values <- ctxt.Values.Change(bname,fun v -> Some (ButtonValue false))
        )
        mouse.add_Scroll(
            fun mouse (scroll:ScrollWheel) ->
                // Note that the api sends  no identifying info for the scroll wheel
                //So this code only supports 1 scroll wheel per mouse
                let xname = name + $".scrollWheel0.x"
                let yname = name + $".scrollwheel0.y"
                ctxt.Values <- ctxt.Values.Change(xname,fun v -> Some (AxisValue (float scroll.X)))
                ctxt.Values <- ctxt.Values.Change(yname,fun v -> Some (AxisValue (float scroll.Y)))
        )
        mouse.add_MouseMove(fun (mouse:IMouse) pos ->
            let xname = name + ".position.x"
            let yname = name + ".position.y"
            ctxt.Values <- ctxt.Values.Change(xname,fun v -> Some (AxisValue (float pos.X)))
            ctxt.Values <- ctxt.Values.Change(yname,fun v -> Some (AxisValue (float pos.Y)))
        )
    let scanDevices (context:SilkDeviceContext) =
        let mouseNodes =
            context.Context.Mice
            |> Seq.map (fun (mouse:IMouse) ->
                            let name = $"Mouse{mouse.Index}"
                            addMouseCallbacks context mouse name
                            let buttons = makeMouseButtonNodeList name mouse.SupportedButtons
                            let scrollWheels = makeScrollWheelNodeList name mouse.ScrollWheels
                            let position = [{Name="position";Type=DeviceType.Collection;Children=Some [
                                {Name="x";Type=DeviceType.Axis;Children=None;Path=name+".position.x"}
                                {Name="y";Type=DeviceType.Axis;Children=None;Path=name+".position.y"}
                            ];Path=name+".position"}]
                            let children = [buttons;scrollWheels;position]|>Seq.concat
                            {Name=name;Type=DeviceType.Mouse;Children=
                                Some children;Path=name})
        let controllerNodes =
            context.Context.Gamepads
            |> Seq.mapi (fun i (ctlr:IGamepad)  ->
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
            
                            let children = [ctlrButtons;ctlrThumbsticks;ctlrTriggers] |>Seq.concat
                            addGamepadCallbacks context ctlr name
                            {Name=name;Type=Collection;Children=Some children; Path=name})
        let joystickNodes =
            context.Context.Joysticks
            |> Seq.mapi makeJoystickNode
        let keyboardNodes =
            context.Context.Keyboards
            |> Seq.mapi (fun i kb ->
                            let name = $"Keyboard{i}"
                            addKeyboardCallbacks context kb name
                            {Name=name;Type=DeviceType.Keyboard;Children=None;Path=name})
        [mouseNodes;controllerNodes;joystickNodes;keyboardNodes] |> Seq.concat
        
            
    
       
            