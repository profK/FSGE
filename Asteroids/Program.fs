// For more information see https://aka.ms/fsharp-console-apps
module Asteroids.Main

open System
open System.DirectoryServices.ActiveDirectory
open System.IO
open System.Numerics
open System.Reflection
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



let SHIP_INCR = 20.0f
let SHIP_ROT_PPS = 0.5f


let mutable asteroidsList =List<RockRec>.Empty
        
let random = System.Random()


    

//execution starts here
[<EntryPoint>]
let main argv =
   
    // Loading the PLugins
    // This is a recursive function that loads all the plugins in the
    //specified directory and any sub directories
    
    let rec recursiveAssemblyLoad (path:string) =
        Directory.GetFiles(path, "*.dll")
        |> Array.iter (fun file -> 
            try
                Assembly.LoadFrom(file)
                |> ignore
            with
            | _ -> ())
        Directory.GetDirectories(path)
        |> Array.iter (fun dir -> recursiveAssemblyLoad dir)
    
    //We need the logger before the other  plugins so we
    //manually add it to the registry
    addManager typedefof<ConsoleLogger>
    
    //Register the plugins with the manager registry
    // This loads all assemblies found in the current directory
    //or any subdirectories
    recursiveAssemblyLoad "."
    
    // Plugins all have the attribute "Manager"
    // This searches the loaded assmblies for classes with the
    // Manager attribute and adds them to the manager registry
    AppDomain.CurrentDomain.GetAssemblies()
    |> Array.iter (fun a -> 
        a.GetTypes()
        |> Array.filter (fun t -> t.GetCustomAttributes(typeof<Manager>, true).Length > 0)
        |> Array.iter (fun t ->
            Console.WriteLine ("processing "+t.Name)
            t.GetCustomAttributes(typeof<Manager>, true).[0] :?> Manager
            |> function
               | attr when attr.SupportedSystems.HasFlag supportedSystems.Windows ->
                   ManagerRegistry.addManager(t)
               | _ -> ()
        )
    )
   
    //Game setup starts here
    let window = Window.create 800 600 "Asteroids"
    
    // Device contexets are Window system specific so
    // this gets a DeviceContext for the opened window
    let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
            
    //  This section loads all the images the game will need.
    // There are three sizes of rocks named rock0_result, rock1_result,
    // and rock2_result
    let shipImage = Window.LoadImageFromPath "images/ship_reg_result.png" window
    let bulletImage = Window.LoadImageFromPath "images/bullet.png" window
    let explosionImage = Window.LoadImageFromPath "images/explosion.png" window
    let bulletImage = Window.LoadImageFromPath "images/bullet.png" window
    let rockImages = [0..2] |> List.map(fun i ->
        Window.LoadImageFromPath $"images/rock{i}_result.png" window)
    
    // This creates the ship record that holds all the state of the
    // spaceship.
    // Note that shipRec is mutable so that it can be updated
    let mutable shipRec = {
        collider = { pos=Vector2(400.0f,300.0f);velocity=Vector2(0.0f,0.0f)
                     radius=float32 (max shipImage.Size.Height  shipImage.Size.Width)/2.0f
                     rotation=0.0f
                     rotationalVelocity = 0f
                   }
        
        image=shipImage
        bulletImage=bulletImage
        bullets = [] 
    }
 
    // This creates the initial list of asteroids
    // see Rock.fs for the definition of RockRec0
    //This list is also mutable so that it can be updated
    let mutable asteroidsList =
        [0..3] |> List.map (fun _ -> MakeRandomRock rockImages.[0] shipRec)
    
    //This creates a record that holds the state of the explosion animation
    //It is mutable so that it can be updated 
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
      
        match deltaMS with
        | d when d > 100.0f -> 
            lastTime <- DateTime.Now
            Window.Clear {R=0uy;G=0uy;B=0uy;A=255uy} window |> ignore
            // update positions
            asteroidsList <-
                asteroidsList 
                |> List.map (fun rock ->
                    {rock with collider =
                                SimpleCollider.wrap_collider window
                                    (SimpleCollider.update (deltaMS*ROCK_PPS) rock.collider)})
                   
            shipRec <- if showShip then
                           GetInput deviceContext shipRec deltaMS 
                           |> fun ship ->
                                  {ship with collider =
                                                SimpleCollider.wrap_collider window
                                                    (SimpleCollider.update deltaMS ship.collider)
                                             bullets =
                                                ship.bullets
                                                |> List.map (fun bullet ->
                                                    {bullet with
                                                        Collider = SimpleCollider.wrap_collider window
                                                                  (SimpleCollider.update deltaMS bullet.Collider)})
                                                |> List.filter (fun bullet -> bullet.TimeToDie > DateTime.Now)
                                                
                                  }
                        else
                            shipRec          
             //check forcollisions
            match showShip with
            | true ->
                asteroidsList
                |> List.tryPick (fun rock ->
                    match SimpleCollider.try_collide shipRec.collider rock.collider with
                        | Some _ -> Some rock
                        | None -> None)
                //None // for debugging
                |> function
                   | Some _ ->
                       Audio.Rewind sound
                       |> Audio.Play |> ignore
                       explosionAnim <- AnimatedImage.start explosionAnim
                       shipRec <-{shipRec with bullets = []}
                       showShip <- false      
                   | None -> ()
            | false -> ()
            shipRec.bullets
            |> List.iter (fun bullet ->
                asteroidsList
                |> List.tryPick (fun rock ->
                    match SimpleCollider.try_collide bullet.Collider rock.collider with
                        | Some _ -> Some rock
                        | None -> None)
                |> function
                   | Some rock ->
                       Audio.Rewind sound
                       |> Audio.Play  |> ignore
                       match rock.image with
                       | image when image = rockImages.[0] ->
                           let rockDir   = rock.collider.velocity
                           let newRocks = MakeSubRocks rockImages.[1] rock
                           asteroidsList <- newRocks @ asteroidsList
                       | _ -> ()    
                       asteroidsList <- List.filter (fun r -> r <> rock) asteroidsList
                   | None -> ())
            //draw on screen
            asteroidsList |> List.iter (fun rock ->
                Window.DrawImage rock.image (
                    Window.CreateRotation(rock.collider.rotation) *
                    Window.CreateTranslation(Vector2(float32 rock.collider.pos.X,float32 rock.collider.pos.Y))) |> ignore)
            match showShip with
            | true ->
                Window.DrawImage shipImage (
                    Window.CreateRotation(shipRec.collider.rotation) *
                    Window.CreateTranslation(Vector2(float32 shipRec.collider.pos.X,float32 shipRec.collider.pos.Y))) |> ignore
            | false ->
               match explosionAnim.IsPlaying with
               | true ->
                   explosionAnim <- AnimatedImage.update (float deltaMS) explosionAnim
                   AnimatedImage.draw (Window.CreateTranslation shipRec.collider.pos) explosionAnim |> ignore
               | false -> 
                   ()
            shipRec.bullets |> List.iter (fun bullet -> 
                Window.DrawImage bulletImage (
                    Window.CreateTranslation(Vector2(float32 bullet.Collider.pos.X,float32 bullet.Collider.pos.Y))) |> ignore)
            Window.Display window |> ignore
        | _ -> ()
    0