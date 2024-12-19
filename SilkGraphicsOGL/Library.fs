﻿namespace SilkGraphicsOGL
#nowarn "9"    


open System.Numerics
open System.Threading
open Graphics2D.Window
open ManagerRegistry
open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicFunctions
open Silk.NET.OpenGL
open Silk.NET.Windowing
open Silk.NET.Maths
open Graphics2D
open SilkGraphicsOGL.WindowGL



[<Manager("Silk Graphics", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkGraphicsManager() =
    let MessageCallback (source,cbtype:DebugType,id, severity, length,message,userParam ) =
        printfn "GL CALLBACK: %s type = %s, severity = 0x%x, message = %s\n" 
            (if cbtype = DebugType.DebugTypeError then "** GL ERROR **" else "")
            (cbtype.ToString()) severity message
    
    interface Graphics2D.IGraphicsManager with
        member this.CreateWindow width height title =
            let mutable options = WindowOptions.Default
            options.Title <- "Hello from F#"
            options.Size <- Vector2D(800, 600)
            let window = Silk.NET.Windowing.Window.Create(options)

            
            SilkWindow(window)
        member this.CloseWindow window =
            window :?> SilkWindow |> _.SilkWindow.Close()
            window :?> SilkWindow |> _.SilkWindow.DoEvents() // EMPTY THE MESSAGE QUEUE
            window :?> SilkWindow |> _.SilkWindow.Dispose()
            ()
          
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
            silkImage.CreateSubImage (int x) (int y) (int width) (int height)    
        member this.DrawImage matrix image tint  =
            let silkImage = image :?> SilkImage
            silkImage.Draw matrix tint
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
        
            
