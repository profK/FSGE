module SilkDevices

open System
open Devices
open ManagerRegistry
open Silk.NET.Input
open SilkGraphicsOGL.WindowGL

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
        
   
        
    let  makeButtonNodeList mouseButtons =
        mouseButtons
        |> Seq.foldi (fun state mouseButton i ->
                        {Name = $"button{i}"
                         Type = DeviceType.Button
                         Children = None}::state) List.Empty
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
            |> makeButtonNodeList
        let scrollWheels =
            mouse.ScrollWheels
            |> makeScrollWheelNodeList
        let children = [mouseButtons;scrollWheels] |>List.concat
        {Name=name;Type=Collection;Children=Some children}::state
   
   
    let getInputContext (window:Graphics2D.Window) =
            //could be wasteful, measure and buffer if necc
            (window :?> SilkWindow).SilkWindow.CreateInput()
    interface Devices.IDeviceManager with
        member this.GetDeviceTree window  =
            let ctxt = getInputContext window
            let kbNodelist =
                ctxt.Keyboards  
                |> Seq.foldi makeKBNode List.Empty
            let mouseList =
                ctxt.Mice
                |> Seq.foldi makeMouseNode List.Empty
            
            [kbNodelist;mouseList]
            |>List.concat
            
        member this.GetDeviceValue window path = failwith "Not implemented"