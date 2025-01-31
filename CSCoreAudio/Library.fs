namespace CSCoreAudio

open CSCore.Codecs
open FSGEAudio
open ManagerRegistry
open CSCore
open CSCore.Codecs;
open CSCore.CoreAudioAPI;
open CSCore.SoundOut;

//This file is a simple implementation of the Audio interface using CSCore
//CSCore is a .NET audio library that provides a simple interface for playing audio

// SoundBuffer is a simple implementation of the SoundStream interface
// It is what is returned by the OpenSoundStream function in the
//  AudioManager interface
// It uses CSCore to play audio from a stream
// It is a simple wrapper around CSCore's SoundOut class
type SoundBuffer(stream,extension) =
   
    //  this is a private binding to a .NET WasapiOut object
    //  WasapiOut is a class in CSCore that plays audio
    let _soundOut = new CSCore.SoundOut.WasapiOut() //TODO look into adapting OpenaL as a SoundOut
    // this is a private binding to a .NET WaveSource object
    // WaveSource is a class in CSCore that reads audio from a stream
    let _waveSource =
        CodecFactory.Instance.GetCodec(stream,extension)
        |> fun codec -> codec.ToSampleSource().ToMono().ToWaveSource()
    // this line performs initialization on the _soudOut object
    // it is run when ths SoundBuffer object is created
    // In order to have code run when an object is created in F#
    // you use the do keyword    
    do _soundOut.Initialize(_waveSource)
    
    // this plays the audio getting its data from the _waveSource object
    member this.Play() =
        _soundOut.Play()
    // this sets the output volume of this SoundBuffer
    // 0 is silent, 1 is full volume
    member this.SetVolume(vol:float32) =
        _soundOut.Volume <- vol
        ()
        
    // This method returns true if the sound is currently playing
    // otherwise it returns false
    member this.IsPlaying() =
        _soundOut.PlaybackState = CSCore.SoundOut.PlaybackState.Playing
        
    // This method closes the SoundBuffer object
    // it is called when the SoundBuffer object is no longer needed
    member this.Close() =
        _soundOut.Dispose()
        
    // this prematuely stops the sound from playing
    member this.stop() =
        _soundOut.Stop()
        
    // this rewinds the sound to the beginning
    // so the sound can be played again
    member this.Rewind() =
        stream.Position <- 0L
        
    // this makes the SoundBuffer object look like
    // an opaque SoundStream object from outside of this
    // module
    interface SoundStream 

// CSCorePlugin is a simple implementation of the IAudioManager interface
// Using the .NET audio library CSCore
// It uses Wasapi as its audio rendering layer.
// WasApi is a Windows API for audio rendering so this
// plugin will only work on Windows
[<Manager("Silk Audio WasApi", supportedSystems.Windows )>]
type CSCorePlugin()=
    // this is the implementation of the IAudioManager interface
    interface IAudioManager with
        // This is a method that closes a sound stream
        // It uses a match expression to guard the downcast
        // to a SoundBuffer object as defined above
        member this.Close(var0) =
            match var0 with
            | :? SoundBuffer as sound -> sound.Close()
            | _ -> failwith "Invalid sound type"
        // currently unimplemented
        member this.EnumerateOutputDevices() = failwith "todo"
        // This method returns true if the sound stream is
        // currently playing, otherwise false
        // It uses a match expression to guard the downcast
        // to a SoundBuffer object as defined above
        member this.IsPlaying sound =
            match sound with
            | :? SoundBuffer as sound -> sound.IsPlaying()
            | _ -> failwith "Invalid sound type"
        // This method takes a .NET input stream and the AudioFileFormat
        // It creates a SoundBuffer object from the stream and then
        // upcasts it to a SoundStream object which is returned
        member this.OpenSoundStream stream format =
            SoundBuffer(stream,format.ToString()) :> SoundStream
        // currently unimplemented
        member this.Pause(var0) = failwith "todo"
        // This method plays the sound stream
        // It uses a match expression to guard the downcast
        // to a SoundBuffer object as defined above
        member this.Play sound =
            match sound with
            | :? SoundBuffer as sound -> sound.Play()
            | _ -> failwith "Invalid sound type"
            sound
            
        // This method rewinds the sound stream to the beginning
        // It uses a match expression to guard the downcast
        // to a SoundBuffer object as defined above
        member this.Rewind(var0) =
            match var0 with
            | :? SoundBuffer as sound -> sound.Rewind()
            | _ -> failwith "Invalid sound type"
            var0
        // currently unimplemented
        // right now the sound always plays through the default
        // output device
        member this.SetOutputDevice(var0) = failwith "todo"
        // This method sets the volume to play the sound stream at
        // 0 is silent, 1 is full volume
        // It uses a match expression to guard the downcast
        // to a SoundBuffer object as defined above
        member this.SetVolume vol sound =
            match sound with
            | :? SoundBuffer as sound -> sound.SetVolume(vol)
            | _ -> failwith "Invalid sound type"
            sound
        // This method stops playing the sound stream
        // It uses a match expression to guard the downcast
        // to a SoundBuffer object as defined above
        member this.Stop(var0) =
            match var0 with
            | :? SoundBuffer as sound -> sound.stop()
            | _ -> failwith "Invalid sound type"
            var0
            
       