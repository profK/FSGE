﻿// For more information see https://aka.ms/fsharp-console-apps
module Asteroids.Main

open System
open System.IO
open System.Numerics
open AngelCodeText
open Asteroids.GraphicsExtensions
open Asteroids.Rocks
open Asteroids.Ship
open Asteroids.SimpleCollider
open FSGEAudio
open Devices
open Graphics2D
open ManagerRegistry
open CSCoreAudio

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
    addManager(typeof<AngelCodeTextRenderer>)
    addManager(typeof<ConsoleLogger>)
    addManager(typeof<CSCorePlugin>)

 
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
        collider = {pos=Vector2(400.0f,300.0f);velocity=Vector2(0.0f,0.0f)
                    radius=float32 (max shipImage.Size.Height  shipImage.Size.Width)/2.0f
                   }
        rotation=0.0f
        rotVelocity=0.0f
        image=shipImage
    }
    let explosionImage = Window.LoadImageFromPath "images/explosion.png" window
    let mutable explosionAnim = AnimatedImage.create explosionImage 128 128 10 100.0
    // load audio
    let audioStream = new FileStream("audio/explosion.wav", FileMode.Open, FileAccess.Read)
        // buffer sfx in memory
    let memoryStream = new MemoryStream()
    audioStream.CopyTo(memoryStream)
    memoryStream.Position <- 0L // Reset the position to the beginning of the stream
    audioStream.Close()
    let sound = Audio.OpenSoundStream memoryStream AudioFileFormat.WAV
    Audio.SetVolume 1.0f sound
    //Audio.SetOutputDevice 0
  
    let mutable running = true
    let mutable lastTime = DateTime.Now
    let mutable showShip = true
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
                    {rock with collider =
                                SimpleCollider.wrap_collider window
                                    (SimpleCollider.update deltaMS rock.collider)})
                   
            shipRec <- GetInput deviceContext shipRec deltaMS 
                       |> fun ship ->
                              {ship with collider =
                                            SimpleCollider.wrap_collider window
                                                (SimpleCollider.update deltaMS ship.collider)}
             //check forcollisions
            if showShip then
                asteroidsList
                |> List.tryPick (fun rock ->
                    match SimpleCollider.try_collide shipRec.collider rock.collider with
                        | Some _ -> Some rock
                        | None -> None)
                |> function
                   | Some _ ->
                       Audio.Play sound |> ignore
                       explosionAnim <- AnimatedImage.start explosionAnim
                       showShip <- false      
                   | None -> ()
            //draw on screen
            asteroidsList |> List.iter (fun rock ->
                Window.DrawImage rock.image (
                    Window.CreateRotation(rock.rotation) *
                    Window.CreateTranslation(Vector2(float32 rock.collider.pos.X,float32 rock.collider.pos.Y))) |> ignore)
            if showShip then
                Window.DrawImage shipImage (
                    Window.CreateRotation(shipRec.rotation) *
                    Window.CreateTranslation(Vector2(float32 shipRec.collider.pos.X,float32 shipRec.collider.pos.Y))) |> ignore
            else
               if explosionAnim.IsPlaying then          
                   explosionAnim <- AnimatedImage.update (float deltaMS) explosionAnim
                   AnimatedImage.draw (Window.CreateTranslation shipRec.collider.pos) explosionAnim |> ignore
               else
                   ()
            Window.Display window |> ignore
            
    0