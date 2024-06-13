module SilkScanCodeConversion

open Input
open Silk.NET.Input



let scanCodeMapping =
    [
        (Key.A, HIDScanCodes.ScanCode.A)
        (Key.B, HIDScanCodes.ScanCode.B)
        (Key.C, HIDScanCodes.ScanCode.C)
        (Key.D, HIDScanCodes.ScanCode.D)
        (Key.E, HIDScanCodes.ScanCode.E)
        (Key.F, HIDScanCodes.ScanCode.F)
        (Key.G, HIDScanCodes.ScanCode.G)
        (Key.H, HIDScanCodes.ScanCode.H)
        (Key.I, HIDScanCodes.ScanCode.I)
        (Key.J, HIDScanCodes.ScanCode.J)
        (Key.K, HIDScanCodes.ScanCode.K)
        (Key.L, HIDScanCodes.ScanCode.L)
        (Key.M, HIDScanCodes.ScanCode.M)
        (Key.N, HIDScanCodes.ScanCode.N)
        (Key.O, HIDScanCodes.ScanCode.O)
        (Key.P, HIDScanCodes.ScanCode.P)
        (Key.Q, HIDScanCodes.ScanCode.Q)
        (Key.R, HIDScanCodes.ScanCode.R)
        (Key.S, HIDScanCodes.ScanCode.S)
        (Key.T, HIDScanCodes.ScanCode.T)
        (Key.U, HIDScanCodes.ScanCode.U)
        (Key.V, HIDScanCodes.ScanCode.V)
        (Key.W, HIDScanCodes.ScanCode.W)
        (Key.X, HIDScanCodes.ScanCode.X)
        (Key.Y, HIDScanCodes.ScanCode.Y)
        (Key.Z, HIDScanCodes.ScanCode.Z)
        (Key.Number1, HIDScanCodes.ScanCode.Digit1)
        (Key.Number2, HIDScanCodes.ScanCode.Digit2)
        (Key.Number3, HIDScanCodes.ScanCode.Digit3)
        (Key.Number4, HIDScanCodes.ScanCode.Digit4)
        (Key.Number5, HIDScanCodes.ScanCode.Digit5)
        (Key.Number6, HIDScanCodes.ScanCode.Digit6)
        (Key.Number7, HIDScanCodes.ScanCode.Digit7)
        (Key.Number8, HIDScanCodes.ScanCode.Digit8)
        (Key.Number9, HIDScanCodes.ScanCode.Digit9)
        (Key.Number0, HIDScanCodes.ScanCode.Digit0)
        (Key.Enter, HIDScanCodes.ScanCode.Enter)
        (Key.Escape, HIDScanCodes.ScanCode.Escape)
        (Key.Backspace, HIDScanCodes.ScanCode.Backspace)
        (Key.Tab, HIDScanCodes.ScanCode.Tab)
        (Key.Space, HIDScanCodes.ScanCode.Space)
        (Key.F1, HIDScanCodes.ScanCode.F1)
        (Key.F2, HIDScanCodes.ScanCode.F2)
        (Key.F3, HIDScanCodes.ScanCode.F3)
        (Key.F4, HIDScanCodes.ScanCode.F4)
        (Key.F5, HIDScanCodes.ScanCode.F5)
        (Key.F6, HIDScanCodes.ScanCode.F6)
        (Key.F7, HIDScanCodes.ScanCode.F7)
        (Key.F8, HIDScanCodes.ScanCode.F8)
        (Key.F9, HIDScanCodes.ScanCode.F9)
        (Key.F10, HIDScanCodes.ScanCode.F10)
        (Key.F11, HIDScanCodes.ScanCode.F11)
        (Key.F12, HIDScanCodes.ScanCode.F12)
        // Add more mappings as needed
    ] |> Map.ofList

let intToSilkScanCode (value: int) : Key option =
    if System.Enum.IsDefined(typeof<Key>, value) then
        Some (enum<Key> value)
    else
        None
let mapSilkToHID (openGLScanCode: uint32) : uint32 =
    match intToSilkScanCode (int openGLScanCode) with
    |Some oglScanCode -> 
        match scanCodeMapping.TryFind oglScanCode with
        | Some hidScanCode -> uint32 hidScanCode
        | None -> 0u
    | None -> 0u
   