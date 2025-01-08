module Asteroids.InputExtensions
open Devices
open Input.HIDScanCodes

let IsKeyPressed context (hidKey:ScanCode) =
    Devices.TryGetDeviceValue context $"Keyboard0"
    |> Option.map (function
        | Devices.KeyboardValue keys -> Array.contains (uint32 hidKey) keys
        | _ -> false)
    |> Option.defaultValue false