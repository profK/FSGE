namespace FSGEText
open System.Numerics
open Graphics2D

type Font = interface end
type  Text  = interface end
type ITextManager =
    abstract member FontList : string list
    abstract member LoadFont : Window -> string -> Font
    abstract member CreateText : string -> Font -> Text
    abstract member DrawText : Text -> Matrix4x4 -> Color -> unit
module Text =
    //This fetches the plugin text manager
    //All plugins must be loaded before this module is used
    //or it may throw an exception
    let _textManager =
        match ManagerRegistry.getManager<ITextManager>() with
        | Some manager -> manager
        | None -> failwith "No text manager found"
    let FontList = _textManager.FontList
    let  LoadFont Window string : Font =
        _textManager.LoadFont Window string
        
    let CreateText text font : Text =
        _textManager.CreateText text font    
 
    let DrawText text xform color = 
        _textManager.DrawText text xform color
   
