namespace FSGEAudio

type Sound = interface end

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


type IAudioManager =
 
    abstract member EnumerateOutputDevices : unit -> (int * string) seq
    abstract member SetOutputDevice : int -> unit
    abstract member LoadSound: string -> Sound
    abstract member OpenSoundStream: string -> Sound
    abstract member Play : Sound -> Sound
    abstract member Stop : Sound -> Sound
    abstract member Pause : Sound -> Sound
    abstract member SetVolume : float32 -> Sound -> Sound
    abstract member Rewind : Sound -> Sound
    abstract member Close : Sound -> unit
    abstract member IsPlaying : Sound -> bool
    
module Audio =
    let _audioManager =
        match ManagerRegistry.getManager<IAudioManager>() with
        | Some manager -> manager
        | None -> failwith "No audio manager found"
        
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
    let EnumerateOuputDevices() =
        _audioManager.EnumerateOutputDevices()
    let SetOutputDevice deviceIndex = _audioManager.SetOutputDevice deviceIndex    

    let LoadSound path : Sound =
        _audioManager.LoadSound path
        
    let OpenSoundStream path =
        _audioManager.OpenSoundStream path
   
    let Play sound = _audioManager.Play sound
    let Stop sound = _audioManager.Stop sound
    let Pause sound = _audioManager.Pause sound
    let SetVolume volume sound = _audioManager.SetVolume volume sound
    let Rewind sound = _audioManager.Rewind sound
    let IsPlaying sound = 
        _audioManager.IsPlaying sound
    