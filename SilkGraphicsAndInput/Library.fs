namespace SilkGraphicsAndInput
#nowarn "9"    

open System
open System.Drawing
open System.IO
open System.Numerics
open System.Runtime.InteropServices.Marshalling
open ManagerRegistry
open Microsoft.FSharp.NativeInterop
open Silk.NET.GLFW
open Silk.NET.Windowing
open Silk.NET.Input                       
open Silk.NET.OpenGL
open Silk.NET.Maths
open Silk.NET.GLFW
open Graphics2D
open ShadersGLSL
open Xunit
open StbImageSharp
open System.Numerics



// This is for options where the is meaningful info to return with None

// Because Silk input is closely tied to Swift graphics, we need to define their plugins in the same project
type SilkWindow(silkWindow:IWindow) =
    // What comes below is ugly because OpenGL is a big stateful mess and
    // things need to be done in the right order
    // Function to convert a Matrix4x4from screen coordinates to normalized coordinates
   
        
    let _gl =
        silkWindow.Initialize()
        silkWindow.CreateOpenGL()
    do
        silkWindow.Initialize()
  
    let defaultShader = (Shader.getDefaultShaderProgram _gl)
            
    interface Window
    
    member val GL = _gl with get
    member val DefaultShaderProgram = defaultShader with get
    
    member this.ConvertToNormalizedMatrix (matrix:Matrix4x4) =
        let width = float32 silkWindow.Size.X
        let height = float32 silkWindow.Size.Y
        // Calculate scaling factors
        let scaleX = 2.0f / width
        let scaleY = -2.0f / height
        // Create the normalized matrix
        let m11 = matrix.M11 
        let m12 = matrix.M12
        let m13 = matrix.M13
        let m14 = matrix.M14 
        let m21 = matrix.M21
        let m22 = matrix.M22
        let m23 = matrix.M23
        let m24 = matrix.M24
        let m31 = matrix.M31
        let m32 = matrix.M32
        let m33 = matrix.M33
        let m34 = matrix.M34
        let m41 = matrix.M41  * scaleX
        let m42 = matrix.M42  * scaleY
        let m43 = matrix.M43
        let m44 = matrix.M44
        Matrix4x4(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44)
        
// Calculate scaling factors

    
    member this.SilkWindow = silkWindow
    member this.ScreenToNormalizedMatrix (matrix:Matrix4x4) =
        let width = float32 silkWindow.Size.X
        let height = float32 silkWindow.Size.Y
        Matrix4x4.CreateTranslation(Vector3(-400f,-300f,0f))* matrix * Matrix4x4.CreateScale(
                            2f/(float32 silkWindow.Size.X),
                            -2f/(float32 silkWindow.Size.Y),
                            1f) 
        // Calculate scaling factors
       
    member this.SetBackgroundColor color =
        _gl.ClearColor(color)
    member this.Clear() =
        _gl.Clear(ClearBufferMask.ColorBufferBit)
     member this.Display() =
        let ctxt = silkWindow.GLContext
        ctxt.SwapBuffers()


