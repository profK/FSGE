module SilkDevices

open System
open System.Text.RegularExpressions
open Devices
open ManagerRegistry
open Silk.NET.Input
open SilkGraphicsOGL.WindowGL
open SwiftGraphicsAndInput.SilkDeviceStates

module Seq =
    let foldi func state seq =
        let result =
            seq
            |> Seq.fold (fun stateTuple mbr ->
                         (func (fst stateTuple) mbr (snd stateTuple)),
                            (snd stateTuple)+1
                         )
                         (state,0)
        fst result
        
type SilkDeviceContext(silkInputContext:IInputContext) =
    interface DeviceContext
    member val Context = silkInputContext with get      
 
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkInputManager() =
    let makeKBNode state kb i =
        let name = $"Keyboard{i}"
        let newkb =
            {Name=name; Type= Keyboard; Children=None; Path=name}
        newkb::state     
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
        Regex.Split(id, @"(?<=[a-zA-Z])(?=\d)")
     

            
    interface IDeviceManager with
        member this.tryGetDeviceContext window =
            let inputContext = getInputContext window
            Some (SilkDeviceContext(inputContext))
        member this.GetDeviceTree deviceContext  =
            let ctxt = (deviceContext :?> SilkDeviceContext).Context
            let kbNodelist =
                ctxt.Keyboards  
                |> Seq.foldi makeKBNode List.Empty
            let mouseList =
                ctxt.Mice
                |> Seq.foldi makeMouseNode List.Empty
            let controllerList =
                ctxt.Gamepads
                |> Seq.foldi makeControllerNode List.Empty
            let joystickList =
                ctxt.Joysticks
                |> Seq.foldi makeJoystickNode List.Empty    
            
            [kbNodelist;mouseList;controllerList]
            |>List.concat
            
     
        member this.tryGetDeviceValue deviceContext node =
            None