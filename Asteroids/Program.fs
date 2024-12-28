// For more information see https://aka.ms/fsharp-console-apps

open System
open System.Numerics
open AngelCodeText
open CSCoreAudio
open Graphics2D
open ManagerRegistry
open Silk.NET.Maths
open SilkDevices
open SilkGraphicsOGL

//record types
type RockSize =
    | small = 0
    | medium = 1
    | large = 2
type ShipRec = {
    pos: Vector2D<float>
    velocity: Vector2D<float>
    rotation: float 
}

type RockRec = {
    pos: Vector2D<float>
    velocity: Vector2D<float>
    rotation: float
    size: RockSize
    rotVelocity: float
}

let MAX_VELOCITY = 0.5
let MAX_ROT_VELOCITY = 0.005
let mutable asteroidsList =List<RockRec>.Empty
        
let random = System.Random()

let MakeRandomRock() =
    {
        size=RockSize.large;
        //pos=Vector2D(random.NextDouble() * 800.0, random.NextDouble() * 600.0)
        pos = Vector2D(400.0,300.0)
        rotation=random.NextDouble()*Math.PI*2.0
        velocity =Vector2D((random.NextDouble() * 2.0 - 1.0)*MAX_VELOCITY,
                           (random.NextDouble() * 2.0 - 1.0)*MAX_VELOCITY)
        rotVelocity=(random.NextDouble()*2.0-1.0)*MAX_ROT_VELOCITY
    }
let UpdateRockPosition ( elapsedMS: float, rock:RockRec) =
    //let newPos = rock.pos + Vector2D(
    //                                rock.velocity.X*(float elapsedMS),
    //                                rock.velocity.Y*(float elapsedMS))
                   
    let newRot = rock.rotation + (rock.rotVelocity*(float elapsedMS))
    {rock with  rotation=newRot}
    
    
let DrawRock (window:Window,images:Image list, rock:RockRec) =
    let rockImage = images.[(0)] //t)rock.size]
    let imageSize = rockImage.Size
    let matrix =
       //Window.CreateTranslation(
       //         Vector2(float32 rock.pos.X,float32 rock.pos.Y))*
        Window.CreateTranslation(
                Vector2(400f,300f))
                //Vector2(400.0f-(float32 imageSize.Width/2.0f),
                //        300.0f-(float32 imageSize.Height/2.0f))) *                                                                                  Window.CreateRotation(float32 rock.rotation)
        //Window.CreateRotation(float32 rock.rotation)
    Window.DrawImage rockImage matrix |> ignore
    

//execution starts here
[<EntryPoint>]
let main argv =
    asteroidsList <- [0] |> List.map (fun _ -> MakeRandomRock())
 
    // Loading the PLugins
    // We are doing it manually here, but in a real application
    // The plugins woul be autoloaded from a plugin directory and
    // registered automatically
    addManager(typeof<SilkGraphicsManager>)
    addManager(typeof<SilkDeviceManager>)
    addManager(typeof<xUnitLogger.xUnitLogger>)
    addManager(typeof<AngelCodeTextRenderer>)
    addManager(typeof<CSCorePlugin>)
    
    //preloading the images
    let window = Window.create 800 600 "Asteroids"
    let rockImages = [0..2] |> List.map(fun i ->
        Window.LoadImageFromPath $"images/rock{i}.png" window)
    
    let mutable running = true
    let mutable lastTime = DateTime.Now
    while running do
        let deltaTime = DateTime.Now - lastTime
        let deltaMS = deltaTime.TotalMilliseconds
        if deltaMS>100 then
            lastTime <- DateTime.Now
            Window.Clear {R=0uy;G=0uy;B=0uy;A=255uy} window |> ignore
            asteroidsList <-
                asteroidsList 
                |> List.map (fun rock ->
                    UpdateRockPosition (deltaMS, rock) )
            asteroidsList |> List.iter (fun rock -> DrawRock (window,rockImages,rock)) |> ignore
            Window.Display window |> ignore
            
    0