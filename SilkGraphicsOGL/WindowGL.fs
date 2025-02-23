// This file implements the Window
// and Image interfaces for the SilkGraphicsOGL library



module SilkGraphicsOGL.WindowGL

open System
open System.IO
open System.Numerics
open System.Runtime.InteropServices
open System.Threading
open Microsoft.FSharp.NativeInterop
open ShadersGLSL
open Silk.NET.OpenGL
open Silk.NET.Windowing
open Graphics2D
open StbImageSharp


// These set a callback for OpenGL debug messages that prints them
// to the console

let MessageCallback (source:GLEnum) (cbtype:GLEnum) (id:int) (severity:GLEnum) (length:int)
        (message:nativeint) (userParam:nativeint) =
    let msg = Marshal.PtrToStringAnsi message
    printfn "GL CALLBACK:  type = %s, severity = 0x%s, message = %s\n" 
        (cbtype.ToString()) (severity.ToString()) msg
let messageCB:DebugProc = new DebugProc(MessageCallback)        


// This is a type that represents a window on the screen
// It implements the Window interface from the Graphics2D library
type SilkWindow(silkWindow:IWindow) =
    // What comes below is ugly because OpenGL is a big stateful mess
    // and things need to be done in the right order
    // This is why we have a class that wraps the window
    // and the OpenGL context
    
    // This is an internal biding to OpenGL context
    let _gl =
        silkWindow.Initialize()
        silkWindow.CreateOpenGL()
    do
       // silkWindow.Initialize()

        _gl.Enable GLEnum.DebugOutput
        // This is the callback that prints the debug messages
        // to the console
        // The IntPtr.Zero.ToPointer() is a hack to get a null pointer
        _gl.DebugMessageCallback( MessageCallback, IntPtr.Zero.ToPointer() )
        // This sets the drawing mode to blend
        // and sets the blending function
        _gl.Enable(GLEnum.Blend)
        _gl.BlendFunc(GLEnum.SrcAlpha , GLEnum.OneMinusSrcAlpha);
    //This sets a binding to the default shader program
    //provided by the ShadersGLSL module
    let defaultShader = (Shader.getDefaultShaderProgram _gl)
         
    // This makes this an implementation of the Window opaque
    // interface        
    interface Window
    
    // Ths provides read-only access to the OpenGL context by
    // other classes
    member val GL = _gl with get
    // This provides read-only access to the default shader program
    // by other classes
    member val DefaultShaderProgram = defaultShader with get
    

    // This provides access to the underlying IWindow provided
    // by the Silk.NET library
    member this.SilkWindow = silkWindow
    
     // Function to convert a Matrix4x4from screen coordinates to normalized
    // coordinates
    member this.ScreenToNormalizedMatrix (matrix:Matrix4x4) =
        let width = float32 silkWindow.Size.X
        let height = float32 silkWindow.Size.Y
       //Matrix4x4.CreateTranslation(Vector3(-400f,-300f,0f)
        matrix*  
        Matrix4x4.CreateScale(2f/width,
                             -2f/height, 1f) *
        Matrix4x4.CreateTranslation(-1f,1f,0f)

    //This function sets the background color of the window
    //It takes a Graphics2D.Color object as an argument
    //and fills the window with that color when the Clear method is called
    member this.SetBackgroundColor (color:Graphics2D.Color) =
        let syscolor = System.Drawing.Color.FromArgb(int color.A,int color.R,int color.G,int color.B)
        _gl.ClearColor(syscolor)
    //This function clears the window
    member this.Clear() =
        _gl.Clear(ClearBufferMask.ColorBufferBit)
    //This function swaps the buffers
    //This is necessary because OpenGL uses double buffering
    member this.Display() =
        let ctxt = silkWindow.GLContext
        ctxt.SwapBuffers()

// This is a function that loads a texture from a stream
// It returns a tuple with the texture id and the image information
// The texture id is an integer that is used to refer to the texture
// in OpenGL calls
//  This function is at the top level because it is used by the
// SilkImage class
let loadTexture stream (silkWindow:SilkWindow) =
        let texture = silkWindow.GL.GenTexture()
        silkWindow.GL.ActiveTexture(TextureUnit.Texture0)
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, texture)
        silkWindow.GL.PixelStore( PixelStoreParameter.UnpackAlignment, 1)
        //silkWindow.GL.TexParameterI(GLEnum.Texture2D,GLEnum.TextureMinFilter, GLEnum.Nearest);
        //silkWindow.GL.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, GLEnum.Nearest);
        let image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha)
        use ptrv  = fixed image.Data
        // This is a hack to get a void pointer from a native pointer
        // This is necessary because the OpenGL function expects a void pointer
        let ptrv' = NativeInterop.NativePtr.toVoidPtr ptrv
        silkWindow.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, uint32 image.Width,
                                    uint32 image.Height, 0,
                                    PixelFormat.Rgba, PixelType.UnsignedByte, ptrv')
        silkWindow.GL.GenerateMipmap(GLEnum.Texture2D)
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, 0u)
        (texture,image)
// The API uses Matrix4x4 from System.Numerics to represent transformations
// but OpenGL uses a float32 array in column-major order
// This function converts a Matrix4x4 to a float32 array in column-major order
let matrix4x4ToOpenGLArray (matrix: Matrix4x4) : float32[] =
        [|
            matrix.M11; matrix.M12; matrix.M13; matrix.M14
            matrix.M21; matrix.M22; matrix.M23; matrix.M24
            matrix.M31; matrix.M23; matrix.M33; matrix.M34
            matrix.M41; matrix.M42; matrix.M43; matrix.M44
        |]
