module SilkGraphicsOGL.WindowGL

open System
open System.IO
open System.Numerics
open Microsoft.FSharp.NativeInterop
open ShadersGLSL
open Silk.NET.OpenGL
open Silk.NET.Windowing
open Graphics2D
open StbImageSharp
open Xunit

type SilkWindow(silkWindow:IWindow) =
    // What comes below is ugly because OpenGL is a big stateful mess and
    // things need to be done in the right order
    // Function to convert a Matrix4x4from screen coordinates to normalized coordinates
   
        
    let _gl =
        silkWindow.Initialize()
        silkWindow.CreateOpenGL()
    do
        //// JW: Why are you calling this twice? If this is intentional, it should be documented because WTF
        silkWindow.Initialize()
    
    //// JW: Unnecessary parens
    let defaultShader = (Shader.getDefaultShaderProgram _gl)
            
    //// JW: This should be qualified
    interface Window
    
    member val GL = _gl with get
    member val DefaultShaderProgram = defaultShader with get
    
   
        
// Calculate scaling factors

    //// JW: Make `val SilkWindow = silkWindow with get`
    member this.SilkWindow = silkWindow
    member this.ScreenToNormalizedMatrix (matrix:Matrix4x4) =
        let width = float32 silkWindow.Size.X
        let height = float32 silkWindow.Size.Y
        Matrix4x4.CreateTranslation(Vector3(-400f,-300f,0f))* matrix * Matrix4x4.CreateScale(
                            2f/(float32 silkWindow.Size.X),
                            -2f/(float32 silkWindow.Size.Y),
                            1f) 
        // Calculate scaling factors
    
    member this.SetBackgroundColor (color:Graphics2D.Color) =
        let syscolor = System.Drawing.Color.FromArgb(int color.A,int color.R,int color.G,int color.B)
        _gl.ClearColor(syscolor)
    member this.Clear() =
        _gl.Clear(ClearBufferMask.ColorBufferBit)
     member this.Display() =
        let ctxt = silkWindow.GLContext
        ctxt.SwapBuffers()


type SilkImage(stream:Stream, silkWindow:SilkWindow) =
    let checkErrors = false
    let  glCheckError() =
        if checkErrors then
            let error = silkWindow.GL.GetError()
            if error <> GLEnum.NoError then
                Assert.Fail $"OpenGL error : {error}"
        else
            ()
    let positionLoc = 0u
    
    let loadTexture stream =
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
        let image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha)
        use ptrv  = fixed image.Data
        let ptrv' = NativeInterop.NativePtr.toVoidPtr ptrv
        silkWindow.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, uint32 image.Width,
                                    uint32 image.Height, 0,
                                    PixelFormat.Rgba, PixelType.UnsignedByte, ptrv')
        glCheckError()
        silkWindow.GL.GenerateMipmap(GLEnum.Texture2D)
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, 0u)
        (texture,image)
             
    let (_texture,_image) = loadTexture stream
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
 
    //Secondary constructor to create a new SilkImage from a file path
    new(path:String, silkWindow:SilkWindow) =
        let stream = new FileStream(path, FileMode.Open, FileAccess.Read)
        SilkImage(stream, silkWindow)
   
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
        
    member this.CreateSubImage x y width height =
        failwith "Method unimplemented"   

    interface Image 
    
    