module Asteroids.Ship

open System
open System.Numerics
open Graphics2D
open Devices
open Input
open Input.HIDScanCodes

type ShipRec = {
    pos: Vector2
    velocity: Vector2
    rotation: float32
    rotVelocity: float32
}


let SHIP_INCR = 20.0f
let SHIP_ROT_PPS = 1.0f

let UpdateShipPosition elapsedMS (ship:ShipRec) =
    let newPos = ship.pos + Vector2(
                                   ship.velocity.X*elapsedMS*SHIP_INCR/1000.0f,
                                   ship.velocity.Y*elapsedMS*SHIP_INCR/1000.0f)
    let newRot = ship.rotation + (ship.rotVelocity*elapsedMS)
    {ship with  rotation=newRot ; pos=newPos}
    
let WrapShip window (ship:ShipRec) =
    if ship.pos.X > 800.0f then {ship with pos= Vector2(0.0f,ship.pos.Y)}  
    elif ship.pos.X < 0.0f then
        { ship with pos=Vector2(float32(Window.width window),ship.pos.Y)}
    elif ship.pos.Y > 600.0f then
        {ship with pos=Vector2(ship.pos.X, Y=0.0f)}
    elif ship.pos.Y < 0.0f then
        {ship with pos=Vector2(ship.pos.X, Y=600.0f)}
    else ship
    
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
            Vector2(ship.velocity.X+x, ship.velocity.Y+y)
        else ship.velocity           
    {ship with velocity=vel; rotVelocity=rot}