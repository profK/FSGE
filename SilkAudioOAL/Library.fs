namespace SilkAudioOAL
open System
open System.IO
open FSGEAudio
open ManagerRegistry
open Microsoft.FSharp.NativeInterop
open Silk.NET.OpenAL
open Silk.NET.OpenAL.Extensions

type OALSound(oalApi:AL, source:uint32) =
    member this.Play() = oalApi.SourcePlay(source)
    member this.Stop() = oalApi.SourceStop(source)
    member this.Pause() = oalApi.SourcePause(source)
    member this.Rewind() = oalApi.SourceRewind(source)
    member this.SetVolume(volume:float32) = oalApi.SetSourceProperty(source, SourceFloat.Gain, volume)
    
    interface SoundStream

[<Manager("Silk Audio OAL", supportedSystems.Windows ||| supportedSystems.Mac ||| supportedSystems.Linux)>]

type SilkOalManager() =
    let oalApi = AL.GetApi()
    let ctxtApi = ALContext.GetApi() // get default context
    let device = ctxtApi.OpenDevice("")
    
    let oalCtxt = ctxtApi.CreateContext(device,NativePtr.nullPtr)
    do
        ctxtApi.MakeContextCurrent oalCtxt
        |> function
            | true -> ()
            | false -> failwith "Failed to make context current"
    
   
    
    let loadStream (stream:Stream) =
        use memoryStream = new MemoryStream()
        stream.CopyTo(memoryStream);
        memoryStream.ToArray();
   
    interface IAudioManager with
        member this.CloseSound(var0) = failwith "todo"
        member this.CloseStream(var0) = failwith "todo"
        override this.LoadSound stream =
            let buffer = oalApi.GenBuffer()
            let source = oalApi.GenSource()
            let data = loadStream stream
            use sptr = fixed data
            let sptr' = NativeInterop.NativePtr.toVoidPtr sptr
            oalApi.BufferData(buffer, BufferFormat.Mono16, sptr', data.Length, 44100)
            oalApi.SetSourceProperty(source, SourceInteger.Buffer, buffer)
            OALSound( oalApi,source) :> Sound
        member this.OpenSoundStream(var0) = failwith "todo"
        member this.PauseSound sound=
            (sound :?> OALSound).Pause()
            sound
        member this.PauseStream(var0) = failwith "todo"
        member this.PlaySound sound =
            (sound :?> OALSound).Play()
            sound
        member this.PlayStream(var0) = failwith "todo"
        member this.RewindSound sound =
            (sound :?> OALSound).Rewind()
            sound
        member this.RewindStream(var0) = failwith "todo"
        member this.SetSoundVolume volume sound =
            (sound :?> OALSound).SetVolume volume
            sound
                                           
        member this.SetStreamVolume var0 var1 = failwith "todo"
        member this.StopSound sound =
            (sound :?> OALSound).Stop()
            sound
        member this.StopStream(var0) = failwith "todo"