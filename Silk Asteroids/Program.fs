// For more information see https://aka.ms/fsharp-console-apps
open Asteroids.Main
open ManagerRegistry
open ConsoleLogger
[<EntryPoint>]
let start argv =
    addManager typedefof<ConsoleLogger>
    main argv