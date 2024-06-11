﻿namespace SilkGraphicsOGL
#nowarn "9"    

open System.Drawing
open System.Numerics
open ManagerRegistry
open Silk.NET.Windowing
open Silk.NET.Maths
open Graphics2D
open SilkGraphicsOGL.WindowGL


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
                Point(spos.X,spos.Y)
        member this.SetWindowPosition window position =
            window :?> SilkWindow
            |> fun sw ->
                sw.SilkWindow.Position <- Vector2D(position.X,position.Y)
                window
                
        member this.WindowSize window =
            window :?> SilkWindow
            |> fun sw ->
                let v2d = sw.SilkWindow.Size
                Size(v2d.X,v2d.Y)
        member this.SetWindowSize window size =
            window :?> SilkWindow
            |> fun sw ->
                sw.SilkWindow.Size <- Vector2D(size.Width,size.Height)
                window
        member this.LoadImage path window =
            let silkWindow = (window :?> SilkWindow)
            SilkImage(path, silkWindow) 
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
            