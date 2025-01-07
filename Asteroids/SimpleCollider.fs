module Asteroids.SimpleCollider

type Collider = {
        pos: System.Numerics.Vector2
        velocity: System.Numerics.Vector2
        radius: float32
        rotation: float32
        rotationalVelocity: float32
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
        let newRot = collider.rotation + collider.rotationalVelocity*dt
        { collider with pos = newPos; rotation = newRot }
        
    let wrap_collider window (collider: Collider) =
        let width = float32(Graphics2D.Window.width window)
        let height = float32(Graphics2D.Window.height window)
        let newx = 
            if collider.pos.X > width then 0.0f
            else if collider.pos.X < 0.0f then width
            else collider.pos.X
        let newy =  
            if collider.pos.Y > height then 0.0f
            else if collider.pos.Y < 0.0f then height
            else collider.pos.Y
        
        { collider with pos = System.Numerics.Vector2(newx,newy) }
         