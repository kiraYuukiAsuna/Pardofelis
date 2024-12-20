﻿using Gradio.Net;
using PardofelisCore.Config;
using PardofelisCore.Util;
using PortAudioSharp;
using Serilog;
using SherpaOnnx;
using System.Runtime.InteropServices;

namespace PardofelisCore.VoiceInput;

public delegate void VoiceInputCallback(string text);
public class VoiceInputController
{
    private readonly string ModelRootPath;
    private VoiceInputCallback Callback;

    private PortAudioSharp.Stream AudioStream;

    private readonly object _lockObject = new object();
    private bool isHotKeyPressed = false;
    
    public VoiceInputController(VoiceInputCallback callback)
    {
        ModelRootPath = CommonConfig.VoiceModelRootPath;
        Callback = callback;
    }

    private void ThreadProcess(CancellationTokenSource cancellationToken)
    {
        AudioStream.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        PortAudio.Terminate();
    }

    public ResultWrap<string> StartListening(CancellationTokenSource cancellationToken)
    {
        int sampleRate = 16000;

        OfflineRecognizerConfig config = new OfflineRecognizerConfig();
        config.ModelConfig.Provider = "cpu";
        config.ModelConfig.Paraformer.Model = Path.Join(ModelRootPath, "VoiceInput","model.int8.onnx");
        config.ModelConfig.Tokens = Path.Join(ModelRootPath, "VoiceInput", "tokens.txt");
        config.ModelConfig.Debug = 0;
        config.FeatConfig.SampleRate = sampleRate;
        config.ModelConfig.NumThreads = 2;
        OfflineRecognizer recognizer = new OfflineRecognizer(config);

        VadModelConfig vadModelConfig = new VadModelConfig();
        vadModelConfig.Provider = "cpu";
        vadModelConfig.SileroVad.Model = Path.Join(ModelRootPath, "VoiceInput", "silero_vad.onnx");
        vadModelConfig.Debug = 0;
        vadModelConfig.SampleRate = sampleRate;
        vadModelConfig.NumThreads = 2;
        vadModelConfig.SileroVad.MinSilenceDuration = 0.25f;

        int windowSize = vadModelConfig.SileroVad.WindowSize;

        VoiceActivityDetector vad = new VoiceActivityDetector(vadModelConfig, 60);

        Log.Information(PortAudio.VersionInfo.versionText);
        PortAudio.Initialize();

        Log.Information($"Number of devices: {PortAudio.DeviceCount}");
        for (int i = 0; i != PortAudio.DeviceCount; ++i)
        {
            Log.Information($" Device {i}");
            DeviceInfo deviceInfo = PortAudio.GetDeviceInfo(i);
            Log.Information($"   Name: {deviceInfo.name}");
            Log.Information($"   Max input channels: {deviceInfo.maxInputChannels}");
            Log.Information($"   Default sample rate: {deviceInfo.defaultSampleRate}");
        }

        int deviceIndex = PortAudio.DefaultInputDevice;
        if (deviceIndex == PortAudio.NoDevice)
        {
            Log.Information("No default input device found");
            return new ResultWrap<string>(false, "No default input device found");
        }

        DeviceInfo info = PortAudio.GetDeviceInfo(deviceIndex);

        Log.Information($"Use default device {deviceIndex} ({info.name})");

        StreamParameters param = new StreamParameters();
        param.device = deviceIndex;
        param.channelCount = 1;
        param.sampleFormat = SampleFormat.Float32;
        param.suggestedLatency = info.defaultLowInputLatency;
        param.hostApiSpecificStreamInfo = IntPtr.Zero;
        
        string voiceInputConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/VoiceInputConfig.json");
        var voiceInputConfig = VoiceInputConfig.ReadConfig(voiceInputConfigPath);
        
        KeyboardHookEntry.Hook.KeyDown += (sender, args) =>
        {
            if (args.Key == voiceInputConfig.HotKey)
            {
                lock (_lockObject)
                {
                    isHotKeyPressed = true;
                }
            }
        };
        KeyboardHookEntry.Hook.KeyUp += (sender, args) =>
        {
            if (args.Key == voiceInputConfig.HotKey)
            {
                lock (_lockObject)
                {
                    isHotKeyPressed = false;
                }
            }
        };
        
        float[] buffer = [];

        PortAudioSharp.Stream.Callback callback = (IntPtr input, IntPtr output,
            UInt32 frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            IntPtr userData
        ) =>
        {
            bool hotKeyPressed;
            lock (_lockObject)
            {
                hotKeyPressed = isHotKeyPressed;
            }
            
            if (voiceInputConfig.IsPressKeyReceiveAudioInputEnabled)
            {
                if (!hotKeyPressed)
                {
                    int threshold = (int)(sampleRate * 0.2); // 0.2s
                    if(buffer.Length <= threshold)
                    {
                        buffer = [];
                        return StreamCallbackResult.Continue;
                    }
                }
            }
            
            float[] samples = new float[frameCount];
            Marshal.Copy(input, samples, 0, (Int32)frameCount);

            buffer = buffer.Concat(samples).ToArray();

            try
            {
                while (buffer.Length > windowSize)
                {
                    vad.AcceptWaveform(buffer.Take(windowSize).ToArray());
                    buffer = buffer.Skip(windowSize).ToArray();
                }

                while (!vad.IsEmpty())
                {
                    SpeechSegment segment = vad.Front();

                    if (segment.Samples.Length == 0)
                    {
                        vad.Pop();
                        continue;
                    }

                    float startTime = segment.Start / (float)sampleRate;
                    float duration = segment.Samples.Length / (float)sampleRate;
                    OfflineStream s = recognizer.CreateStream();
                    s.AcceptWaveform(sampleRate, segment.Samples);

                    recognizer.Decode(s);
                    String text = s.Result.Text.Trim().ToLower();

                    if (!String.IsNullOrEmpty(text))
                    {
                        Log.Information("{0}--{1}: {2}", String.Format("{0:0.00}", startTime),
                            String.Format("{0:0.00}", startTime + duration), text);
                        Callback(text);
                    }

                    vad.Pop();
                }
            }catch(Exception e)
            {
                Log.Error(e, "Voice Input Error.");
            }

            return StreamCallbackResult.Continue;
        };

        AudioStream = new PortAudioSharp.Stream(inParams: param, outParams: null,
            sampleRate: config.FeatConfig.SampleRate,
            framesPerBuffer: (uint)(sampleRate * 0.1),
            streamFlags: StreamFlags.ClipOff,
            callback: callback,
            userData: IntPtr.Zero
        );

        Log.Information(param.ToString());
        Log.Information("Voice Input Service Start Successfully! You can speak now!");

        Thread thread = new Thread(() => ThreadProcess(cancellationToken));
        thread.Start();

        return new ResultWrap<string>(true, "Voice Input Service Start Successfully!");
    }
}


