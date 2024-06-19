namespace SilkDevices

open Devices
open ManagerRegistry
open Silk.NET.Input
open Silk.NET.Windowing
open SilkDevices
open SilkGraphicsOGL.WindowGL
open SilkScanCodeConversion


            
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]            
type SilkDeviceManager() =
    interface Devices.IDeviceManager with
        member this.tryGetDeviceContext (window:Graphics2D.Window) =
            let silkWindow = (window :?> SilkWindow).SilkWindow
            let silkDeviceContext = SilkDeviceContext(silkWindow)
            Some(silkDeviceContext)
        member this.PollDevices context =
            (context :?> SilkDeviceContext).PollEvents()
            
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