namespace SilkGraphicsOGL
#nowarn "9"    


open System.Numerics
open ManagerRegistry
open Silk.NET.Windowing
open Silk.NET.Maths
open Graphics2D
open SilkGraphicsOGL.WindowGL

//// JW: Consider something like the following:
module Helpers =
    let inline silkWindow (f : SilkWindow -> 'a) (window : Window) : 'a = f (window :?> SilkWindow)
    
    let inline silkWindowPassthrough (f : SilkWindow -> unit) (window : Window) : Window = f (window :?> SilkWindow) ; window
    
    let inline silkWindowW (f : IWindow -> 'a) (window : Window) : 'a = f (window :?> SilkWindow).SilkWindow
    
    let inline silkWindowWPassthrough (f : IWindow -> unit) (window : Window) : Window = f (window :?> SilkWindow).SilkWindow ; window
     
     
//// JW: Needed because we're in a namespace for some reason
open Helpers


[<Manager("Silk Graphics", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkGraphicsManagerJW() =

    interface Graphics2D.IGraphicsManager with
        member this.CreateWindow width height title =
            let mutable options = WindowOptions.Default
            options.Title <- "Hello from F#"
            options.Size <- Vector2D(800, 600)
            let window = Silk.NET.Windowing.Window.Create(options)
            SilkWindow(window)
        member this.CloseWindow window = window |> silkWindowW (_.Close())
        member this.WindowWidth window = window |> silkWindowW _.Size.X
        member this.WindowHeight window = window |> silkWindowW _.Size.Y
        member this.WindowTitle window = window |> silkWindowW _.Title
        member this.SetWindowTitle window title = window |> silkWindowWPassthrough (fun w -> w.Title <- title)
        member this.WindowPosition window = window |> silkWindowW (fun w -> Point(w.Position.X, w.Position.Y))
        member this.SetWindowPosition window position = window |> silkWindowWPassthrough (fun w -> w.Position <- Vector2D(position.X, position.Y))
        member this.WindowSize window = window |> silkWindowW (fun w -> Size(w.Size.X, w.Size.Y))
        member this.SetWindowSize window size = window |> silkWindowWPassthrough (fun w -> w.Size <- Vector2D(size.Width, size.Height))
        member this.LoadImage path window = window |> silkWindow (fun w -> SilkImage(path, w))
        member this.DrawImage (matrix:Matrix4x4) (image:Image)  =
            let silkImage = image :?> SilkImage
            silkImage.Draw matrix
            silkImage.Window
        member this.Clear color window =
            window :?> SilkWindow
            |> fun w ->
                w.SetBackgroundColor color
                w.Clear()
            window
        member this.Display window = window |> silkWindowPassthrough (_.Display())
        member this.DoEvents window = window |> silkWindowW (_.DoEvents())
            

//// End JW Suggestions










[<Manager("Silk Graphics", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkGraphicsManager() =   

    interface Graphics2D.IGraphicsManager with
        member this.CreateWindow width height title =
            let mutable options = WindowOptions.Default
            options.Title <- "Hello from F#"
            options.Size <- Vector2D(800, 600)
            let window = Silk.NET.Windowing.Window.Create(options)
            SilkWindow(window)
        member this.CloseWindow window =
            window :?> SilkWindow |> _.SilkWindow.Close()
        member this.WindowWidth window =
            window :?> SilkWindow |> _.SilkWindow.Size.X
        member this.WindowHeight window =
             window :?> SilkWindow |> _.SilkWindow.Size.Y
        member this.WindowTitle window =
            window :?> SilkWindow |> _.SilkWindow.Title
        member this.SetWindowTitle window title =
            window :?> SilkWindow |> fun sw -> sw.SilkWindow.Title <- title
            window
        member this.WindowPosition window =
            window :?> SilkWindow
            |> fun sw ->
                let spos =sw.SilkWindow.Position
                {X=spos.X; Y=spos.Y}
        member this.SetWindowPosition window position =
            window :?> SilkWindow
            |> fun sw ->
                sw.SilkWindow.Position <- Vector2D(position.X,position.Y)
                window
                
        member this.WindowSize window =
            window :?> SilkWindow
            |> fun sw ->
                let v2d = sw.SilkWindow.Size
                {Width=v2d.X;Height=v2d.Y}
        member this.SetWindowSize window size =
            window :?> SilkWindow
            |> fun sw ->
                sw.SilkWindow.Size <- Vector2D(size.Width,size.Height)
                window
        member this.LoadImageFromStream stream window =
            let silkWindow = (window :?> SilkWindow)
            SilkImage(stream, silkWindow)
        member this.LoadImageFromPath path window =
            let silkWindow = (window :?> SilkWindow)
            SilkImage(path, silkWindow)    
        member this.CreateSubImage image x y width height =
            let silkImage = (image :?> SilkImage)
            silkImage.CreateSubImage x y width height    
        member this.DrawImage (matrix:Matrix4x4) (image:Image)  =
            let silkImage = image :?> SilkImage
            silkImage.Draw matrix
            silkImage.Window
        member this.Clear color window =
            window :?> SilkWindow
            |> fun w ->
                w.SetBackgroundColor color
                w.Clear()
            window
        member this.Display window = 
            window :?> SilkWindow
            |> fun w -> w.Display()
            window
        member this.DoEvents window =
            let silkWindow = (window :?> SilkWindow)
            silkWindow.SilkWindow.DoEvents()
        
            
