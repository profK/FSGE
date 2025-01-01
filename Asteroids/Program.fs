// For more information see https://aka.ms/fsharp-console-apps

open System
open System.Numerics
open AngelCodeText
open Asteroids.Rocks
open CSCoreAudio
open Graphics2D
open Devices

open ManagerRegistry
open Silk.NET.Input
open SilkDevices
open SilkGraphicsOGL
open ConsoleLogger


//record types

type ShipRec = {
    pos: Vector2
    velocity: Vector2
    rotation: float32
    rotVelocity: float32
}


let SHIP_INCR = 20.0f
let SHIP_ROT_PPS = 1.0f
let mutable asteroidsList =List<RockRec>.Empty
        
let random = System.Random()


    
let UpdateShipPosition elapsedMS (ship:ShipRec) =
    let newPos = ship.pos + Vector2(
                                   ship.velocity.X*elapsedMS*SHIP_INCR/1000.0f,
                                   ship.velocity.Y*elapsedMS*SHIP_INCR/1000.0f)
    let newRot = ship.rotation + (ship.rotVelocity*elapsedMS)
    {ship with  rotation=newRot ; pos=newPos}
    
let WrapShip window (ship:ShipRec) =
    if ship.pos.X > 800.0f then {ship with pos= Vector2(0.0f,ship.pos.Y)}  
    elif ship.pos.X < 0.0f then
        { ship with pos=Vector2(float32(Window.width window),ship.pos.Y)}
    elif ship.pos.Y > 600.0f then
        {ship with pos=Vector2(ship.pos.X, Y=0.0f)}
    elif ship.pos.Y < 0.0f then
        {ship with pos=Vector2(ship.pos.X, Y=600.0f)}
    else ship
    
let IsKeyPressed context hidKey =
    Devices.TryGetDeviceValue context $"Keyboard0"
    |> Option.map (function
        | Devices.KeyboardValue keys -> Array.contains (uint32 hidKey) keys
        | _ -> false)
    |> Option.defaultValue false


let GetInput context (ship:ShipRec) elapsedMS=
    let rot =
        if IsKeyPressed context Key.Right then
            SHIP_ROT_PPS/1000f
        elif IsKeyPressed context Key.Left then
           -SHIP_ROT_PPS/1000f
        else 0.0f
    let vel =
        if IsKeyPressed context Key.Up then
            let angle = ship.rotation
            let x = float32(Math.Sin(float angle))*(elapsedMS*SHIP_INCR/1000f)
            let y = -float32(Math.Cos(float angle))*(elapsedMS*SHIP_INCR/1000f)
            Vector2(ship.velocity.X+x, ship.velocity.Y+y)
        else ship.velocity           
    {ship with velocity=vel; rotVelocity=rot}
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