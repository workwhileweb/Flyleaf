﻿using FlyleafLib.MediaFramework.MediaDemuxer;
using FlyleafLib.MediaFramework.MediaStream;

namespace FlyleafLib.MediaFramework.MediaDecoder;

public abstract unsafe class DecoderBase : RunThreadBase
{
    public MediaType                Type            { get; protected set; }

    public bool                     OnVideoDemuxer  => demuxer?.Type == MediaType.Video;
    public Demuxer                  Demuxer         => demuxer;
    public StreamBase               Stream          { get; protected set; }
    public AVCodecContext*          CodecCtx        => codecCtx;
    public Action<DecoderBase>      CodecChanged    { get; set; }
    public Config                   Config          { get; protected set; }
    public double                   Speed           { get => speed; set { if (Disposed) { speed = value; return; } if (speed != value) OnSpeedChanged(value); } }
    protected double speed = 1, oldSpeed = 1;
    protected virtual void OnSpeedChanged(double value) { }

    internal bool               filledFromCodec;
    protected AVFrame*          frame;
    protected AVCodecContext*   codecCtx;
    internal  object            lockCodecCtx    = new();

    protected Demuxer           demuxer;

    public DecoderBase(Config config, int uniqueId = -1) : base(uniqueId)
    {
        Config = config;

        if (this is VideoDecoder)
            Type = MediaType.Video;
        else if (this is AudioDecoder)
            Type = MediaType.Audio;
        else if (this is SubtitlesDecoder)
            Type = MediaType.Subs;
        else if (this is DataDecoder)
            Type = MediaType.Data;

        threadName = $"Decoder: {Type,5}";
    }

    public string Open(StreamBase stream)
    {
        lock (lockActions)
        {
            var prevStream = Stream;
            Dispose();
            Status = Status.Opening;
            string error = Open2(stream, prevStream);
            if (!Disposed)
                frame = av_frame_alloc();

            return error;
        }
    }
    protected string Open2(StreamBase stream, StreamBase prevStream, bool openStream = true)
    {
        string error = null;

        try
        {
            lock (stream.Demuxer.lockActions)
            {
                if (stream == null || stream.Demuxer.Interrupter.ForceInterrupt == 1 || stream.Demuxer.Disposed)
                    return "Cancelled";

                int ret = -1;
                Disposed= false;
                Stream  = stream;
                demuxer = stream.Demuxer;

                if (stream is not DataStream) // if we don't open/use a data codec context why not just push the Data Frames directly from the Demuxer? no need to have DataDecoder*
                {
                    // avcodec_find_decoder will use libdav1d which does not support hardware decoding (software fallback with openStream = false from av1 to default:libdav1d) [#340]
                    var codec = stream.CodecID == AVCodecID.Av1 && openStream && Config.Video.VideoAcceleration ? avcodec_find_decoder_by_name("av1") : avcodec_find_decoder(stream.CodecID);
                    if (codec == null)
                        return error = $"[{Type} avcodec_find_decoder] No suitable codec found";

                    codecCtx = avcodec_alloc_context3(codec); // Pass codec to use default settings
                    if (codecCtx == null)
                        return error = $"[{Type} avcodec_alloc_context3] Failed to allocate context3";

                    ret = avcodec_parameters_to_context(codecCtx, stream.AVStream->codecpar);
                    if (ret < 0)
                        return error = $"[{Type} avcodec_parameters_to_context] {FFmpegEngine.ErrorCodeToMsg(ret)} ({ret})";

                    codecCtx->pkt_timebase  = stream.AVStream->time_base;
                    codecCtx->codec_id      = codec->id; // avcodec_parameters_to_context will change this we need to set Stream's Codec Id (eg we change mp2 to mp3)

                    if (Config.Decoder.ShowCorrupted)
                        codecCtx->flags |= CodecFlags.OutputCorrupt;

                    if (Config.Decoder.LowDelay)
                        codecCtx->flags |= CodecFlags.LowDelay;

                    try { ret = Setup(codec); } catch(Exception e) { return error = $"[{Type} Setup] {e.Message}"; }
                    if (ret < 0)
                        return error = $"[{Type} Setup] {ret}";

                    var codecOpts = Config.Decoder.GetCodecOptPtr(stream.Type);
                    AVDictionary* avopt = null;
                    foreach(var optKV in codecOpts)
                        av_dict_set(&avopt, optKV.Key, optKV.Value, 0);

                    ret = avcodec_open2(codecCtx, null, avopt == null ? null : &avopt);

                    if (avopt != null)
                    {
                        if (ret >= 0)
                        {
                            AVDictionaryEntry *t = null;

                            while ((t = av_dict_get(avopt, "", t, DictReadFlags.IgnoreSuffix)) != null)
                                Log.Debug($"Ignoring codec option {Utils.BytePtrToStringUTF8(t->key)}");
                        }

                        av_dict_free(&avopt);
                    }

                    if (ret < 0)
                        return error = $"[{Type} avcodec_open2] {FFmpegEngine.ErrorCodeToMsg(ret)} ({ret})";
                }

                if (openStream)
                {
                    if (prevStream != null)
                    {
                        if (prevStream.Demuxer.Type == stream.Demuxer.Type)
                            stream.Demuxer.SwitchStream(stream);
                        else if (!prevStream.Demuxer.Disposed)
                        {
                            if (prevStream.Demuxer.Type == MediaType.Video)
                                prevStream.Demuxer.DisableStream(prevStream);
                            else if (prevStream.Demuxer.Type == MediaType.Audio || prevStream.Demuxer.Type == MediaType.Subs)
                                prevStream.Demuxer.Dispose();

                            stream.Demuxer.EnableStream(stream);
                        }
                    }
                    else
                        stream.Demuxer.EnableStream(stream);

                    Status = Status.Stopped;
                    CodecChanged?.Invoke(this);
                }

                return null;
            }
        }
        finally
        {
            if (error != null)
                Dispose(true);
        }
    }
    protected abstract int Setup(AVCodec* codec);

    public void Dispose(bool closeStream = false)
    {
        if (Disposed)
            return;

        lock (lockActions)
        {
            if (Disposed)
                return;

            Stop();
            DisposeInternal();

            if (closeStream && Stream != null && !Stream.Demuxer.Disposed)
            {
                if (Stream.Demuxer.Type == MediaType.Video)
                    Stream.Demuxer.DisableStream(Stream);
                else
                    Stream.Demuxer.Dispose();
            }

            if (frame != null)
                fixed (AVFrame** ptr = &frame)
                    av_frame_free(ptr);

            if (codecCtx != null)
                fixed (AVCodecContext** ptr = &codecCtx)
                    avcodec_free_context(ptr);

            codecCtx        = null;
            demuxer         = null;
            Stream          = null;
            Status          = Status.Stopped;
            Disposed        = true;
            Log.Info("Disposed");
        }
    }
    protected abstract void DisposeInternal();
}
