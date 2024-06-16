namespace SilkDevices

open System.Text.RegularExpressions
open Devices
open Logger
open Silk.NET.Input
open FSharp.Collections

type SilkDeviceContext(silkInputContext:IInputContext) =
    let mutable _values=Map.empty    // device node tree assmbly routines
    
    
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
    let splitOrdinal id =
        Regex.Match(id, @"([a-zA-Z]+)(\d+)")
    let addKeyboardCallbacks (kb:IKeyboard) name =
         kb.add_KeyDown(
            fun kb key  i ->
                _values <-
                    _values.Change( name, fun valueOpt ->
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
                _values <-
                    _values.Change( name, fun valueOpt ->
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
    let addGamepadCallbacks (ctlr:IGamepad) name=
        ctlr.add_ButtonDown(
            fun ctlr (button:Button) ->
                let bname = name + $".{button.Name}"
                _values <- _values.Change (bname, fun devValue ->
                    Some (ButtonValue true))      
         )
         
        ctlr.add_ButtonUp(
             fun ctlr (button:Button) ->
                let bname = name + $".{button.Name}"
                _values <- _values.Change (bname, fun devValue ->
                    Some (ButtonValue false)  )   
         )
        ctlr.add_ThumbstickMoved(fun (pad:IGamepad)(thumbstick:Thumbstick ) ->
            let xname = name +  $"thumbStick{thumbstick.Index}.x"
            let yname = name + $"thumbStick{thumbstick.Index}.y"
            _values <- _values.Change(xname,fun devValue -> Some (AxisValue (float thumbstick.X)))
            _values <-_values.Change(yname,fun devValue -> Some (AxisValue (float thumbstick.Y)))
        )
        ctlr.add_TriggerMoved(fun (pad:IGamepad)(trigger:Trigger) ->
            let tname = name + $"trigger{trigger.Index}"
            _values<- _values.Change(tname,fun devValue -> Some (AxisValue (float trigger.Position)))
        ) 
    let addJoystickCallbacks (joystick:IJoystick) name =
        joystick.add_ButtonDown ( fun joystick (button:Button) ->
            let bname = name + $".Button{button.Index}"
            _values <- _values.Change(bname,fun v -> Some (ButtonValue true))
        )
        joystick.add_ButtonUp ( fun joystick (button:Button) ->
            let bname = name + $".Button{button.Index}"
            _values <- _values.Change(bname,fun v -> Some (ButtonValue false))
        )
    let addMouseCallbacks (mouse:IMouse) name =
        mouse.add_MouseDown(
            fun mouse (button:MouseButton) ->
                let bname = name + $"{button.ToString()}"
                _values <- _values.Change(bname,fun v -> Some (ButtonValue true))
        )
        mouse.add_MouseUp(
            fun mouse (button:MouseButton) ->
                let bname = name + $"{button.ToString()}"
                _values <- _values.Change(bname,fun v -> Some (ButtonValue false))
        )
        mouse.add_Scroll(
            fun mouse (scroll:ScrollWheel) ->
                // Note that the api sends  no identifying info for the scroll wheel
                //So this code only supports 1 scroll wheel per mouse
                let xname = name + $".scrollWheel0.x"
                let yname = name + $".scrollwheel0.y"
                _values <- _values.Change(xname,fun v -> Some (AxisValue (float scroll.X)))
                _values <- _values.Change(yname,fun v -> Some (AxisValue (float scroll.Y)))
        )
        mouse.add_MouseMove(fun (mouse:IMouse) pos ->
            let xname = name + ".position.x"
            let yname = name + ".position.y"
            _values <- _values.Change(xname,fun v -> Some (AxisValue (float pos.X)))
            _values <-_values.Change(yname,fun v -> Some (AxisValue (float pos.Y)))
        )
    let scanDevices() =
        Logger.logMessage("Scanning devices")
        let mouseNodes =
            silkInputContext.Mice
            |> Seq.map (fun (mouse:IMouse) ->
                            let name = $"Mouse{mouse.Index}"              
                            let buttons = makeMouseButtonNodeList name mouse.SupportedButtons
                            let scrollWheels = makeScrollWheelNodeList name mouse.ScrollWheels
                            let position = [{Name="position";Type=DeviceType.Collection;Path=name;Children=Some [
                                {Name="x";Type=DeviceType.Axis;Children=None;Path=name+".position.x"}
                                {Name="y";Type=DeviceType.Axis;Children=None;Path=name+".position.y"}]
                            }]
                            addMouseCallbacks mouse name
                            let children = [buttons;scrollWheels;position]|>Seq.concat
                            {Name=name;Type=DeviceType.Mouse;Children=
                                Some children;Path=name})
        let controllerNodes =
            silkInputContext.Gamepads
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
                            addGamepadCallbacks ctlr name
                            {Name=name;Type=Collection;Children=Some children; Path=name})
        let joystickNodes =
            silkInputContext.Joysticks
            |> Seq.mapi (fun i (joystick:IJoystick)  ->
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
                addJoystickCallbacks joystick name
                {Name=name;Type=Collection;Children=Some children;Path=name})
        Logger.logMessage($"Scanning {silkInputContext.Keyboards.Count} keyboards")    
        let keyboardNodes =
            silkInputContext.Keyboards
            |> Seq.mapi (fun i (kb:IKeyboard) ->
                                let name = $"Keyboard{i}"
                                Logger.logMessage($"found keyboard {name}")
                                {Name=name;Type=DeviceType.Keyboard;Children=None;Path=name})
        // This ugly thing is to force evaluation because Seq.mapi is a lazy evaluator
        // but we need to force the side effects because we are dealing with a stateful input library           
        keyboardNodes
        |> Seq.iteri(fun i node ->
            let name = node.Path
            addKeyboardCallbacks (silkInputContext.Keyboards.[i]) name)
                            
                           
        [keyboardNodes;mouseNodes;controllerNodes;joystickNodes] |> Seq.concat 
        
    let _devices = scanDevices()
            
    interface DeviceContext
        member val Context = silkInputContext with get
        member this.Values 
            with get () = _values
        member this.Devices
            with get () = _devices   

    
               
            
    
       
            