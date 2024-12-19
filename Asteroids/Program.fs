// For more information see https://aka.ms/fsharp-console-apps

open AngelCodeText
open CSCoreAudio
open Graphics2D
open ManagerRegistry
open SilkDevices
open SilkGraphicsOGL

//execution starts here
[<EntryPoint>]
let main argv =
    // Loading the PLugins
    // We are doing it manually here, but in a real application
    // The plugins woul be autoloaded from a plugin directory and
    // registered automatically
    addManager(typeof<SilkGraphicsManager>)
    addManager(typeof<SilkDeviceManager>)
    addManager(typeof<xUnitLogger.xUnitLogger>)
    addManager(typeof<AngelCodeTextRenderer>)
    addManager(typeof<CSCorePlugin>)

    let window = Window.create 800 600 "Asteroids"
    let mutable running = true
    let mutable lastTime = 0L
    while running do
        ()
    0