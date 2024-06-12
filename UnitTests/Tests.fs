module Tests

open System
open System.Drawing
open System.Numerics
open System.Reflection
open System.Threading
open ManagerRegistry
open SilkGraphicsOGL
open Xunit
open Xunit.Abstractions
open Devices

// Graphics Manager tests
type GraphicsManagerTestsFixture() =
    do
        addManager(typeof<SilkGraphicsManager>)
        addManager(typeof<SilkDevices.SilkInputManager>)
type GraphicsManagerTests(output:ITestOutputHelper ) =
    let ITestOutputHelper = output;
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
        let xform = Graphics2D.Window.CreateTranslation(Vector2(100.0f,300.0f))
                    * Graphics2D.Window.CreateRotation((float32 Math.PI)/4.0f)
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
        
    member this.recursivePrintDevices indent deviceList =
        deviceList |> List.iter (fun node -> output.WriteLine(indent+node.Name+": "+node.Path); 
                                             match node.Children with
                                             | Some children -> this.recursivePrintDevices (indent+"  ") children 
                                             | None -> ())   
    [<Fact>]
    member this.testDeviceList() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.tryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext
        Assert.True(deviceList.Length > 0)
        output.WriteLine "Devices:"
        deviceList |>  this.recursivePrintDevices ""
        Graphics2D.Window.close window    
  
    [<Fact>]
    member this.testDeviceValues() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.tryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext
        deviceList |> List.filter (fun node -> node.Type = Keyboard)
        |> List.iter (fun node ->
            output.WriteLine($"Testing Keyboard: {node.Name}")
            output.WriteLine("Press just ESC to exit")
            let mutable finished=false
            while not finished do
                let deviceValue = Devices.tryGetDeviceValue deviceContext node.Path
                match deviceValue with
                | Some deviceValue  ->
                    match deviceValue with
                    | KeyboardValue keys -> 
                        keys |> Array.iter (fun key -> output.WriteLine(key.ToString()))
                        if keys |> Array.exists (fun key -> key = 27u) then
                            finished <- true
                
                    | _ -> ()                
                | None -> failwith "No device value found"
                Thread.Sleep(100)
            )
        Graphics2D.Window.close window
