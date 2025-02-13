namespace FSGEAudio

open System.IO

//SoundStream is an interface that represents a stream of audio data
// it is opaque to the user of the library and implemented by the audio
// manager plugin
type SoundStream = interface end

//AudioFileFormat is an enumeration of the supported audio file formats
type AudioFileFormat =
    | MP3
    | WAV
    | OGG
    | FLAC
    | AIFF
    | WMA
    | AAC
    | ALAC
    | OPUS
    | UNKNOWN

//IAudioManager is an interface that represents an audio manager plugin
// it is just an abstract desciption of the interface to a audio manager plugin
// Client applications get an audio manager instance by calling
// ManagerRegistry.getManager<IAudioManager>()
type IAudioManager =
 
    // This is a method that returns a sequence of tuples
    // the first element of the tuple is the index of the output device
    //  the second element of the tuple is the human readable name of
    // the output device
    abstract member EnumerateOutputDevices : unit -> (int * string) seq
    // This method sets which device will get the audio output
    // the deviceIndex is the index of the device in the sequence returned
    // by EnumerateOutputDevices
    abstract member SetOutputDevice : int -> unit
    /// This method takes a .NET input stream and the AudioFileFormat
    /// the data is encoded with and returns a SoundStream that can be played
    abstract member OpenSoundStream: Stream->AudioFileFormat -> SoundStream
    // This method plays the sound stream
    abstract member Play : SoundStream -> SoundStream
    // This method stops playing the sound stream
    abstract member Stop : SoundStream -> SoundStream
    // This method pauses the sound stream so it can be resumed later
    abstract member Pause : SoundStream -> SoundStream
    // This method sets the volume to play the sound stream at
    abstract member SetVolume : float32 -> SoundStream -> SoundStream
    // This method rewinds the sound stream to the beginning
    abstract member Rewind : SoundStream -> SoundStream
    // This method closes disposes of the sound stream
    abstract member Close : SoundStream -> unit
    // This method returns true if the sound stream is
    // currently playing, otherwise false
    abstract member IsPlaying : SoundStream -> bool
    
    // The audio functions are defined in a submodule
    // in this case that submodule is called Audio
    // This is a common pattern in F# libraries and
    // makes the functions all reside on Audio
    // For example: Audio.OpenSoundStream 
module Audio =
    // This is a private field that holds the audio manager plugin
    // so the interface may call it.
    // This is a bit ugly but necessary because loading the plugin at runtime
    // depends on it being a .net class. This module acts as a functional
    // interface to the loaded plugin
    let _audioManager =
        match ManagerRegistry.getManager<IAudioManager>() with
        | Some manager -> manager
        | None -> failwith "No audio manager found"
        
        // This function takes a file format extension like .wav
        // and returns the enum value that represents that file format
    let extensionToAudioFileFormat (extension:string) =
        match extension.ToLower() with
        | ".mp3" -> MP3
        | ".wav" -> WAV
        | ".ogg" -> OGG
        | ".flac" -> FLAC
        | ".aiff" -> AIFF
        | ".wma" -> WMA
        | ".aac" -> AAC
        | ".alac" -> ALAC
        | ".opus" -> OPUS
        | _ -> UNKNOWN
        
    // This function returns a sequence of tuples of the form
    // (id, name) where id is the device number of the output device
    let EnumerateOuputDevices() =
        _audioManager.EnumerateOutputDevices()
    // This function sets the output device to the device with the
    // device ID of deviceIndex
    let SetOutputDevice deviceIndex = _audioManager.SetOutputDevice deviceIndex    


    // This function takes a .NET input stream and returns a SoundStream
    // instance that can be played
    // Note that the stream doesnt hve to be a file stream, it could be
    // a memory stream if the sound data is in memory
    let OpenSoundStream stream format =
        _audioManager.OpenSoundStream stream format
    // This function plays the sound stream
    let Play sound = _audioManager.Play sound
    // This function stops playing the sound stream
    let Stop sound = _audioManager.Stop sound
    // This function pauses the play of the sound stream
    let Pause sound = _audioManager.Pause sound
    // This function sets the volume of the sound stream
    // 0 is silent and 1 is full volume
    let SetVolume volume sound = _audioManager.SetVolume volume sound
    // This function rewinds the sound stream to the beginning
    let Rewind sound = _audioManager.Rewind sound
    // This function closes the sound stream, enabling it to be garbage collected
    let Close sound = _audioManager.Close sound
    // This function returns true if the sound stream is currently playing
    // otherwise it returns false
    let IsPlaying sound = 
        _audioManager.IsPlaying sound
    