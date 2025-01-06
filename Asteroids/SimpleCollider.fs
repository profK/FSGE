module Asteroids.SimpleCollider

type Collider = {
        pos: System.Numerics.Vector2
        velocity: System.Numerics.Vector2
        radius: float32
    }

module SimpleCollider =
    let try_collide (obj1: Collider) (obj2: Collider)  =
        let distanceSqd = (obj1.pos - obj2.pos).LengthSquared()
        let collisionDistanceSqd = (obj1.radius + obj2.radius)**2.0f
        if distanceSqd < collisionDistanceSqd then
            Some (obj1,obj2)
        else
            None
    let update (dt: float32) (collider: Collider) =
        let newPos = collider.pos + collider.velocity*dt
        { collider with pos = newPos }
        
    let wrap_collider window (collider: Collider) =
        let width = float32(Graphics2D.Window.width window)
        let height = float32(Graphics2D.Window.height window)
        if collider.pos.X > width then {collider with pos= System.Numerics.Vector2(0.0f,collider.pos.Y)}  
        elif collider.pos.X < 0.0f then
            { collider with pos=System.Numerics.Vector2(width,collider.pos.Y)}
        elif collider.pos.Y > height then
            {collider with pos=System.Numerics.Vector2(collider.pos.X, Y=0.0f)}
        elif collider.pos.Y < 0.0f then
            {collider with pos=System.Numerics.Vector2(collider.pos.X, Y=height)}
        else collider    