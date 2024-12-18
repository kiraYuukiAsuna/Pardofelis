﻿using PardofelisCore.Util;
using Python.Runtime;
using NAudio.Wave;
using Serilog;
using System.Runtime.Intrinsics.Arm;
using PardofelisCore.Config;

namespace PardofelisCore.VoiceOutput;

public class VoiceOutputController
{
    private PythonInstance PythonInstance;

    private CancellationTokenSource CurrentCancellationToken;

    private static readonly Mutex GilMutex = new Mutex();

    PardofelisCore.Config.VoiceOutputConfig TTSConfig;

    public VoiceOutputController(PythonInstance pythonInstance)
    {
        PythonInstance = pythonInstance;
        CurrentCancellationToken = new CancellationTokenSource();

        var ttsConfigPath = Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/VoiceOutputConfig.json");
        TTSConfig = PardofelisCore.Config.VoiceOutputConfig.ReadConfig(ttsConfigPath);
    }

    private void PlayAudio(byte[] audioData, CancellationTokenSource cancellationToken)
    {
        using (var ms = new MemoryStream(audioData))
        try
        {
            using (var rdr = new WaveFileReader(ms))
            using (var waveOut = new WaveOutEvent())
            {
                waveOut.Init(rdr);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        waveOut.Stop();
                        break;
                    }
                    Task.Delay(100).Wait(); // 保持播放状态直到播放完成
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to play audio.");
        }
    }

    public void Speak(string text, bool saveAudioFile=false, string savePath="")
    {
        try
        {
            if (!CurrentCancellationToken.IsCancellationRequested)
            {
                CurrentCancellationToken.Cancel();
                CurrentCancellationToken = new CancellationTokenSource();
            }

            Task.Run(() =>
            {
                CancellationTokenSource ThisCancellationToken = CurrentCancellationToken;
                List<string> texts = SentenceSplitter.SentenceSplit(text, 50);
                List<bool> finished = new List<bool>();
                foreach (var text in texts)
                {
                    finished.Add(false);
                }
                int index = 0;
                foreach (var text in texts)
                {
                    if (ThisCancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    byte[] audioData = new byte[0];

                    GilMutex.WaitOne();
                    try
                    {
                        using (Py.GIL())
                        {
                            Log.Information("GIL Acquired");

                            if (ThisCancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                            PyObject[] arg = new PyObject[] { new PyString(text), new PyInt(TTSConfig.Id), new PyFloat(TTSConfig.Length), new PyFloat(TTSConfig.SdpRatio), new PyFloat(TTSConfig.Noise), new PyFloat(TTSConfig.Noisew) };

                            var result = PythonInstance.InvokeMethod("InferenceAudioFromText", arg);

                            if (result == null)
                            {
                                return;
                            }

                            dynamic bytesIO = result;
                            PyObject readMethod = bytesIO.GetAttr("read");
                            PyObject bytes = readMethod.Invoke();
                            audioData = bytes.As<byte[]>();
                        }
                        Log.Information("GIL Released");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to infer audio.");
                    }
                    finally
                    {
                        GilMutex.ReleaseMutex();
                    }

                    if (saveAudioFile)
                    {
                        File.WriteAllBytes(savePath + $"_{index}.wav", audioData);
                    }

                    Task.Run(() =>
                    {
                        int curIndex = index;
                        index++;

                        while (!finished[curIndex])
                        {
                            if (ThisCancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                            bool isPlaying = true;
                            for (int i = 0; i < curIndex; i++)
                            {
                                if (!finished[i])
                                {
                                    isPlaying = false;
                                }
                            }
                            if (isPlaying)
                            {
                                PlayAudio(audioData, ThisCancellationToken);
                                finished[curIndex] = true;
                                if (curIndex == finished.Count - 1)
                                {
                                    ThisCancellationToken.Cancel();
                                    ThisCancellationToken = new CancellationTokenSource();
                                    CurrentCancellationToken = new CancellationTokenSource();
                                }
                            }
                        }
                    });
                }
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Python Invoke Method Error.");
            throw;
        }
    }
}