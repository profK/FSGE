module Asteroids.SimpleCollider

let try_collide (ship: Asteroids.Ship.ShipRec) (rock: Asteroids.Rocks.RockRec)  =
    let distanceSqd = (ship.pos - rock.pos).LengthSquared()
    let shipRadius = float32 (max ship.image.Size.Width ship.image.Size.Height) / 2.0f
    let rockRadius = float32 (max rock.image.Size.Width rock.image.Size.Height) / 2.0f
    let collisionDistanceSqd = (rockRadius + shipRadius)**2.0f
    distanceSqd < collisionDistanceSqd
