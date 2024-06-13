module SwiftGraphicsAndInput.SilkDeviceStates

open Devices
open Silk.NET.Input



// Path: SilkGraphicsOGL/SilkDeviceStates.fs
module DeviceValueCache =
    let MakeCache () =[]
    let AddValue cache name value = (name,value)::cache   