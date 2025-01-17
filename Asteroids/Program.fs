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
    // The FSGE audio API plays audio from streams
    // In order to preload the audio, we need to copy the stream from a file stream
    // to a memory stream.
    //Preloaded audio reduces the latency of starting the sound
    let memoryStream = new MemoryStream()
    audioStream.CopyTo(memoryStream)
    memoryStream.Position <- 0L // Reset the position to the beginning of the stream
    audioStream.Close()
    let sound = Audio.OpenSoundStream memoryStream AudioFileFormat.WAV
    Audio.SetVolume 1.0f sound

  
    // These are mutables that track the state of the game
    let mutable running = true
    let mutable lastTime = DateTime.Now
    let mutable showShip = true
    
    // Usually recursion makes more sense in F# then while loops
    // When it comes to long running loops like this one,however, recursion
    // can cause stack overflows. In this case, it is better to use
    // a while loop
    while running do
        // This call to DoEvents is necessary to update controller states
        // and keep the window responsive
        Window.DoEvents window
        
        // Find the time in MS siece last frame
        let deltaTime = DateTime.Now - lastTime
        let deltaMS = float32 deltaTime.TotalMilliseconds
      
        // When the time since the last frame is greater than 100ms
        // we update the game state and draw the screen
        match deltaMS with
        | d when d > 100.0f ->
            // this sets last time to now so we can measure the time
            // until the next frame
            lastTime <- DateTime.Now
            // clear the screen
            Window.Clear {R=0uy;G=0uy;B=0uy;A=255uy} window |> ignore
            // update  of asteroids
            // as position is held in the Collider record e need to create
            // a new asteroid record for each asteroid with its collider
            // updated to the new position
            asteroidsList <-
                asteroidsList 
                |> List.map (fun rock ->
                    {rock with collider =
                                SimpleCollider.wrap_collider window
                                    (SimpleCollider.update (deltaMS*ROCK_PPS) rock.collider)})
            // Update of the player's ship position
            // Ship only participates in the game if showShip is true
            // This prevents the ship from moving or shooting when it is
            // not onscreen     
            shipRec <- match showShip with
                        | true ->
                           // GetInput is a function in Ship.fs that reads the input
                           // and updates the ship appropriately
                           // as bullets are held in the ship record we need to
                           // update them as well. This happens in the GetInput function
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
                        | false -> shipRec
       
             //check for ship collision with asteroids
            // again, collision only happens if showShip is true
            match showShip with
            | true ->
                asteroidsList
                |> List.tryPick (fun rock ->
                    match SimpleCollider.try_collide shipRec.collider rock.collider with
                        | Some _ -> Some rock
                        | None -> None)
                |> function
                   | Some _ ->
                       Audio.Rewind sound
                       |> Audio.Play |> ignore
                       explosionAnim <- AnimatedImage.start explosionAnim
                       shipRec <-{shipRec with bullets = []} // clear bullets
                       showShip <- false      
                   | None -> ()
            | false -> ()
            
            //check for bullet collision with asteroids
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
            
            //draw the asteroids on screen
            asteroidsList |> List.iter (fun rock ->
                Window.DrawImage rock.image (
                    Window.CreateRotation(rock.collider.rotation) *
                    Window.CreateTranslation(Vector2(float32 rock.collider.pos.X,float32 rock.collider.pos.Y))) |> ignore)
            // if the ship is on screen, draw it 
            // otherwise draw the ship explosion animation
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
                   
            // draw the bullets       
            shipRec.bullets |> List.iter (fun bullet -> 
                Window.DrawImage bulletImage (
                    Window.CreateTranslation(Vector2(float32 bullet.Collider.pos.X,float32 bullet.Collider.pos.Y))) |> ignore)
            Window.Display window |> ignore
        | _ -> ()
    0