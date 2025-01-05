// DO NOT directly refence this fromy your project
// It will mess up audio loading
using CSCore.Codecs;
using CSCore.Codecs.AAC;
using CSCore.Codecs.AIFF;
using CSCore.Codecs.DDP;
using CSCore.Codecs.FLAC;
using CSCore.Codecs.MP1;
using CSCore.Codecs.MP2;
using CSCore.Codecs.MP3;
using CSCore.Codecs.WAV;
using CSCore.Codecs.WMA;
using CSCore.MediaFoundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSCore.Code.Extensions
{
  
  /// <summary>
  ///     Creates a codec that reads from the passed in memory  stream
  ///     
  /// </summary>
  public class StreamCodecFactory
  {
    private static readonly StreamCodecFactory _instance = new StreamCodecFactory();
    private readonly Dictionary<object, CodecFactoryEntry> _codecs;

    private StreamCodecFactory()
    {
      this._codecs = new Dictionary<object, CodecFactoryEntry>();
      this.Register((object) "mp3", new CodecFactoryEntry((GetCodecAction) (s =>
      {
        try
        {
          return (IWaveSource) new DmoMp3Decoder(s);
        }
        catch (Exception ex)
        {
          if (Mp3MediafoundationDecoder.IsSupported)
            return (IWaveSource) new Mp3MediafoundationDecoder(s);
          throw;
        }
      }), new string[2]{ "mp3", "mpeg3" }));
      this.Register((object) "wave", new CodecFactoryEntry((GetCodecAction) (s =>
      {
        IWaveSource waveSource = (IWaveSource) new WaveFileReader(s);
        if (waveSource.WaveFormat.WaveFormatTag != AudioEncoding.Pcm && waveSource.WaveFormat.WaveFormatTag != AudioEncoding.IeeeFloat && waveSource.WaveFormat.WaveFormatTag != AudioEncoding.Extensible)
        {
          waveSource.Dispose();
          waveSource = (IWaveSource) new MediaFoundationDecoder(s);
        }
        return waveSource;
      }), new string[2]{ "wav", "wave" }));
      this.Register((object) "flac", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new FlacFile(s)), new string[2]
      {
        "flac",
        "fla"
      }));
      this.Register((object) "aiff", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new AiffReader(s)), new string[3]
      {
        "aiff",
        "aif",
        "aifc"
      }));
      if (AacDecoder.IsSupported)
        this.Register((object) "aac", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new AacDecoder(s)), new string[14]
        {
          "aac",
          "adt",
          "adts",
          "m2ts",
          "mp2",
          "3g2",
          "3gp2",
          "3gp",
          "3gpp",
          "m4a",
          "m4v",
          "mp4v",
          "mp4",
          "mov"
        }));
      if (WmaDecoder.IsSupported)
        this.Register((object) "wma", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new WmaDecoder(s)), new string[4]
        {
          "asf",
          "wm",
          "wmv",
          "wma"
        }));
      if (Mp1Decoder.IsSupported)
        this.Register((object) "mp1", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new Mp1Decoder(s)), new string[2]
        {
          "mp1",
          "m2ts"
        }));
      if (Mp2Decoder.IsSupported)
        this.Register((object) "mp2", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new Mp2Decoder(s)), new string[2]
        {
          "mp2",
          "m2ts"
        }));
      if (!DDPDecoder.IsSupported)
        return;
      this.Register((object) "ddp", new CodecFactoryEntry((GetCodecAction) (s => (IWaveSource) new DDPDecoder(s)), new string[14]
      {
        "mp2",
        "m2ts",
        "m4a",
        "m4v",
        "mp4v",
        "mp4",
        "mov",
        "asf",
        "wm",
        "wmv",
        "wma",
        "avi",
        "ac3",
        "ec3"
      }));
    }

    /// <summary>
    ///     Gets the default singleton instance of the <see cref="T:CSCore.Codecs.CodecFactory" /> class.
    /// </summary>
    public static StreamCodecFactory Instance => StreamCodecFactory._instance;

    /// <summary>
    ///     Gets the file filter in English. This filter can be used e.g. in combination with an OpenFileDialog.
    /// </summary>
    public static string SupportedFilesFilterEn => StreamCodecFactory.Instance.GenerateFilter();

    /// <summary>Registers a new codec.</summary>
    /// <param name="key">
    ///     The key which gets used internally to save the <paramref name="codec" /> in a
    ///     <see cref="T:System.Collections.Generic.Dictionary`2" />. This is typically the associated file extension. For example: the mp3 codec
    ///     uses the string "mp3" as its key.
    /// </param>
    /// <param name="codec"><see cref="T:CSCore.Codecs.CodecFactoryEntry" /> which provides information about the codec.</param>
    public void Register(object key, CodecFactoryEntry codec)
    {
      if (key is string str)
        key = (object) str.ToLower();
      if (this._codecs.ContainsKey(key))
        return;
      this._codecs.Add(key, codec);
    }

    /// <summary>
    ///     Returns a fully initialized <see cref="T:CSCore.IWaveSource" /> instance which is able to decode the specified file. If the
    ///     specified file can not be decoded, this method throws an <see cref="T:System.NotSupportedException" />.
    /// </summary>
    /// <param name="filename">Filename of the specified file.</param>
    /// <returns>Fully initialized <see cref="T:CSCore.IWaveSource" /> instance which is able to decode the specified file.</returns>
    /// <exception cref="T:System.NotSupportedException">The codec of the specified file is not supported.</exception>
    public IWaveSource GetCodec(Stream stream,string extension)
    {
      IWaveSource source = (IWaveSource) null;
      try
      {
        foreach (KeyValuePair<object, CodecFactoryEntry> codec in this._codecs)
        {
          try
          {
            if (codec.Value.FileExtensions.Any<string>((Func<string, bool>) (x => x.Equals(extension, StringComparison.OrdinalIgnoreCase))))
            {
              source = codec.Value.GetCodecAction(stream);
              if (source != null)
                break;
            }
          }
          catch (Exception ex)
          {
          }
        }
      }
      finally
      {
        if (source == null)
          stream.Dispose();
        else
          source = (IWaveSource) new StreamCodecFactory.DisposeFileStreamSource(source, stream);
      }

      return source ?? throw new NotSupportedException("Codec not supported: " + extension);
    }

    /// <summary>
    ///     Returns all the common file extensions of all supported codecs. Note that some of these file extensions belong to
    ///     more than one codec.
    ///     That means that it can be possible that some files with the file extension abc can be decoded but other a few files
    ///     with the file extension abc can't be decoded.
    /// </summary>
    /// <returns>Supported file extensions.</returns>
    public string[] GetSupportedFileExtensions()
    {
      List<string> stringList = new List<string>();
      foreach (CodecFactoryEntry codecFactoryEntry in this._codecs.Select<KeyValuePair<object, CodecFactoryEntry>, CodecFactoryEntry>((Func<KeyValuePair<object, CodecFactoryEntry>, CodecFactoryEntry>) (x => x.Value)))
      {
        foreach (string fileExtension in codecFactoryEntry.FileExtensions)
        {
          if (!stringList.Contains(fileExtension))
            stringList.Add(fileExtension);
        }
      }
      return stringList.ToArray();
    }

    private string GenerateFilter()
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("Supported Files|");
      stringBuilder.Append(string.Concat(((IEnumerable<string>) this.GetSupportedFileExtensions()).Select<string, string>((Func<string, string>) (x => "*." + x + ";")).ToArray<string>()));
      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      return stringBuilder.ToString();
    }


    private class DisposeFileStreamSource : WaveAggregatorBase
    {
      private Stream _stream;

      public DisposeFileStreamSource(IWaveSource source, Stream stream)
        : base(source)
      {
        this._stream = stream;
      }

      protected override void Dispose(bool disposing)
      {
        base.Dispose(disposing);
        if (this._stream == null)
          return;
        try
        {
          this._stream.Dispose();
        }
        catch (Exception ex)
        {
        }
        finally
        {
          this._stream = (Stream) null;
        }
      }
    }
  }
}
