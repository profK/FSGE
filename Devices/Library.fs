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
    Path: string
}

type DeviceContext = interface end


type IDeviceManager =
    abstract member tryGetDeviceContext : Graphics2D.Window -> DeviceContext option
    abstract member tryGetDeviceValue : DeviceContext->string-> DeviceValue option
    abstract member GetDeviceTree : DeviceContext -> DeviceNode list
    abstract member MapPlatformScanCodeToHID : uint32 -> uint32    

module Devices =
    let _deviceManager =
        match ManagerRegistry.getManager<IDeviceManager>() with
        | Some manager -> manager
        | None -> failwith "No input manager found"
    let TryGetDeviceContext window = _deviceManager.tryGetDeviceContext window    
    let GetDeviceTree context = _deviceManager.GetDeviceTree context
    let TryGetDeviceValue context path = _deviceManager.tryGetDeviceValue context path
    let MapPlatformScanCodeToHID code = _deviceManager.MapPlatformScanCodeToHID code
    
  