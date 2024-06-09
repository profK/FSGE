module Tests

open System
open System.Drawing
open System.Numerics
open System.Reflection
open System.Threading
open ManagerRegistry
open Xunit
open Xunit.Abstractions

// Graphics Manager tests
type GraphicsManagerTestsFixture() =
    do
        addManager(typeof<SilkGraphicsAndInput.SilkGraphicsManager>)
type GraphicsManagerTests(output:ITestOutputHelper ) =
    interface IClassFixture<GraphicsManagerTestsFixture>
    
    [<Fact>]
    member _.testGraphicsManager() =
        let graphicsManagerOpt = ManagerRegistry.getManager<Graphics2D.IGraphicsManager>()
        match graphicsManagerOpt with
        | Some graphicsManager -> Assert.True(true)
        | None -> Assert.True(false)
    [<Fact>]
    member _.testWindowCreation() =
        let window = Graphics2D.Window.create 800 600 "Purple Haze"
        Graphics2D.Window.Clear(Color.Purple) window
        Graphics2D.Window.Display window
        Thread.Sleep(5000)
        Graphics2D.Window.close window
        Assert.True(true)
    [<Fact>]
    member _.testImageDrawing() =
        output.WriteLine "Test drawing..."
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let image = Graphics2D.Window._graphicsManager.LoadImage "NGTL_tex.png" window
        Graphics2D.Window.Clear(Color.Blue) window
        let xform = Matrix4x4.CreateTranslation(Vector3(100.0f,300.0f,0.0f))
                    * Matrix4x4.CreateRotationZ((float32 Math.PI)/4.0f)
        Graphics2D.Window.DrawImage image xform window
        Graphics2D.Window.Display window
        Thread.Sleep(5000)
        Graphics2D.Window.close window
        Assert.True(true)
    [<Fact>]
    member _.testWindowWidth() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let width = Graphics2D.Window._graphicsManager.WindowWidth window
        Graphics2D.Window.close window
        Assert.Equal(800, width)
    [<Fact>]
    member _.testWindowHeight() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let height = Graphics2D.Window._graphicsManager.WindowHeight window
        Graphics2D.Window.close window
        Assert.Equal(600, height)

    [<Fact>]
    member _.testSetTitle() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        Graphics2D.Window.setTitle window "New Title"
        let title = Graphics2D.Window._graphicsManager.WindowTitle window
        Graphics2D.Window.close window
        Assert.Equal("New Title", title)
    [<Fact>]
    member _.testSetPosition() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        Graphics2D.Window.setPosition window (Point(100,100))
        let position = Graphics2D.Window._graphicsManager.WindowPosition window
        Thread.Sleep(5000)
        Graphics2D.Window.close window
        Assert.Equal(Point(100,100), position)
    [<Fact>]
    member _.testSize() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let size = Graphics2D.Window._graphicsManager.WindowSize window
        Graphics2D.Window.close window
        Assert.Equal(Size(800,600), size)
    [<Fact>]
    member _.testSetSize() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        Graphics2D.Window.setSize window (Size(400,400))
        let size = Graphics2D.Window._graphicsManager.WindowSize window
        Graphics2D.Window.close window
        Assert.Equal(Size(400,400), size)
  

