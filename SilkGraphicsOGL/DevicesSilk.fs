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
 
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkInputManager() =
    let makeKBNode state kb i =
        let name = $"Keyboard{i}"
        let newkb =
            {Name=name; Type= Keyboard; Children=None}
        newkb::state     
    let makeMouseButtonNodeList (mouseButtons:MouseButton seq) =
        mouseButtons
        |> Seq.fold (fun state mouseButton  ->
                        {Name = mouseButton.ToString()
                         Type = DeviceType.Button
                         Children = None}::state) List.Empty
    let makeButtonIndexNodeList (buttons:Button seq) =
        buttons
        |> Seq.fold (fun state button  ->
                        {Name = $"Button{button.Index}"
                         Type = DeviceType.Button
                         Children = None}::state) List.Empty
    let makeButtonNameNodeList (buttons:Button seq) =
        buttons
        |> Seq.fold (fun state button ->
                         {
                             Name=button.Name.ToString()
                             Type=DeviceType.Button
                             Children = None
                         }::state) List.Empty    
    let makeScrollWheelNodeList scrollWheels =
        scrollWheels
        |> Seq.foldi (fun state mouseButton i ->
                        {Name = $"scrollwheel{i}"
                         Type = DeviceType.Collection
                         Children = Some [
                             {Name = "x"; Type = DeviceType.Axis; Children = None }
                             {Name = "y"; Type = DeviceType.Axis; Children = None }
                         ]}::state) List.Empty
        
    let makeMouseNode state (mouse:IMouse) i =
        let name = $"Mouse{i}"
        let mouseButtons =
            mouse.SupportedButtons
            |> makeMouseButtonNodeList
        let scrollWheels =
            mouse.ScrollWheels
            |> makeScrollWheelNodeList
        let position = [{Name="position";Type=Collection;Children=
                             Some [
                                {Name="x";Type=DeviceType.Axis;Children=None}
                                {Name="y";Type=DeviceType.Axis;Children=None}
                             ]}]  
        let children = [mouseButtons;scrollWheels;position] |>List.concat
        {Name=name;Type=Collection;Children=Some children}::state
        
    let makeThumbstickNodeList (thumbSticks:Thumbstick seq)=
        thumbSticks
        |> Seq.foldi (fun state thumbstick i ->
                        {Name = $"thumbStick{thumbstick.Index}"
                         Type = DeviceType.Collection
                         Children = Some [
                             {Name = "x"; Type = DeviceType.Axis; Children = None }
                             {Name = "y"; Type = DeviceType.Axis; Children = None }
                         ]}::state) List.Empty
   
    let makeTriggerNodeList (triggers:Trigger seq) =
        triggers
        |> Seq.fold (fun state (trigger:Trigger)  ->
                        {Name = $"trigger{trigger.Index}"
                         Type = DeviceType.Button
                         Children = None}::state) List.Empty    
            
    let makeControllerNode state (ctlr:IGamepad) i =
        let name = $"Gamepad{i}"
        let ctlrButtons =
            ctlr.Buttons
            |> makeButtonNameNodeList
        let ctlrThumbsticks =
            ctlr.Thumbsticks
            |> makeThumbstickNodeList
        let ctlrTriggers = 
            ctlr.Triggers
            |> makeTriggerNodeList    
      
        let children = [ctlrButtons;ctlrThumbsticks] |>List.concat
        {Name=name;Type=Collection;Children=Some children}::state    
    let makeJoystickNode state (joystick:IJoystick) i =
        let name = $"Joystick{i}"
        let buttons =
            joystick.Buttons
            |> makeButtonIndexNodeList
        let axes =
            joystick.Axes
            |> Seq.foldi (fun state axis i ->
                            {Name = $"axis{i}"
                             Type = DeviceType.Axis
                             Children = None}::state) List.Empty
        let children = [buttons;axes] |> List.concat
        {Name=name;Type=Collection;Children=Some children}::state
   
    let getInputContext (window:Graphics2D.Window) =
            //could be wasteful, measure and buffer if necc
            (window :?> SilkWindow).SilkWindow.CreateInput()
            
    let splitOrdinal id =
        Regex.Split(id, @"(?<=[a-zA-Z])(?=\d)")
     
    member val private _Context = None with get,set
    member val private _KBStates = None with get,set
    
    member this.Context window  = 
        match this._Context with
        | Some ctxt -> ctxt
        | None -> 
            let ctxt = getInputContext window
            this._Context <- Some ctxt
            this._KBStates <- Some (DeviceStates.GetKBStates ctxt)
            ctxt
            
    interface Devices.IDeviceManager with
        member this.GetDeviceTree window  =
            let ctxt = this.Context window
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
            
     
        member this.GetDeviceValue window node =
            match node.Type with
            | Keyboard ->
                let s = splitOrdinal node.Name
                let ctxt = this.Context window
                let kb = ctxt.Keyboards.[int s[1]]
                this._KBStates.Value.GetKeyStates kb
                |> Set.toArray
                |> KeyboardValue
                
               
            
            
            
            