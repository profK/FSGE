namespace FSGEAudio

open System.IO

type SoundStream = interface end

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
    abstract member OpenSoundStream: Stream->AudioFileFormat -> SoundStream
    abstract member Play : SoundStream -> SoundStream
    abstract member Stop : SoundStream -> SoundStream
    abstract member Pause : SoundStream -> SoundStream
    abstract member SetVolume : float32 -> SoundStream -> SoundStream
    abstract member Rewind : SoundStream -> SoundStream
    abstract member Close : SoundStream -> unit
    abstract member IsPlaying : SoundStream -> bool
    
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


    let OpenSoundStream stream format =
        _audioManager.OpenSoundStream stream format
    let Play sound = _audioManager.Play sound
    let Stop sound = _audioManager.Stop sound
    let Pause sound = _audioManager.Pause sound
    let SetVolume volume sound = _audioManager.SetVolume volume sound
    let Rewind sound = _audioManager.Rewind sound
    let Close sound = _audioManager.Close sound
    let IsPlaying sound = 
        _audioManager.IsPlaying sound
    