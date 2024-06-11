namespace Devices

open Graphics2D

type DeviceType =
    | Button 
    | Axis 
    | NormalizedAxis 
    | Keyboard
    | Mouse
    | Joystick
    | Gamepad
    | Touch
    | Accelerometer
    | Gyroscope
    | Magnetometer
    | Collection
type DeviceValue =
    | ButtonValue of bool
    | AxisValue of float
    | NormalizedAxisValue of float
    | KeyboardValue of uint32 array // ascii kb values

type DeviceNode = {
    Name: string
    Type: DeviceType
    Children: (DeviceNode list) option
}


type IDeviceManager =
    abstract member GetDeviceTree : Window -> DeviceNode list
    abstract member GetDeviceValue : Window->DeviceNode -> DeviceValue
    

module Devices =
    let _deviceManager =
        match ManagerRegistry.getManager<IDeviceManager>() with
        | Some manager -> manager
        | None -> failwith "No input manager found"
    let GetDeviceTree window = _deviceManager.GetDeviceTree window
    let GetDeviceValue window deviceNode  = _deviceManager.GetDeviceValue window deviceNode