namespace Logger

type LoggingContext = interface end

type ILogger =
    abstract member logMessage : string -> unit

module Logger =
    let _logger =
        match ManagerRegistry.getManager<ILogger>() with
        | Some logger -> logger
        | None -> failwith "No logger found"
    
    let logMessage  message = 
        _logger.logMessage message