namespace CSCoreAudio

open CSCore.Codecs
open FSGEAudio
open ManagerRegistry
open CSCore
open CSCore.Codecs;
open CSCore.CoreAudioAPI;
open CSCore.SoundOut;

type SoundBuffer(stream,extension) =
   
    let _soundOut = new CSCore.SoundOut.WasapiOut() //TODO look into adapting OpenaL as a SoundOut
    let _waveSource =
        CodecFactory.Instance.GetCodec(stream,extension)
        |> fun codec -> codec.ToSampleSource().ToMono().ToWaveSource()
    do _soundOut.Initialize(_waveSource)
    member this.Play() =
        _soundOut.Play()
    member this.SetVolume(vol:float32) =
        _soundOut.Volume <- vol
        ()
        
    member this.IsPlaying() =
        _soundOut.PlaybackState = CSCore.SoundOut.PlaybackState.Playing
    member this.Close() =
        _soundOut.Dispose()
        
    member this.stop() =
        _soundOut.Stop()  
    
    interface SoundStream 

[<Manager("Silk Audio OAL", supportedSystems.Windows )>]
type CSCorePlugin()=
    interface IAudioManager with
        member this.Close(var0) =
            match var0 with
            | :? SoundBuffer as sound -> sound.Close()
            | _ -> failwith "Invalid sound type"
        member this.EnumerateOutputDevices() = failwith "todo"
        member this.IsPlaying sound =
            match sound with
            | :? SoundBuffer as sound -> sound.IsPlaying()
            | _ -> failwith "Invalid sound type"
        member this.OpenSoundStream stream format =
            SoundBuffer(stream,format.ToString()) :> SoundStream
        member this.Pause(var0) = failwith "todo"
        member this.Play sound =
            match sound with
            | :? SoundBuffer as sound -> sound.Play()
            | _ -> failwith "Invalid sound type"
            sound
            
        member this.Rewind(var0) = failwith "todo"
        member this.SetOutputDevice(var0) = failwith "todo"
        member this.SetVolume vol sound =
            match sound with
            | :? SoundBuffer as sound -> sound.SetVolume(vol)
            | _ -> failwith "Invalid sound type"
            sound
        member this.Stop(var0) =
            match var0 with
            | :? SoundBuffer as sound -> sound.stop()
            | _ -> failwith "Invalid sound type"
            var0
            
       