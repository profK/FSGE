module SwiftGraphicsAndInput.SilkDeviceStates

open Silk.NET.Input



// Path: SilkGraphicsOGL/SilkDeviceStates.fs

type KBStates(ctxt:IInputContext) =
    let mutable keyboardStates = dict[]
    let keyPressed (keyboard:IKeyboard) (key:Key) i =
        keyboardStates.Add(keyboard,
            match keyboardStates.TryGetValue(keyboard) with
            | true, set -> Set.add (uint32 key) set
            | false, _ -> Set.empty.Add (uint32 key)
        )    
    let keyReleased keyboard key i =
         keyboardStates.Add(keyboard,
            match keyboardStates.TryGetValue(keyboard) with
            | true, set -> Set.remove (uint32 key) set
            | false, _ ->
                failwith "Logic error: should never get a key release event for a key that was never pressed")
        
    do
        ctxt.Keyboards
        |> Seq.iter (fun (kb:IKeyboard) ->
                       kb.add_KeyDown keyPressed
                       kb.add_KeyUp keyReleased)  

    
    member _.GetKeyStates keyboard =
        match keyboardStates.TryGetValue keyboard with
        | true, set -> set
        | false, _ -> Set.empty

module DeviceStates =
    let GetKBStates ctxt = KBStates ctxt


   