// This is a class that represents an image in Silk OGL
// It implements the Image interface from the Graphics2D library
type SilkImage(image:uint32, textureInfo:ImageResult, subTexPosOpt, subTexSizeOpt:Size option,silkWindow:SilkWindow) =
    //The next two lets are used to define the position and size of the subtextur
    //If they are not provided the subtexture is the whole texture
    let subTexPos =
        match subTexPosOpt with
        | Some pos -> pos
        | None -> Vector2(0f,0f)
    let subTexSize =
        match subTexSizeOpt with
        | Some size -> size
        | None -> {Width =textureInfo.Width; Height =textureInfo.Height}
    // What comes next sets up the OpenGL buffers
    // this is ugly because OpenGL is a big stateful mess
    // and things need to be done in the right order
    // See the OpenGL docs for more information
    let positionLoc = 0u
    //open gl buffers
    let _vao = silkWindow.GL.GenVertexArray()
    let _vbo = silkWindow.GL.GenBuffer()
    let _ebo = silkWindow.GL.GenBuffer()

    //drawing information
    let vertices =
            let w = float32 subTexSize.Width
            let h = float32 subTexSize.Height
            let texTopLeft = Vector2(subTexPos.X/float32 textureInfo.Width,subTexPos.Y/float32 textureInfo.Height)
            let texSizeN = Vector2(w/float32 textureInfo.Width,
                                   h/float32 textureInfo.Height)
            let texBottomRight = texTopLeft + texSizeN
            [|
                w;     0.0f; 0.0f; texBottomRight.X; texTopLeft.Y;  // top right
                w;     h;    0.0f; texBottomRight.X; texBottomRight.Y; // bottom right
                // notice we have to indent to make the - line up with the block
                0f;    h;    0.0f; texTopLeft.X; texBottomRight.Y; // bottom left
                0.0f;  0.0f; 0.0f; texTopLeft.X; texTopLeft.Y // top left
            |]
    let indices =
        [|
            0u; 1u; 3u;
            1u; 2u; 3u
        |]
    
    //This is a do block to set up the OpenGL buffers
    do
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, image)
        silkWindow.GL.BindVertexArray(_vao)
        //set vertex buffer
        use ptrv  = fixed vertices
        let ptrv' = NativePtr.toVoidPtr ptrv
        silkWindow.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo)
        silkWindow.GL.BufferData(BufferTargetARB.ArrayBuffer, unativeint(vertices.Length * sizeof<float>),
                                     ptrv', BufferUsageARB.StaticDraw)
        use ptri = fixed indices
        let ptri' = NativeInterop.NativePtr.toVoidPtr ptri
        silkWindow.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo)
        silkWindow.GL.BufferData(BufferTargetARB.ElementArrayBuffer, unativeint (indices.Length * sizeof<uint>),
                                 ptri', BufferUsageARB.StaticDraw)
        silkWindow.GL.EnableVertexAttribArray(positionLoc)
        silkWindow.GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false,
                                          uint32 (sizeof<float32>*5), nativeint 0)
        let texCoordLoc = 1u;
        silkWindow.GL.EnableVertexAttribArray(texCoordLoc)
        silkWindow.GL.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float,
                                         false, uint32(5 * sizeof<float32>), (3 * sizeof<float32>));
        silkWindow.GL.BindVertexArray(0u)
        silkWindow.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0u)
        silkWindow.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0u)
        
        // when we leave the do block vertices and indices becomes unpinned
    member val Window = silkWindow with get  
 
    //Secondary constructors to create a new SilkImage from a file path
    // or input stream
    new(stream:Stream, silkWindow:SilkWindow) =
        let (image,texture) = loadTexture stream silkWindow
        SilkImage(image, texture, None, None, silkWindow)
    new (path:string, silkWindow:SilkWindow) =
        let stream = new FileStream(path, FileMode.Open, FileAccess.Read)
        SilkImage(stream, silkWindow)
   
    //This is the Draw method that draws the image on the screen
    member this.Draw (matrix:Matrix4x4) tint =
        silkWindow.GL.UseProgram(silkWindow.DefaultShaderProgram)
        silkWindow.GL.BindVertexArray(_vao)
        silkWindow.GL.ActiveTexture(TextureUnit.Texture0)
        silkWindow.GL.BindTexture(TextureTarget.Texture2D, image)
        let xformLocation =
            silkWindow.GL.GetUniformLocation(silkWindow.DefaultShaderProgram,"xformMatrix")
        let tintLocation =
            silkWindow.GL.GetUniformLocation(silkWindow.DefaultShaderProgram,"tint")    
        let centeredMatrix =             
                Window.CreateTranslation(
                    Vector2((float32 -subTexSize.Width)/2f,
                            (float32 -subTexSize.Height)/2f)) *
                matrix
        let matrix4x4=  silkWindow.ScreenToNormalizedMatrix centeredMatrix
    // Convert to an OpenGL-compatible float array in column-major order
        let openGLMatrix = matrix4x4ToOpenGLArray matrix4x4
        silkWindow.GL.UniformMatrix4(xformLocation,false, ReadOnlySpan<float32>(openGLMatrix))
        silkWindow.GL.Uniform4(tintLocation, (float32 tint.R)/255f, (float32 tint.G)/255f,
                               (float32 tint.B)/255f, (float32 tint.A)/255f)
        silkWindow.GL.DrawElements(PrimitiveType.Triangles, uint32 indices.Length, DrawElementsType.UnsignedInt,
                                  IntPtr.Zero.ToPointer())
        
    //This is a method to create a subimage from an existing image
    member this.CreateSubImage x y width height =
        SilkImage(image, textureInfo, Some(Vector2(float32 x, float32 y)),
              Some({Width=width;Height=height}), silkWindow)

    // this implelemnts the Image interface which has
    // one method that returns the size of the image
    interface Image with
        member this.Size =  {Width=textureInfo.Width; Height=textureInfo.Height}


    