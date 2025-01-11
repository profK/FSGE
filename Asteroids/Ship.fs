module Asteroids.Ship

open System
open System.Numerics
open Graphics2D
open Devices
open Input
open Input.HIDScanCodes
open Asteroids.InputExtensions
open Asteroids.Bullets

type ShipRec = {
    collider: SimpleCollider.Collider
    image: Image
    bulletImage: Image
    bullets: Bullets.Bullet list
}


let SHIP_INCR = 0.0005f
let SHIP_ROT_PPS = 1f

let ROCK_PPS = 0.1f
let ROCK_ROT_PPS =  0.0005f

let GetInput context (ship:ShipRec) (elapsedMS:float32)=
    let rot =
        if IsKeyPressed context ScanCode.RightArrow then
            ship.collider.rotation+(SHIP_ROT_PPS * elapsedMS)
        elif IsKeyPressed context ScanCode.LeftArrow then
           ship.collider.rotation-(SHIP_ROT_PPS*elapsedMS)
        else ship.collider.rotation
    let vel =
        if IsKeyPressed context ScanCode.UpArrow then
            let angle = float ship.collider.rotation
            let x = (float32 (Math.Sin angle)) * elapsedMS * SHIP_INCR
            let y = -float32 (Math.Cos(angle))*elapsedMS*SHIP_INCR
            Vector2(ship.collider.velocity.X+x, ship.collider.velocity.Y+y)
        else ship.collider.velocity 
    let bulletList = 
            if IsKeyPressed context ScanCode.Space then
                let angle = float ship.collider.rotation
                let x = (float32 (Math.Sin angle))  * ROCK_PPS
                let y = -float32 (Math.Cos(angle))  * ROCK_PPS
                let bulletVel = Vector2(ship.collider.velocity.X+x, ship.collider.velocity.Y+y)
                let bulletPos = ship.collider.pos + bulletVel
                let bullet = Bullets.createBullet ship.bulletImage bulletPos bulletVel (DateTime.Now.AddSeconds(2.0))
                bullet::ship.bullets
            else
                ship.bullets
    {ship with
        collider =  {ship.collider with velocity=vel; rotation = rot}
        bullets = bulletList
    }