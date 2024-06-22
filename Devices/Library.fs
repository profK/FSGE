//// JW: You should really use a qualified namespace like FSGE.Devices
namespace Devices

//// JW: Library.fs should be the last file in the project.
/// Unused open directive
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

//// JW: Consider documenting value values/ranges for the below
type DeviceValue =
    | ButtonValue of bool
    | AxisValue of float
    | NormalizedAxisValue of float
    //// JW: Why is this singular if it's an array?
    | KeyboardValue of uint32 array // ascii kb values

type DeviceNode = {
    Name: string
    Type: DeviceType
    //// JW: Parens not needed
    Children: (DeviceNode seq) option
    Path: string
}

type DeviceContext = interface end


type IDeviceManager =
    //// JW: Type members should be PascalCase not camelCase
    abstract member tryGetDeviceContext : Graphics2D.Window -> DeviceContext option
    abstract member PollDevices : DeviceContext -> unit
    abstract member tryGetDeviceValue : DeviceContext->string-> DeviceValue option
    abstract member GetDeviceTree : DeviceContext -> DeviceNode seq
    abstract member MapPlatformScanCodeToHID : uint32 -> uint32
    //// JW: I'd expect this to be named GetDeviceValueMap but I suppose as long as you're consistent about how plaurality is assigned :)
    abstract member GetDeviceValuesMap : DeviceContext -> Map<string,DeviceValue>

module Devices =
    //// JW: Make this private
    let _deviceManager =
        match ManagerRegistry.getManager<IDeviceManager>() with
        | Some manager -> manager
        | None -> failwith "No input manager found"
    
    //// JW: Module values/functions should be camelCase not PascalCase
    let TryGetDeviceContext window = _deviceManager.tryGetDeviceContext window    
    let GetDeviceTree context = _deviceManager.GetDeviceTree context
    let TryGetDeviceValue context path = _deviceManager.tryGetDeviceValue context path
    let GetDeviceValuesMap context = _deviceManager.GetDeviceValuesMap context
    let MapPlatformScanCodeToHID code = _deviceManager.MapPlatformScanCodeToHID code
    
  