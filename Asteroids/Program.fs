// For more information see https://aka.ms/fsharp-console-apps

open System
open System.Numerics
open AngelCodeText
open Asteroids.Rocks
open Asteroids.Ship
open CSCoreAudio
open Graphics2D
open Devices

open ManagerRegistry

//These are needed for the registeration of the plugins
// In a real application, these would be loaded from a plugin directory
// instead of being hardcoded

open SilkDevices
open SilkGraphicsOGL
open ConsoleLogger


//record types


let mutable asteroidsList =List<RockRec>.Empty
        
let random = System.Random()


    

//execution starts here
[<EntryPoint>]
let main argv =
   
    asteroidsList <- [0..5] |> List.map (fun _ -> MakeRandomRock())
 
    // Loading the PLugins
    // We are doing it manually here, but in a real application
    // The plugins woul be autoloaded from a plugin directory and
    // registered automatically
    addManager(typeof<SilkGraphicsManager>)
    addManager(typeof<SilkDeviceManager>)
    addManager(typeof<xUnitLogger.xUnitLogger>)
    addManager(typeof<AngelCodeTextRenderer>)
    addManager(typeof<CSCorePlugin>)
    addManager(typeof<ConsoleLogger>)
 
    //preloading the images
    let window = Window.create 800 600 "Asteroids"
    let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
    let rockImages = [0..2] |> List.map(fun i ->
        Window.LoadImageFromPath $"images/rock{i}.png" window)
    let shipImage = Window.LoadImageFromPath "images/ship_reg.png" window
    let mutable shipRec = {
        pos=Vector2(400.0f,300.0f)
        velocity=Vector2(0.0f,0.0f)
        rotation=0.0f
        rotVelocity=0.0f
    }
    let mutable running = true
    let mutable lastTime = DateTime.Now
    while running do
        Window.DoEvents window
        let deltaTime = DateTime.Now - lastTime
        let deltaMS = float32 deltaTime.TotalMilliseconds
        if deltaMS>100f then
            lastTime <- DateTime.Now
            Window.Clear {R=0uy;G=0uy;B=0uy;A=255uy} window |> ignore
            asteroidsList <-
                asteroidsList 
                |> List.map (fun rock ->
                    UpdateRockPosition (float32 deltaMS) rock )
                |> List.map (fun rock ->
                    WrapRock window rock )  
            asteroidsList |> List.iter (fun rock ->
                DrawRock window rockImages rock) |> ignore
            shipRec<- GetInput deviceContext shipRec deltaMS 
                      |> UpdateShipPosition (float32 deltaMS)
                      |> WrapShip window
                        
            Window.DrawImage shipImage (
                Window.CreateRotation(shipRec.rotation) *
                Window.CreateTranslation(Vector2(float32 shipRec.pos.X,float32 shipRec.pos.Y))) |> ignore
            Window.Display window |> ignore
            
    0