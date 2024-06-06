namespace SilkGraphicsAndInput
#nowarn "9"    

open System
open System.Drawing
open System.Runtime.InteropServices.Marshalling
open ManagerRegistry
open Microsoft.FSharp.NativeInterop
open Silk.NET.GLFW
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.Input                       
open Silk.NET.OpenGL
open Silk.NET.GLFW
open Graphics2D



// Because Silk input is closely tied to Swift graphics, we need to define their plugins in the same project
type SilkWindow(silkWindow:IWindow) =
    // What comes below is ugly because OpenGL is a big stateful mess and
    // things need to be done in the right order
    let defaultVertexShaderCode =
        @" #version 330 core
        layout (location = 0) in vec3 aPosition;
        void main()
        {
            gl_Position = vec4(aPosition, 1.0);
        }"
    let defaultFragmentShaderCode =
        @"#version 330 core
        out vec4 out_color;
        void main()
        {
            out_color = vec4(1.0, 0.5, 0.2, 1.0);
        }"
    do
        silkWindow.Initialize()
    let _gl = silkWindow.CreateOpenGL()
    let _defaultVertexShader = _gl.CreateShader(ShaderType.VertexShader)
    let _defaultFragmentShader = _gl.CreateShader(ShaderType.FragmentShader)
    do
        _gl.ShaderSource(_defaultVertexShader, defaultVertexShaderCode)
        _gl.ShaderSource(_defaultFragmentShader, defaultFragmentShaderCode);
    interface Window
    
    member val GL = _gl with get
    member val DefaultVertexShader = _defaultVertexShader with get
    member val DefaultFragmentShader = _defaultFragmentShader with get
    member this.SilkWindow = silkWindow
    member this.SetBackgroundColor color =
        _gl.ClearColor(color)
    member this.Clear() =
        _gl.Clear(ClearBufferMask.ColorBufferBit)
     member this.Display() =
        let ctxt = silkWindow.GLContext
        ctxt.SwapBuffers()


type SilkImage(silkWindow:SilkWindow) =
   
   
    let _vao = silkWindow.GL.GenVertexArray()
    let _vbo = silkWindow.GL.GenBuffer()
  

    let vertices =
        [|
            0.5f;  0.5f; 0.0f;
            0.5f; -0.5f; 0.0f;
            -0.5f; -0.5f; 0.0f; // notice we have to indent to make the - line up with the block
            -0.5f
        |]
    let indices =
        [|
            0u; 1u; 3u;
            1u; 2u; 3u
        |]
    do
        silkWindow.GL.BindVertexArray(_vao)
        silkWindow.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo)
        
      
        
   
      
 
    member this.Draw() =
        do //set vertex buffer
            use ptr  = fixed vertices
            let ptr' = NativeInterop.NativePtr.toVoidPtr ptr
            silkWindow.GL.BufferData(BufferTargetARB.ArrayBuffer, unativeint(vertices.Length * sizeof<float>),
                                     ptr', BufferUsageARB.StaticDraw)
        // when we leave the block vertices becomes unpinned
        do // set indices buffer
            use ptr = fixed indices
            let ptr' = NativeInterop.NativePtr.toVoidPtr ptr
            silkWindow.GL.BufferData(BufferTargetARB.ElementArrayBuffer, unativeint (indices.Length * sizeof<uint>),
                                     ptr', BufferUsageARB.StaticDraw)
        // unfix the indices
            
    interface Image 
    
    
    
    
    

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
            SilkImage (window :?> SilkWindow)
        member this.DrawImage image matrix window = failwith "Not implemented"
        member this.Translate point = failwith "Not implemented"
        member this.Rotate angle = failwith "Not implemented"
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
            
[<Manager("Silk Input", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]
type SilkInputManager() =
    interface Input.IInputManager with
        member this.GetDeviceTree () = failwith "Not implemented"
        member this.GetDeviceValue path = failwith "Not implemented"