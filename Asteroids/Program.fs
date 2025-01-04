// For more information see https://aka.ms/fsharp-console-apps

open System
open System.Numerics
open AngelCodeText
open Asteroids.Rocks
open Asteroids.Ship
open Asteroids.SimpleCollider
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
        Window.LoadImageFromPath $"images/rock{i}_result.png" window)
    asteroidsList <- [0..3] |> List.map (fun _ -> MakeRandomRock(rockImages.[0]))
    let shipImage = Window.LoadImageFromPath "images/ship_reg_result.png" window
    let mutable shipRec = {
        pos=Vector2(400.0f,300.0f)
        velocity=Vector2(0.0f,0.0f)
        rotation=0.0f
        rotVelocity=0.0f
        image=shipImage
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
            // update positions
            asteroidsList <-
                asteroidsList 
                |> List.map (fun rock ->
                    UpdateRockPosition (float32 deltaMS) rock )
                |> List.map (fun rock ->
                    WrapRock window rock )           
            shipRec<- GetInput deviceContext shipRec deltaMS 
                      |> UpdateShipPosition (float32 deltaMS)
                      |> WrapShip window 
                     
             //check forcollisions
            asteroidsList
            |> List.tryPick (fun rock ->
                if try_collide shipRec rock then
                    Some rock
                else None)
            |> function
               | Some _ -> printf "collided"
               | None -> ()
            //draw on screen
            asteroidsList |> List.iter (fun rock ->
                DrawRock window rockImages rock) |> ignore          
            Window.DrawImage shipImage (
                Window.CreateRotation(shipRec.rotation) *
                Window.CreateTranslation(Vector2(float32 shipRec.pos.X,float32 shipRec.pos.Y))) |> ignore
            Window.Display window |> ignore
            
    0