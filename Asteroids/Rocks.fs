module Asteroids.Rocks

open System
open System.Numerics
open Asteroids.Ship
open Graphics2D

let ROCK_PPS = 0.1f
let ROCK_ROT_PPS = 0.1f

type RockRec = {
    collider: SimpleCollider.Collider
    image: Image
    size: Size
}


let random = System.Random()
let rec GetNonConflictingPosition pos radius size =
    let newPos = Vector2(random.NextSingle()* 800.0f,
                         random.NextSingle() * 600.0f)
    if Vector2.Distance(pos, newPos) < radius + (float32 (max size.Width size.Height)/2.0f) then
        GetNonConflictingPosition pos radius size
    else newPos

let MakeRandomRock  image (shipRec: ShipRec) =
    
    {
        image=image
        size=image.Size
        
        collider = {pos=GetNonConflictingPosition shipRec.collider.pos (shipRec.collider.radius*3f) image.Size
                    velocity=Vector2(
                        random.NextSingle()*2.0f-1.0f,
                        random.NextSingle()*2.0f-1.0f )
                    radius = float32 (max image.Size.Width image.Size.Height)/2.0f
                    rotation = 0.0f
                    rotationalVelocity = (random.NextSingle()*2.0f-1.0f) * ROCK_ROT_PPS
        }
       
    }
    
let MakeSubRocks (image:Image) (parent:RockRec) =
    [
        //{image=image
        // size=image.Size
         //collider={parent.collider with
         //           pos=parent.collider.pos+image.Size*Vector2(-0.5f,-0.5f)
         //           velocity=parent.collider.velocity*Vector2(-1f,-1.0f)}
        //}
        {image=image
         size=image.Size
         collider={parent.collider with
                    pos=parent.collider.pos+
                        Vector2(float32 image.Size.Width, float32 image.Size.Height)*Vector2(1f,-1f)
                    velocity=parent.collider.velocity*Vector2(1f,-1.0f)}
         }
        {image=image
         size=image.Size
         collider={parent.collider with
                    pos=parent.collider.pos+
                        Vector2(float32 image.Size.Width, float32 image.Size.Height)*Vector2(-1f,1f)
                    velocity=parent.collider.velocity*Vector2(-1f,1f)}
         }
    ]

let DrawRock window (images:Image list) rock =
    let rockImage = images.[(0)] //t)rock.size]
    let imageSize = rockImage.Size
    let matrix =
        Window.CreateRotation(float32 rock.collider.rotation) *
        Window.CreateTranslation(Vector2( rock.collider.pos.X, rock.collider.pos.Y))
    Window.DrawImage rockImage matrix |> ignore