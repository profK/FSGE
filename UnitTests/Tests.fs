module Tests

open System
open System.Drawing
open System.Numerics
open System.Reflection
open System.Threading
open Devices
open Input
open Input.HIDScanCodes
open Logger
open ManagerRegistry
open SilkGraphicsOGL
open Xunit
open Xunit.Abstractions
open SilkDevices
open xUnitLogger

// Graphics Manager tests


type GraphicsManagerTestsFixture() =
    do
        addManager(typeof<SilkGraphicsManager>)
        addManager(typeof<SilkDevices.SilkDeviceManager>)
        addManager(typeof<xUnitLogger.xUnitLogger>)
       
type GraphicsManagerTests(output:ITestOutputHelper ) =
    let ITestOutputHelper = output
    let logger =
        match getManager<ILogger>() with
        | Some logger -> (logger :?> xUnitLogger).injectOutput output
        | None -> failwith "No logger found"
        
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
        
    member this.recursivePrintDevices indent (deviceList:DeviceNode seq) =
        deviceList |> Seq.iter (fun node -> output.WriteLine(indent+node.Name+": "+node.Path); 
                                             match node.Children with
                                             | Some children -> this.recursivePrintDevices (indent+"  ") children 
                                             | None -> ())
    member this.recursiveValueCheck deviceContext deviceList indent =
        deviceList |> Seq.iter (fun node -> 
            match Devices.TryGetDeviceValue deviceContext node.Path with
            | Some value -> output.WriteLine(indent+node.Name+": "+value.ToString())
            | None -> output.WriteLine(node.Name+": No value found")
            match node.Children with
            | Some children -> this.recursiveValueCheck deviceContext children (indent+"    ")
            | None -> ())    
    [<Fact>]
    member this.testDeviceList() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext
        let devarray = deviceList |> Seq.toArray
        Assert.True(devarray.Length > 0)
        output.WriteLine "Devices:"
        deviceList |>  this.recursivePrintDevices ""
        Graphics2D.Window.close window    
  
    [<Fact>]
    member this.testKbValues() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext
        let mutable exit = false
        output.WriteLine "Press keys to test, press esc to end test"
        while not exit do
            Graphics2D.Window.DoEvents window     
            let value = Devices.TryGetDeviceValue deviceContext "Keyboard0"
            match value with
            |Some devValue ->
                match devValue with
                | Devices.KeyboardValue(value) ->
                    let keyCodes:ScanCode array =
                        value
                        |> Array.map (fun v -> Devices.MapPlatformScanCodeToHID v)
                        |> Array.map (fun v -> enum<ScanCode> (int32 v))
                    output.WriteLine($"Keyboard0: %A{keyCodes}")
                    exit <- Array.contains ScanCode.Escape keyCodes
                | _ -> output.WriteLine($"Not a keyboard value {devValue.ToString()}")
            | None -> output.WriteLine("No value found for Keyboard0")   
            Thread.Sleep(1000)
        Graphics2D.Window.close window

    [<Fact>]
    member this.testAllValues() =
        let window = Graphics2D.Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext //todo make this unecessary
        let mutable lastValueMap = Map.empty    
        let mutable exit = false
        while not exit do
            Graphics2D.Window.DoEvents window    
            let valueMap = Devices.GetDeviceValuesMap deviceContext
            output.WriteLine("Value count: "+valueMap.Count.ToString())
            valueMap |> Map.toArray
            |> Array.filter (fun (k,v) ->
                not (lastValueMap.ContainsKey k) || (lastValueMap.[k] = v))   
            |>Array.iter (fun kv -> output.WriteLine($"{fst kv}: {snd kv}"))
            lastValueMap <- valueMap
            exit <- if valueMap.ContainsKey "Keyboard0" then
                        let value = valueMap.["Keyboard0"]
                        match value with
                        | Devices.KeyboardValue(value) ->
                            let keyCodes:ScanCode array =
                                value
                                |> Array.map (fun v -> Devices.MapPlatformScanCodeToHID v)
                                |> Array.map (fun v -> enum<ScanCode> (int32 v))
                            Array.contains ScanCode.Escape keyCodes
                        | _ -> false
                    else
                        //output.WriteLine "Warning Keyboard0 not detechhhhted"
                        false
            Thread.Sleep(1000)
        Graphics2D.Window.close window
    