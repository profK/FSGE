namespace FSGEAudio

type Sound = interface end
type SoundStream = interface end



type IAudioManager =
 
    abstract member LoadSound: System.IO.Stream -> Sound
    abstract member OpenSoundStream: System.IO.Stream -> SoundStream
    abstract member PlaySound : Sound -> Sound
    abstract member StopSound : Sound -> Sound
    abstract member PauseSound : Sound -> Sound
    abstract member SetSoundVolume : float32 -> Sound -> Sound
    abstract member RewindSound : Sound -> Sound
    abstract member PlayStream : SoundStream -> SoundStream
    abstract member StopStream : SoundStream -> SoundStream
    abstract member PauseStream : SoundStream -> SoundStream
    abstract member SetStreamVolume : float32 -> SoundStream -> SoundStream
    abstract member RewindStream : SoundStream -> SoundStream
    abstract member CloseSound : Sound -> unit
    abstract member CloseStream : SoundStream -> unit
    
module Audio =
    let _audioManager =
        match ManagerRegistry.getManager<IAudioManager>() with
        | Some manager -> manager
        | None -> failwith "No audio manager found"
    let LoadSoundFromIOStream stream : Sound =
        _audioManager.LoadSound stream
    let LoadSoundFromPath path : Sound =
        use stream = System.IO.File.OpenRead path
        LoadSoundFromIOStream stream
        
    let OpenSoundStreamFromIOStream stream : SoundStream =
        _audioManager.OpenSoundStream stream
    let OpenSoundStreamFromPath path : SoundStream =
        use stream = System.IO.File.OpenRead path
        OpenSoundStreamFromIOStream stream
    let PlaySound sound = _audioManager.PlaySound sound
    let StopSound sound = _audioManager.StopSound sound
    let PauseSound sound = _audioManager.PauseSound sound
    let SetSoundVolume volume sound = _audioManager.SetSoundVolume volume sound
    let RewindSound sound = _audioManager.RewindSound sound
    let PlayStream stream = _audioManager.PlayStream stream
    let StopStream stream = _audioManager.StopStream stream
    let PauseStream stream = _audioManager.PauseStream stream
    let SetStreamVolume volume stream = _audioManager.SetStreamVolume volume stream
    let RewindStream stream = _audioManager.RewindStream stream
    let CloseSound sound = _audioManager.CloseSound sound
    let CloseStream stream = _audioManager.CloseStream stream
    