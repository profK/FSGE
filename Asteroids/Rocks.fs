module Asteroids.Rocks

open System
open System.Numerics
open Graphics2D



type RockRec = {
    collider: SimpleCollider.Collider
    rotation: float32
    rotVelocity: float32
    image: Image
    size: Size
}


let random = System.Random()

let MakeRandomRock  image =
    {
        image=image
        size=image.Size
        collider = {pos=Vector2(random.NextSingle()* 800.0f,
                                     random.NextSingle() * 600.0f)
                    velocity=Vector2(
                        random.NextSingle()*2.0f-1.0f,
                        random.NextSingle()*2.0f-1.0f )
                    radius = float32 (max image.Size.Width image.Size.Height)/2.0f
        }
        rotation = (random.NextSingle()*2.0f-1.0f) * 2.0f * float32(Math.PI)
        rotVelocity = (random.NextSingle()*2.0f-1.0f) 
    }

let DrawRock window (images:Image list) rock =
    let rockImage = images.[(0)] //t)rock.size]
    let imageSize = rockImage.Size
    let matrix =
        Window.CreateRotation(float32 rock.rotation) *
        Window.CreateTranslation(Vector2( rock.collider.pos.X, rock.collider.pos.Y))
    Window.DrawImage rockImage matrix |> ignore