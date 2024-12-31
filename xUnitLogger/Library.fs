namespace xUnitLogger

open Logger
open Xunit.Abstractions



type xUnitLogger( ) =
    let mutable _output = None
    member this.injectOutput (output:ITestOutputHelper) = 
        _output <- Some output
    interface ILogger with
        member this.logMessage message = 
            match _output with
            | Some output ->
                output.WriteLine message
            | None -> ()
       