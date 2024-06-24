namespace AngelCodeText

open System.IO
open System.Numerics
open FSharp.Collections
open Cyotek.Drawing.BitmapFont
open ManagerRegistry
open Graphics2D
open System.Drawing
open FSGEText




[<Manager("Text renderer that uses angelcode bitmap fonts",
          supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Windows)>]
type AngelCodeTextRenderer() =
    interface ITextManager with
        member this.FontList = Directory.GetFiles("AngelcodeFonts") |> Array.toList
        member this.LoadFont window fontName =
            let bmFont = BitmapFontLoader.LoadFontFromFile(fontName)
            AngelCodeFont (window,bmFont) :> Font
        member this.CreateText text font =
            let acFont = (font :?> AngelCodeFont)
            AngelCodeText(text, acFont) :> Text    
        member this.DrawText text xform color = 
            let acText = (text :?> AngelCodeText)
            acText.Draw xform color
            ()     

and AngelCodeFont(window:Window, bmFont:BitmapFont) =
    let bitmapFont = bmFont
    let graphics = ManagerRegistry.getManager<IGraphicsManager> ()
    let pages =
        bmFont.Pages
        |> Array.fold
            (fun (pageMap: Map<int, Lazy<Image>>) page ->
                let fileStream = File.Open(page.FileName, FileMode.Open)
                let lazyImage = lazy (Window.LoadImageFromStream fileStream window)
                Map.add page.Id lazyImage pageMap)
            Map.empty
    member this.GetPage(id) = pages.[id].Force()
    member this.GetCharacter char = bitmapFont.Characters.[char]
    member this.GetKern(last, curr) =
        float32 (bitmapFont.GetKerning(last, curr))
    interface Font with
        member this.MakeText(text) = AngelCodeText(text, this) :> Text
        member val Name = bmFont.FamilyName
        member val Size = bmFont.FontSize

and AngelCodeText(text: string, font: AngelCodeFont) =
    inherit Text(text, font) 
        override this.GetText () =
            text
        override this.GetFont ()=
            font
    member this.Draw xform color =
        //let graphics = window.graphics
        text
        |> Seq.fold
            (fun (state: (Vector2 * char) ) char ->
                let pos = fst state
                let lastChar = snd state
                let acChar: Character = font.GetCharacter char
                let acImage: Image = font.GetPage(acChar.TexturePage)
                let rectPos = Point (int acChar.X, int acChar.Y)
                let rectSz = Size (int acChar.Width, int acChar.Height)
                let charImage = Window.CreateSubImage acImage (uint32 rectPos.X) (uint32 rectPos.Y)
                                    (uint32 rectSz.Width) (uint32 rectSz.Height)
                let kern = font.GetKern(lastChar, char)
                let newX = pos.X + (float32 acChar.Width) + kern
                let xlateXform = Window.CreateTranslation (Vector2(pos.X,  pos.Y+float32 acChar.YOffset))
                let totalXform = xlateXform * xform
                Window.DrawTintedImage charImage totalXform color
                (Vector2(newX, pos.Y), char)
            ) (Vector2(0f,0f),'\n')
        |> ignore
 
