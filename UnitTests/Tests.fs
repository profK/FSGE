module Tests

open System
open System.Drawing
open System.Numerics
open System.Reflection
open System.Threading
open AngelCodeText
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
open Graphics2D
open FSGEText

// Graphics Manager tests


type GraphicsManagerTestsFixture() =
    do
        addManager(typeof<SilkGraphicsManager>)
        addManager(typeof<SilkDeviceManager>)
        addManager(typeof<xUnitLogger.xUnitLogger>)
        addManager(typeof<AngelCodeTextRenderer>)
       
type GraphicsManagerTests(output:ITestOutputHelper ) =
    let ITestOutputHelper = output
    let logger =
        match getManager<ILogger>() with
        | Some logger -> (logger :?> xUnitLogger).injectOutput output
        | None -> failwith "No logger found"
        
    interface IClassFixture<GraphicsManagerTestsFixture>
    
    [<Fact>]
    member _.testGraphicsManager() =
        let graphicsManagerOpt = getManager<IGraphicsManager>()
        match graphicsManagerOpt with
        | Some graphicsManager -> Assert.True(true)
        | None -> Assert.True(false)
    [<Fact>]
    member _.testWindowCreation() =
        let window = Window.create 800 600 "Purple Haze"
        Window.Clear {A=0xFFuy;R=0x80uy;G=0uy;B=0x80uy} window
        Window.Display window
        Thread.Sleep(5000)
        Window.close window
        Assert.True(true)
    [<Fact>]
    member _.testImageDrawing() =
        output.WriteLine "Test drawing..."
        let window = Window.create 800 600 "Test Window"
        let image = Window.LoadImageFromPath "NGTL_tex.png" window
        Window.Clear {A=0xFFuy;R=0uy;G=0uy;B=0xFFuy} window
        let xform = Window.CreateTranslation (Vector2(100.0f,300.0f))
                    * Window.CreateRotation((float32 Math.PI)/4.0f)
        Window.DrawImage image xform

        let xform2 = Window.CreateTranslation (Vector2(400.0f,300.0f))
        let subImage = Window.CreateSubImage image 50u 50u 100u 100u
        Window.DrawImage subImage xform2
        Window.Display window
        Thread.Sleep(5000)
        Window.close window
        Assert.True(true)
    [<Fact>]
    member _.testTextDrawing() =
        output.WriteLine "Test text drawing..."
        let window = Window.create 800 600 "Test Window"
        Window.Clear {A=0xFFuy;R=0uy;G=0xFFuy;B=0uy} window
        let xform2 = Window.CreateTranslation (Vector2(400.0f,300.0f))
        let font = Text.LoadFont window "AngelcodeFonts/Latin.fnt"
        let text = Text.CreateText "Hello World" font
        Text.DrawText text xform2
        Window.Display window
        Thread.Sleep(5000)
        Window.close window
        Assert.True(true)    
    [<Fact>]
    member _.testWindowWidth() =
        let window = Window.create 800 600 "Test Window"
        let width = Window._graphicsManager.WindowWidth window
        Window.close window
        Assert.Equal(800, width)
    [<Fact>]
    member _.testWindowHeight() =
        let window = Window.create 800 600 "Test Window"
        let height = Window._graphicsManager.WindowHeight window
        Window.close window
        Assert.Equal(600, height)

    [<Fact>]
    member _.testSetTitle() =
        let window = Window.create 800 600 "Test Window"
        Window.setTitle window "New Title"
        let title = Window._graphicsManager.WindowTitle window
        Window.close window
        Assert.Equal("New Title", title)
    [<Fact>]
    member _.testSetPosition() =
        let pos = {X=100; Y=100} 
        let window = Window.create 800 600 "Test Window"
        Window.setPosition window 
        let position = Window._graphicsManager.WindowPosition window
        Thread.Sleep(5000)
        Window.close window
        
        Assert.Equal ({X=100; Y=100},  position)
    [<Fact>]
    member _.testSize() =
        let window = Window.create 800 600 "Test Window"
        let size = Window._graphicsManager.WindowSize window
        Window.close window
        Assert.Equal( {Width=800;Height=600}, size)
    [<Fact>]
    member _.testSetSize() =
        let window = Window.create 800 600 "Test Window"
        Window.setSize window {Width=400;Height=400}
        let size = Window._graphicsManager.WindowSize window
        Window.close window
        Assert.Equal({Width=400;Height=400},size)
        
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
        let window = Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext
        let devarray = deviceList |> Seq.toArray
        Assert.True(devarray.Length > 0)
        output.WriteLine "Devices:"
        deviceList |>  this.recursivePrintDevices ""
        Window.close window    
  
    [<Fact>]
    member this.testKbValues() =
        let window = Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext
        let mutable exit = false
        output.WriteLine "Press keys to test, press esc to end test"
        while not exit do
            Window.DoEvents window     
            let value = Devices.TryGetDeviceValue deviceContext "Keyboard0"
            match value with
            |Some devValue ->
                match devValue with
                | KeyboardValue(value) ->
                    let keyCodes:ScanCode array =
                        value
                        |> Array.map (fun v -> Devices.MapPlatformScanCodeToHID v)
                        |> Array.map (fun v -> enum<ScanCode> (int32 v))
                    output.WriteLine($"Keyboard0: %A{keyCodes}")
                    exit <- Array.contains ScanCode.Escape keyCodes
                | _ -> output.WriteLine($"Not a keyboard value {devValue.ToString()}")
            | None -> output.WriteLine("No value found for Keyboard0")   
            Thread.Sleep(1000)
        Window.close window

    [<Fact>]
    member this.testAllValues() =
        let window = Window.create 800 600 "Test Window"
        let deviceContext =
            match  Devices.TryGetDeviceContext window with
            | Some context -> context
            | None -> failwith "No device context found"
        let deviceList = Devices.GetDeviceTree deviceContext //todo make this unecessary
        let mutable lastValueMap = Map.empty    
        let mutable exit = false
        while not exit do
            Window.DoEvents window    
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
                        | KeyboardValue(value) ->
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
        Window.close window
    