namespace ConsoleLogger

open Logger

type ConsoleLogger( ) =
    interface ILogger with
        member this.logMessage message = 
            System.Console.WriteLine message
       