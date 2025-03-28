﻿namespace SilkDevices

open System
open Devices
open ManagerRegistry
open SilkDevices
open SilkGraphicsOGL.WindowGL
open SilkScanCodeConversion
            
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]            
type SilkDeviceManager() =
    interface Devices.IDeviceManager with
        member this.tryGetDeviceContext (window:Graphics2D.Window) =
            if (typeof<SilkWindow>.IsAssignableFrom(window.GetType())) then
                let silkWindow = (window :?> SilkWindow).SilkWindow
                let silkDeviceContext = SilkDeviceContext(silkWindow)
                Some(silkDeviceContext)
            else None    
        member this.PollDevices context =
            (context :?> SilkDeviceContext).PollEvents()
            
        member this.GetDeviceTree deviceContext  =
            (deviceContext :?> SilkDeviceContext).Devices
            
        member this.tryGetDeviceValue deviceContext path =
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            Map.tryFind path silkCtxt.Values

        member this.MapPlatformScanCodeToHID var0 =
            mapSilkToHID var0
            
        member this.MapHIDToPlatformScanCode (var0:uint32) =
            mapHIDToSilk var0
        member this.GetDeviceValuesMap deviceContext  =    
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            silkCtxt.Values
        member this.CloseDeviceContext deviceContext  =    
            let silkCtxt = (deviceContext :?> IDisposable)
            silkCtxt.Dispose()
            ()