namespace Input

type DeviceValue =
    | Button of bool
    | Axis of float
    | NormalizedAxis of float
    | Keyboard of char array

type DeviceNode = {
    Name: string
    Children: (DeviceNode list) option
}

type IInputManager =
    abstract member GetDeviceTree : unit -> DeviceNode
    abstract member GetDeviceValue : string -> DeviceValue
    

module Input =
    let _inputManager =
        match ManagerRegistry.getManager<IInputManager>() with
        | Some manager -> manager
        | None -> failwith "No input manager found"
    let GetDeviceTree () = _inputManager.GetDeviceTree()
    let GetDeviceValue path = _inputManager.GetDeviceValue path