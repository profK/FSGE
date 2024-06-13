namespace SilkGraphicsSDL

open System
open System.IO
open Graphics2D
open Silk.NET.SDL

type SDLWindow(sdlWindow,sdlRenderer) =
    interface Graphics2D.Window
    member val Window = sdlWindow with get
    member val Renderer = sdlRenderer with get

type SilkGraphicsSDL() =
    let _sdl = SdlProvider.SDL
    interface IGraphicsManager with
       
        member this.CreateWindow width height title =
            let window = _sdl.Value.CreateWindow(title, 100, 100, width, height, uint32 WindowFlags.Shown)
            let renderer = _sdl.Value.CreateRenderer(window, -1, uint32 RendererFlags.Accelerated)
            SDLWindow(window, renderer)
           
        member this.Clear color window =
            let sdlRenderer =(window:?>SDLWindow).Renderer
            _sdl.Value.SetRenderDrawColor(sdlRenderer, color.R, color.G, color.B, color.A)
            _sdl.Value.RenderClear(sdlRenderer)
            window              
        member this.CloseWindow(window) =
            let sdlWindow =(window:?>SDLWindow).Window
            _sdl.Value.DestroyWindow(sdlWindow)
            

        member this.Display(var0) = failwith "todo"
        member this.DrawImage var0 var1 = failwith "todo"
        member this.LoadImage path window = failwith "todo"
        member this.SetWindowPosition var0 var1 = failwith "todo"
        member this.SetWindowSize var0 var1 = failwith "todo"
        member this.SetWindowTitle var0 var1 = failwith "todo"
        member this.WindowHeight(var0) = failwith "todo"
        member this.WindowPosition(var0) = failwith "todo"
        member this.WindowSize(var0) = failwith "todo"
        member this.WindowTitle(var0) = failwith "todo"
        member this.WindowWidth(var0) = failwith "todo"
    
       