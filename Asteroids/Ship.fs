module Asteroids.Ship

open System
open System.Numerics
open Graphics2D
open Devices
open Input
open Input.HIDScanCodes

type ShipRec = {
    collider: SimpleCollider.Collider
    image: Image
}


let SHIP_INCR = 0.0005f
let SHIP_ROT_PPS = 0.05f


    

    
let IsKeyPressed context hidKey =
    Devices.TryGetDeviceValue context $"Keyboard0"
    |> Option.map (function
        | Devices.KeyboardValue keys -> Array.contains (uint32 hidKey) keys
        | _ -> false)
    |> Option.defaultValue false


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
    {ship with
        collider =
            {ship.collider with velocity=vel;rotation=rot}}
    