module Asteroids.Bullets

open System
open Graphics2D
open Asteroids.InputExtensions

type Bullet = {
        Collider: SimpleCollider.Collider
        TimeToDie: DateTime
        Image:Image
    }
module Bullets= 
    let createBullet (image:Image) position velocity timeToDie = 
        let rad = float32 (max image.Size.Width image.Size.Height)/2.0f
        {
            Collider= {
                        pos=position
                        velocity=velocity
                        radius = rad
                        rotation = 0f
                        rotationalVelocity = 0F
            }
            TimeToDie = timeToDie
            Image=image
        }
    
    
    
    
