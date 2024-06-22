namespace xUnitLogger

open Logger
open Xunit.Abstractions


//// JW: Classes should be named in PascalCase
type xUnitLogger( ) =
    let mutable _output = None
    //// JW: Members should be named in PascalCase
    member this.injectOutput (output:ITestOutputHelper) = 
        _output <- Some output
    interface ILogger with
        member this.logMessage message = 
            match _output with
            | Some output ->
                output.WriteLine message
            | None -> failwith "No xunit output injected"
       