type SilkImage(path:string, silkWindow:SilkWindow) =
    let checkErrors = false
    let  glCheckError() =
        if checkErrors then
            let error = silkWindow.GL.GetError()
            if error <> GLEnum.NoError then
                Assert.Fail $"OpenGL error : {error}"
        else
            ()
    let positionLoc = 0u
    
    let loadTexture path =
        let texture = silkWindow.GL.GenTexture()
        glCheckError()
        silkWindow.GL.ActiveTexture(TextureUnit.Texture0)
        glCheckError()
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, texture)
        glCheckError()
        silkWindow.GL.PixelStore( PixelStoreParameter.UnpackAlignment, 1)
        glCheckError()
        //silkWindow.GL.TexParameterI(GLEnum.Texture2D,GLEnum.TextureMinFilter, GLEnum.Nearest);
        //silkWindow.GL.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, GLEnum.Nearest);
        let image = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha)
        use ptrv  = fixed image.Data
        let ptrv' = NativeInterop.NativePtr.toVoidPtr ptrv
        silkWindow.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, uint32 image.Width,
                                    uint32 image.Height, 0,
                                    PixelFormat.Rgba, PixelType.UnsignedByte, ptrv')
        glCheckError()
        silkWindow.GL.GenerateMipmap(GLEnum.Texture2D)
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, 0u)
        (texture,image)
        
  
    
        
             
    let (_texture,_image) = loadTexture path
    let _scaleMatrix = Matrix4x4.CreateScale(
        float32 _image.Width/(float32 silkWindow.SilkWindow.Size.X/2f), 
        float32 _image.Height/ (float32 silkWindow.SilkWindow.Size.Y/2f),
        1f)
    let matrix4x4ToOpenGLArray (matrix: Matrix4x4) : float32[] =
        [|
            matrix.M11; matrix.M12; matrix.M13; matrix.M14
            matrix.M21; matrix.M22; matrix.M23; matrix.M24
            matrix.M31; matrix.M23; matrix.M33; matrix.M34
            matrix.M41; matrix.M42; matrix.M43; matrix.M44
        |]
  
    let _vao = silkWindow.GL.GenVertexArray()
    do
        silkWindow.GL.BindVertexArray(_vao)
        glCheckError()
    let _vbo = silkWindow.GL.GenBuffer()
    let _ebo = silkWindow.GL.GenBuffer()
  

    let vertices =
            let h = (float32) _image.Height
            let w = (float32) _image.Width
            [|
                w;     0.0f; 0.0f; 1.0f; 0.0f;  // top right
                w;     h;    0.0f; 1.0f; 1.0f; // bottom right
                // notice we have to indent to make the - line up with the block
                0f;    h;    0.0f; 0.0f; 1.0f; // bottom left
                0.0f;  0.0f; 0.0f; 0.0f; 0.0f // top left
            |]
        
    
    let indices =
        [|
            0u; 1u; 3u;
            1u; 2u; 3u
        |]

            
    do
        //set vertex buffer
        use ptrv  = fixed vertices
        let ptrv' = NativePtr.toVoidPtr ptrv
        silkWindow.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo)
        glCheckError()
        silkWindow.GL.BufferData(BufferTargetARB.ArrayBuffer, unativeint(vertices.Length * sizeof<float>),
                                     ptrv', BufferUsageARB.StaticDraw)
        glCheckError()
        use ptri = fixed indices
        let ptri' = NativeInterop.NativePtr.toVoidPtr ptri
        silkWindow.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo)
        glCheckError()
        silkWindow.GL.BufferData(BufferTargetARB.ElementArrayBuffer, unativeint (indices.Length * sizeof<uint>),
                                 ptri', BufferUsageARB.StaticDraw)
        glCheckError()
        silkWindow.GL.EnableVertexAttribArray(positionLoc)
        glCheckError()               
     
        silkWindow.GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false,
                                          uint32 (sizeof<float32>*5), nativeint 0)
        glCheckError()
        let texCoordLoc = 1u;
        silkWindow.GL.EnableVertexAttribArray(texCoordLoc)
        glCheckError()
        silkWindow.GL.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float,
                                         false, uint32(5 * sizeof<float32>), (3 * sizeof<float32>));
        glCheckError()
     
        silkWindow.GL.BindVertexArray(0u)
        glCheckError()
        silkWindow.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0u)
        glCheckError()
        silkWindow.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0u)
        glCheckError()
        
        // when we leave the do block vertices and indices becomes unpinned
   
    member val Window = silkWindow with get  
 
   
    member this.Draw (matrix:Matrix4x4) =
        silkWindow.GL.UseProgram(silkWindow.DefaultShaderProgram)
        glCheckError()
        silkWindow.GL.BindVertexArray(_vao)
        glCheckError()
        silkWindow.GL.ActiveTexture(TextureUnit.Texture0)
        glCheckError()
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, _texture)
        glCheckError()
        let uniformLocation =
            silkWindow.GL.GetUniformLocation(silkWindow.DefaultShaderProgram,"xformMatrix")
        //if uniformLocation = -1 then
        //    failwith "Could not find uniform location"    
        glCheckError()

        let matrix4x4=  silkWindow.ScreenToNormalizedMatrix matrix
    // Convert to an OpenGL-compatible float array in column-major order
        let openGLMatrix = matrix4x4ToOpenGLArray matrix4x4
       
        silkWindow.GL.UniformMatrix4(uniformLocation,false, ReadOnlySpan<float32>(openGLMatrix))
        glCheckError()
        silkWindow.GL.DrawElements(PrimitiveType.Triangles, uint32 indices.Length, DrawElementsType.UnsignedInt,
                                  IntPtr.Zero.ToPointer())

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
            let silkWindow = (window :?> SilkWindow)
            SilkImage(path, silkWindow) 
        member this.DrawImage (matrix:Matrix4x4) (image:Image)  =
            let silkImage = image :?> SilkImage
            silkImage.Draw matrix
            silkImage.Window
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