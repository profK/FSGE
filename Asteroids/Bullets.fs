module Asteroids.Bullets

open System
module Bullets=
    type Bullet = {
        Position: System.Numerics.Vector2
        Velocity: System.Numerics.Vector2
        TimeToDie: DateTime
    }

    let createBullet position velocity timeToDie = {
        Position = position
        Velocity = velocity
        TimeToDie = timeToDie
    }
    
    
