module DeviceValueCollector

open System.Collections.Concurrent
open System.Collections.Generic
open RawInputLight
open Devices

type DeviceValueCollector()=
       let DeviceValueDictionary:Dictionary<string,DeviceValue> =
           Dictionary<string,DeviceValue>()
     
          
                    
       member this.SetAnalogAxis (name:string, value:float):DeviceValue =
           lock DeviceValueDictionary (fun () ->
               if (DeviceValueDictionary.ContainsKey(name)) then
                   DeviceValueDictionary[name] <-
                       AxisValue value
           )
           DeviceValueDictionary[name]
           
       member this.SetDigitalAxis(name:string, value:bool):DeviceValue =
           lock DeviceValueDictionary (fun () ->
               if (DeviceValueDictionary.ContainsKey(name)) then
                   DeviceValueDictionary[name] <-
                       ButtonValue value
                   |> ignore    
                else
                    DeviceValueDictionary.Add(
                       name, ButtonValue value )
           )
           DeviceValueDictionary[name]
       member this.SetKeyboardAxis(name:string,key:char, keystate:KeyState):DeviceValue =
         lock DeviceValueDictionary (fun () ->
           if (DeviceValueDictionary.ContainsKey(name)) then
               let downKeys = DeviceValueDictionary[name]
               match downKeys with
                   | KeyboardValue keys -> 
                       DeviceValueDictionary[name] <-
                           match keystate with
                           | KeyState.KeyDown ->
                                   KeyboardValue  (
                                       Array.append
                                           keys 
                                           [|uint32 key|]
                                   )
                           | KeyState.KeyUp ->
                                   KeyboardValue (
                                       Array.except [|uint32 key|] keys)
                           | _ -> DeviceValueDictionary[name]
                   | _ -> ()
               |> ignore     
           else
                DeviceValueDictionary.Add(
                   name, KeyboardValue(
                       match keystate with
                       | KeyState.KeyDown ->
                               [|uint32 key|]
                       | KeyState.KeyUp ->
                               [||] // this really shouldnt happen
                   )
                )
         )      
         DeviceValueDictionary[name]
       member this.GetState():Map<string,DeviceValue> = 
           lock DeviceValueDictionary (fun () ->
               DeviceValueDictionary
               |> Seq.fold (fun m kvp ->
                    m.Add(kvp.Key,kvp.Value)
                   ) Map.empty
           )
               