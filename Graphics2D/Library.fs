namespace Graphics2D

open System.Drawing
open System.Numerics



// This is type that represents a window on the screen
// It is an opaque type, meaning that you can't see the implementation details
// It is sub-classed by plugin objects that are used to draw on the window
type Window = interface end

// This is a type that represents an image
// It is an opaque type, meaning that you can't see the implementation details

type Image = interface end

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
    abstract member LoadImage : string-> Window -> Image
    abstract member DrawImage : Matrix4x4->Image-> Window
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

    // This is a function that gets the background color of a window
    // It takes a window object as an argument
    // It returns the background color of the window
   
    let LoadImage path window : Image = 
        _graphicsManager.LoadImage path window
    let DrawImage image  Matrix4x4 window : Window = 
        _graphicsManager.DrawImage Matrix4x4 image 
    let Clear color window : Window =
        _graphicsManager.Clear color window
    let Display window : Window =
        _graphicsManager.Display window
    let CreateRotation radians = Matrix4x4.CreateRotationZ(radians)
    let CreateTranslation (vector:Vector2) =
        Matrix4x4.CreateTranslation(vector.X,vector.Y,0.0f)
    let DoEvents window = _graphicsManager.DoEvents window    
        
        
    