namespace CSCoreAudio

open FSGEAudio
open ManagerRegistry
open CSCore
open CSCore.Codecs;
open CSCore.CoreAudioAPI;
open CSCore.SoundOut;

type SoundBuffer(path:string) =
   
    let _soundOut = new CSCore.SoundOut.WasapiOut()
    let _waveSource =
        CodecFactory.Instance.GetCodec(path)
        |> fun codec -> codec.ToSampleSource().ToMono().ToWaveSource()
    do _soundOut.Initialize(_waveSource)
    
    member this.Play() =
        _soundOut.Play()
    member this.SetVolume(vol:float32) =
        _soundOut.Volume <- vol
        ()
        
    member this.IsPlaying() =
        _soundOut.PlaybackState = CSCore.SoundOut.PlaybackState.Playing    
    
    interface Sound 

[<Manager("Silk Audio OAL", supportedSystems.Windows )>]
type CSCorePlugin()=
    interface IAudioManager with
        member this.Close(var0) = failwith "todo"
        member this.EnumerateOutputDevices() = failwith "todo"
        member this.IsPlaying sound =
            match sound with
            | :? SoundBuffer as sound -> sound.IsPlaying()
            | _ -> failwith "Invalid sound type"
        member this.LoadSound path=
            new SoundBuffer(path) :> Sound
        member this.OpenSoundStream path = failwith "todo"
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
        member this.Stop(var0) = failwith "todo"