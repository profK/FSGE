module Asteroids.Rocks

open System
open System.Numerics
open Graphics2D



type RockRec = {
    pos: Vector2
    velocity: Vector2
    rotation: float32
    rotVelocity: float32
    image: Image
}

let ROCK_PPS = 20.0f
let ROCK_ROT_PPS = 0.0005f
let random = System.Random()

let MakeRandomRock image =
    {
        image=image
        pos=Vector2(random.NextSingle()* 800.0f,
                    random.NextSingle() * 600.0f)
        velocity=Vector2(
            random.NextSingle()*2.0f-1.0f * ROCK_PPS/2.0f,
            random.NextSingle()*2.0f-1.0f * ROCK_PPS/2.0f)
        rotation = (random.NextSingle()*2.0f-1.0f) * 2.0f * float32(Math.PI)
        rotVelocity = (random.NextSingle()*2.0f-1.0f) * ROCK_ROT_PPS
    }
let UpdateRockPosition elapsedMS rock:RockRec =
    let newPos = rock.pos +
                 Vector2(rock.velocity.X*float32(elapsedMS*ROCK_PPS/1000.0f),
                         rock.velocity.Y*float32(elapsedMS*ROCK_PPS/1000.0f))
                   
    let newRot = rock.rotation + (rock.rotVelocity*elapsedMS)
    {rock with  rotation=newRot ; pos=newPos}
    
let WrapRock window rock =
    if rock.pos.X > 800.0f then {rock with pos= Vector2(0.0f,rock.pos.Y)}  
    elif rock.pos.X < 0.0f then
        { rock with pos=Vector2(float32(Window.width window),rock.pos.Y)}
    elif rock.pos.Y > 600.0f then
        {rock with pos=Vector2(rock.pos.X, Y=0.0f)}
    elif rock.pos.Y < 0.0f then
        {rock with pos=Vector2(rock.pos.X, Y=600.0f)}
    else rock
let DrawRock window (images:Image list) rock =
    let rockImage = images.[(0)] //t)rock.size]
    let imageSize = rockImage.Size
    let matrix =
        Window.CreateRotation(float32 rock.rotation) *
        Window.CreateTranslation(Vector2( rock.pos.X, rock.pos.Y))
    Window.DrawImage rockImage matrix |> ignore