module Asteroids.Ship

open System
open System.Numerics
open Graphics2D
open Devices
open Input
open Input.HIDScanCodes

type ShipRec = {
    collider: SimpleCollider.Collider
    rotation: float32
    rotVelocity: float32
    image: Image
}


let SHIP_INCR = 20.0f
let SHIP_ROT_PPS = 1.0f


    

    
let IsKeyPressed context hidKey =
    Devices.TryGetDeviceValue context $"Keyboard0"
    |> Option.map (function
        | Devices.KeyboardValue keys -> Array.contains (uint32 hidKey) keys
        | _ -> false)
    |> Option.defaultValue false


let GetInput context (ship:ShipRec) elapsedMS=
    let rot =
        if IsKeyPressed context ScanCode.RightArrow then
            SHIP_ROT_PPS/1000f
        elif IsKeyPressed context ScanCode.LeftArrow then
           -SHIP_ROT_PPS/1000f
        else 0.0f
    let vel =
        if IsKeyPressed context ScanCode.UpArrow then
            let angle = ship.rotation
            let x = float32(Math.Sin(float angle))*(elapsedMS*SHIP_INCR/1000f)
            let y = -float32(Math.Cos(float angle))*(elapsedMS*SHIP_INCR/1000f)
            Vector2(ship.collider.velocity.X+x, ship.collider.velocity.Y+y)
        else ship.collider.velocity           
    {ship with
        rotVelocity = rot
        collider=  {ship.collider with velocity=vel}
    }