module AxisStateCollector

open System.Collections.Concurrent
open System.Collections.Generic
open RawInputLight
open TDE3ManagerInterfaces.InputDevices

type AxisStateCollector()=
       let axisStateDictionary:Dictionary<string,AxisState> =
           Dictionary<string,AxisState>()
       member this.DeltaAnalogAxis (name:string, value:float):AxisState =
           lock axisStateDictionary (fun () ->
               if (axisStateDictionary.ContainsKey(name)) then
                   let (DeltaState currentValue) = axisStateDictionary[name]
                   axisStateDictionary[name] <-
                       DeltaState(value+currentValue) 
                else
                    axisStateDictionary.Add(
                       name, DeltaState(value))
           )
           axisStateDictionary[name]
          
                    
       member this.SetAnalogAxis (name:string, value:float):AxisState =
           lock axisStateDictionary (fun () ->
               if (axisStateDictionary.ContainsKey(name)) then
                   axisStateDictionary[name] <-
                       AnalogState(value) 
                else
                    axisStateDictionary.Add(
                       name, AnalogState(value))
           )
           axisStateDictionary[name]
           
       member this.SetDigitalAxis(name:string, value:bool):AxisState =
           lock axisStateDictionary (fun () ->
               if (axisStateDictionary.ContainsKey(name)) then
                   axisStateDictionary[name] <-
                       DigitalState value
                   |> ignore    
                else
                    axisStateDictionary.Add(
                       name, DigitalState value )
           )
           axisStateDictionary[name]
       member this.SetKeyboardAxis(name:string,key:char, keystate:KeyState):AxisState =
         lock axisStateDictionary (fun () ->
           if (axisStateDictionary.ContainsKey(name)) then
               let (KeyboardState downKeys) = axisStateDictionary[name]
               axisStateDictionary[name] <-
                   match keystate with
                   | KeyState.KeyDown ->
                           KeyboardState (key::downKeys)
                   | KeyState.KeyUp ->
                           KeyboardState(
                               downKeys
                               |> List.except [key])
               |> ignore     
            else
                axisStateDictionary.Add(
                   name, KeyboardState(
                       match keystate with
                       | KeyState.KeyDown ->
                               [key]
                       | KeyState.KeyUp ->
                               [] // this really shouldnt happen
                   )
                )
         )      
         axisStateDictionary[name]
       member this.GetState():Map<string,AxisState> = 
           lock axisStateDictionary (fun () ->
               axisStateDictionary
               |> Seq.fold (fun m kvp ->
                    m.Add(kvp.Key,kvp.Value)
                   ) Map.empty
           )
               