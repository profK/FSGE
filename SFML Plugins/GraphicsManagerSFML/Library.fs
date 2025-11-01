namespace GraphicsManagerSFML
open System.IO
open ManagerRegistry
open SFML.Graphics 
open SFML.System
open SFML.Window
open TDE3ManagerInterfaces.GraphicsManagerInterface
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
    

type TransformSFML (sfXform:SFTransform) =
    member val sfXform = sfXform with get
    interface Transform with
      override this.Multiply (otherVec:Vector2) : Vector2 =
          let result =sfXform.TransformPoint(Vector2f(otherVec.X,otherVec.Y))
          Vector2(result.X,result.Y)
      override this.Multiply (otherXform:Transform) : Transform =
          TransformSFML(sfXform * (otherXform:?>TransformSFML).sfXform)

type ImageSFML(tex:SFTexture,rect) =
    member val sprite = new SFSprite(tex,rect) with get
    
    new(tex) =
        ImageSFML(tex,IntRect(
            Vector2i(0,0),
            Vector2i(int(tex.Size.X),int(tex.Size.Y))))
    interface Image with
        member this.Size =
            let size = this.sprite
            Vector2(float32(this.sprite.TextureRect.Width),
                       float32(this.sprite.TextureRect.Height))
        member this.SubImage(newrect:Rectangle) =
            ImageSFML(tex,IntRect(
                Vector2i(int(newrect.X+rect.Left),int(newrect.Y+rect.Top)),
                Vector2i(int(newrect.Width),int(newrect.Height))))
             

       
        
and WindowSFML(sfmlWindow:SFWindow,gm:GraphicsManager) =
    inherit Window(gm) with
        member val sfmlWindow = sfmlWindow with get
        override this.Start(startFunc) =
            startFunc this
        override this.Start() =
            ()
        override this.DrawImage (xform:Transform) (image:Image) =
            let state = SFRenderStates((xform:?>TransformSFML).sfXform)
            let sprite = (image:?>ImageSFML).sprite
            sfmlWindow.Draw(sprite,state) 
            ()
        override this.IdentityTransform =
            TransformSFML(SFTransform.Identity)
        override this.LoadImage(stream:Stream) = 
            ImageSFML(new SFTexture(stream))
        override this.RotationTransform(degrees) =
            let xform = SFTransform.Identity
            xform.Rotate(degrees)
            TransformSFML(xform)
        override this.ScaleTransform(x:float32) (y:float32) =
            let xform = SFTransform.Identity
            xform.Scale(x,y)
            TransformSFML(xform)
        override this.ScreenSize =
            Vector2(
                float32(SFVideoMode.DesktopMode.Width),
                float32(SFVideoMode.DesktopMode.Height))
        override this.TranslationTransform(x) (y) =
            let xform = SFTransform.Identity
            xform.Translate(x,y)
            TransformSFML(xform)

        override this.Clear(color) =
            sfmlWindow.Clear (SFColor(color.R,color.G,color.B,color.A))
            ()
        override this.Close() = sfmlWindow.Close()
        override this.Show() = sfmlWindow.Display()
        override this.IsOpen() = sfmlWindow.IsOpen
            
[<Manager("Graphics interface forSFML",
          supportedSystems.Windows )>]
type GraphicsManagerSFML() =
    interface GraphicsManager with
        override this.OpenWindow videoMode name  =
            match videoMode with
            | FullScreen ->
                WindowSFML(new SFWindow(SFVideoMode.FullscreenModes[0],
                           name),this)
            | FullScreenWindow ->
                WindowSFML(new SFWindow(SFVideoMode.DesktopMode,
                           name),this)
            | Windowed (x, y) ->
                WindowSFML(new SFWindow(SFVideoMode(x,y),
                           name),this)
        
        member this.GraphicsListeners = failwith "todo"
        member this.GraphicsListeners with set value = failwith "todo"
        