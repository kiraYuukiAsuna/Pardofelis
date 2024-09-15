using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Util
{
    public enum RunningStatus
    {
        Stopped,
        OnLaunchVoiceInputModel,
        OnLaunchEmbeddingModel,
        OnLaunchLlmModel,
        OnLaunchVoiceOutputModel,
        Running
    }

    public enum SystemStatus
    {
        Idle,
        OnVoiceInput,
        OnTextInput,
        OnApiInput,
        OnLlmInference,
        OnVoiceOutput
    }

    public class GlobalStatus
    {
        public static RunningStatus CurrentRunningStatus = RunningStatus.Stopped;
        public static SystemStatus CurrentStatus = SystemStatus.Idle;



    }
}
