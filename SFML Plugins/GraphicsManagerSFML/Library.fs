namespace GraphicsManagerSFML
open System.IO
open ManagerRegistry
open SFML.Graphics 
open SFML.System
open SFML.Window
open Graphics2D
open System.Numerics
open System.Drawing


//aliases
type SFImage = SFML.Graphics.Image
type SFTexture = SFML.Graphics.Texture
type SFSprite = SFML.Graphics.Sprite
type SFWindow =  SFML.Graphics.RenderWindow
type SFTransform = SFML.Graphics.Transform
type SFVideoMode = SFML.Window.VideoMode
type SFRenderStates = SFML.Graphics.RenderStates
type SFColor = SFML.Graphics.Color
    

type ImageSFML( tex:SFTexture,optrect:IntRect option , window:WindowSFML) =
  
    let rect =
        match optrect with
        |Some r -> r
        |None ->  IntRect(0,0,int tex.Size.X,int tex.Size.Y)
    member val sprite = new SFSprite(tex,rect)
       
       
    member val window =window with get
    
 
    interface Image with
        member this.Size:Graphics2D.Size =
            {Width = int32 this.sprite.TextureRect.Width; Height = int32 this.sprite.TextureRect.Height}
  //TODO: less than optimal subimage creation, only cuts from whole texture
    static member CreateSubImage (tex:SFTexture) (x:int32) (y:int32) (width:int32) (height:int32) (window:WindowSFML) : Graphics2D.Image =
        ImageSFML(tex,Some(IntRect(
            Vector2i(int(x),int y),
            Vector2i(int(width),int(height)))),window) :> Graphics2D.Image       

       
        
and WindowSFML(mode:SFVideoMode, name,gm:IGraphicsManager) =
    let sfmlWindow = new SFWindow(mode, name)
    member val Name = name with get
    member val GraphicsManager = gm with get
    member val SFMLWindow = sfmlWindow with get

    member val Size:Graphics2D.Size = 
        {Width = int32 sfmlWindow.Size.X; Height = int32 sfmlWindow.Size.Y} with get
    interface Window with
        
 
[<Manager("Graphics interface for SFML", supportedSystems.Windows, [||], 0)>]
type GraphicsManagerSFML() =
    interface IGraphicsManager with
        override this.CreateWindow width height name  =
                //TODO add video mode tp IGraphicsManaa
                WindowSFML(SFVideoMode(uint32 width,uint32 height),
                           name,this)
        override this.CloseWindow window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            sfmlWindow.Close()
            ()
        override this.WindowWidth window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            int sfmlWindow.Size.X
        override this.WindowHeight window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            int sfmlWindow.Size.Y
        override this.WindowTitle window =
            let windowSFML = (window :?> WindowSFML)
            windowSFML.Name
        override this.SetWindowTitle window title =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            sfmlWindow.SetTitle (title)
            window
        override this.WindowPosition window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            let pos = sfmlWindow.Position
            {X = int32 pos.X; Y = int32 pos.Y}
        override this.SetWindowPosition window position =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            sfmlWindow.Position <- Vector2i(int(position.X),int(position.Y))
            window
        override this.WindowSize window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            let size = sfmlWindow.Size
            {Width = int32 size.X; Height = int32 size.Y}
        override this.SetWindowSize window size =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            sfmlWindow.Size <- Vector2u(uint32(size.Width),uint32(size.Height))
            window
        override this.LoadImageFromStream stream window =
            let tex = new SFTexture(stream)
            ImageSFML(tex,None,window :?> WindowSFML) :> Graphics2D.Image
        override this.LoadImageFromPath path window =
            let tex = new SFTexture(path)
            ImageSFML(tex,None, window :?> WindowSFML) :> Graphics2D.Image
        
        override this.CreateSubImage image x y width height =
            let parent  =  image :?> ImageSFML
            ImageSFML.CreateSubImage
               parent.sprite.Texture
               (int32 x) (int32 y) (int32 width) (int32 height)
               parent.window
                
        
        override this.DrawImage matrix image coloropt =
            let imageSF =  (image :?> ImageSFML)
            let sprite =imageSF.sprite
            let window = imageSF.window
            sprite.Color <-
                match coloropt with
                | Some color ->
                    SFColor(color.R,color.G,color.B,color.A)                  
                | None ->
                    SFColor.White
            let transform = SFTransform(
                        float32 matrix.M11, float32 matrix.M12, float32 matrix.M14,
                        float32 matrix.M21, float32 matrix.M22, float32 matrix.M24,
                        float32 matrix.M41, float32 matrix.M42, float32 matrix.M44)
            // this doesnt account for sub images yet TODO
            let ox =   -(int32 sprite.Texture.Size.X/2)
            let oy =   -(int32 sprite.Texture.Size.Y/2)
            let objcenter = SFTransform.Identity
            objcenter.Translate(float32 ox,float32 oy)
            let screencenter = SFTransform.Identity
            screencenter.Translate(float32 (window.Size.Width/2), float32 (window.Size.Height/2 ))
            let state = RenderStates(screencenter * transform * objcenter)
            let sfmlWindow =window.SFMLWindow
            sfmlWindow.Draw(sprite,state)
            imageSF.window

   
    // This fills the window with a color, clearing the previous frame
        override this.Clear color window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            sfmlWindow.Clear (SFColor(color.R,color.G,color.B,color.A))
            window
        // Drawing is double buffered to prevent tearing.
        // This displays the next frame on the window
        override  this.Display window =
             let sfmlWindow = (window :?> WindowSFML).SFMLWindow
             sfmlWindow.Display()
             window
    // This processes window events and device input events
    // It should be called once per frame
    
        override this.DoEvents window =
            let sfmlWindow = (window :?> WindowSFML).SFMLWindow
            sfmlWindow.DispatchEvents()
            ()
        
       