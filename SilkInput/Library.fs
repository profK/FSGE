namespace SilkDevices

open Devices
open ManagerRegistry
open Silk.NET.Input
open SilkDevices
open SilkGraphicsOGL.WindowGL
open SilkScanCodeConversion


            
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]            
type SilkDeviceManager() =
    let getInputContext (window:Graphics2D.Window) =
            //could be wasteful, measure and buffer if necc
            (window :?> SilkWindow).SilkWindow.CreateInput()
    interface Devices.IDeviceManager with
        member this.tryGetDeviceContext window =
            let inputContext = getInputContext window
            let silkDeviceContext = SilkDeviceContext(inputContext)
            silkDeviceContext.Devices <- SilkDevices.scanDevices silkDeviceContext
            silkDeviceContext.Values <- Map.empty
            Some(silkDeviceContext)
            
        member this.GetDeviceTree deviceContext  =
            (deviceContext :?> SilkDeviceContext).Devices
            
        member this.tryGetDeviceValue deviceContext path =
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            Map.tryFind path silkCtxt.Values

        member this.MapPlatformScanCodeToHID var0 =
            mapSilkToHID var0
            
        member this.GetDeviceValuesMap deviceContext  =    
            let silkCtxt = (deviceContext :?> SilkDeviceContext)
            silkCtxt.Values