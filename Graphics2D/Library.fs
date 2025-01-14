﻿namespace Graphics2D

open System.IO
open System.Numerics

// These are defined in here because they are used in the API
// There are equivalents in the System.Drawing namesapce
// but those have a checkered history of being available cross-platform

type Color = { R: byte; G: byte; B: byte; A: byte }
type Point = { X: int32; Y: int32 }
type Size = { Width: int32; Height: int32 }

// This is type that represents a window on the screen
// It is an opaque type, meaning that you can't see the implementation details
// It is sub-classed by plugin objects that are used to draw on the window
type Window = interface end

// This is a type that represents an image
// It is an opaque type, meaning that you can't see the implementation details

type Image =
    interface
        abstract member Size : Size    
    end

// THis interface defines the functionality a graphics plugin implementation must implement
type IGraphicsManager =
    abstract member CreateWindow : int -> int -> string -> Window
    abstract member CloseWindow : Window -> unit
    abstract member WindowWidth : Window -> int
    abstract member WindowHeight : Window -> int
    abstract member WindowTitle : Window -> string
    abstract member SetWindowTitle : Window -> string -> Window
    abstract member WindowPosition : Window -> Point
    abstract member SetWindowPosition : Window -> Point -> Window
    abstract member WindowSize : Window -> Size
    abstract member SetWindowSize : Window -> Size -> Window
    abstract member LoadImageFromStream : Stream-> Window -> Image
    abstract member LoadImageFromPath : string -> Window -> Image
    abstract member CreateSubImage : Image->uint32->uint32->uint32->uint32 -> Image
    abstract member DrawImage : Matrix4x4->Image-> Color-> Window
    abstract member Clear : Color ->  Window -> Window
    abstract member Display : Window -> Window
    abstract member DoEvents : Window -> unit

module Window =
    //This fetches the plugin graphics manager plugin
    //All plugins must be loaded before this module is used
    //or it may throw an exception
    let _graphicsManager =
        match ManagerRegistry.getManager<IGraphicsManager>() with
        | Some manager -> manager
        | None -> failwith "No graphics manager found"
        
    // This is a function that creates a new window
    // It takes a width, a height and the window's name as arguments
    // It returns a new window object
    let create width height title : Window =
        _graphicsManager.CreateWindow width height title

    // This is a function that closes a window
    // It takes a window object as an argument
    // It returns unit
    let close window =
        _graphicsManager.CloseWindow window
       

    // This is a function that gets the width of a window
    // It takes a window object as an argument
    // It returns the width of the window
    let width window =
        _graphicsManager.WindowWidth window

    // This is a function that gets the height of a window
    // It takes a window object as an argument
    // It returns the height of the window
    let height window =
        _graphicsManager.WindowHeight window
        

    // This is a function that gets the title of a window
    // It takes a window object as an argument
    // It returns the title of the window
    let title window = 
        _graphicsManager.WindowTitle window

    // This is a function that sets the title of a window
    // It takes a window object and a string as arguments
    // It returns the set window object
    let setTitle window title =
        _graphicsManager.SetWindowTitle window title
        

    // This is a function that gets the position of a window
    // It takes a window object as an argument
    // It returns the position of the window
    let position window =
        _graphicsManager.WindowPosition window

    // This is a function that sets the position of a window
    // It takes a window object and a position as arguments
    // It returns the set window object
    let setPosition window position =
        _graphicsManager.SetWindowPosition window position


    // This is a function that gets the size of a window
    // It takes a window object as an argument
    // It returns the size of the window
    let size window = 
        _graphicsManager.WindowSize window
    

    // This is a function that sets the size of a window
    // It takes a window object and a size as arguments
    // It returns unit
    let setSize window size = 
        _graphicsManager.SetWindowSize window size


    // This is a function that loads an image
    // It takes a path to the image and a window object as arguments
    // It returns an image object
    let LoadImageFromPath (path:string) window : Image = 
        _graphicsManager.LoadImageFromPath path window
    
    let LoadImageFromStream (stream:Stream) window : Image = 
        _graphicsManager.LoadImageFromStream stream window
    
    // This is a function that draws an image on a window
    // It takes a matrix, an image and a window object as arguments
    // It returns the window object to support railroad style chaining
    let CreateSubImage image x y width height  =
        _graphicsManager.CreateSubImage image x y width height
        
    // This is a function that draws an image on a window
    // It takes a matrix, an image and a window object as arguments
    // It returns the window object to support railroad style chaining
        
    let DrawTintedImage image  Matrix4x4 tint = 
        _graphicsManager.DrawImage Matrix4x4 image tint
        
    let DrawImage image  Matrix4x4  = 
        _graphicsManager.DrawImage Matrix4x4 image {R=255uy;G=255uy;B=255uy;A=255uy}
    // This is a function that clears a window
    // It takes a color and a window object as arguments
    // It returns the window object to support railroad style chaining    
    let Clear color window : Window =
        _graphicsManager.Clear color window
    // This is a function that displays the next frame on a window
    // It takes a window object as an argument
    // It returns the window object to support railroad style chaining
    let Display window : Window =
        _graphicsManager.Display window
    // This is a function that processes window events
    // It takes a window object as an argument
    // It returns unit
    let DoEvents window = _graphicsManager.DoEvents window
    
    //this translates a 2D translation vector to a 4x4 3D matrix for compositing
    let CreateTranslation (v:Vector2) = Matrix4x4.CreateTranslation(Vector3(v.X,v.Y,0.0f))
    //this translates a 2D rotation angle to a 4x4 3D matrix for compositing
    let CreateRotation angle = Matrix4x4.CreateRotationZ(angle)
        
        
    