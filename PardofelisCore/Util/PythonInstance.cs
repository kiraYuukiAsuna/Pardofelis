using Microsoft.AspNetCore.Components.Forms;
using PardofelisCore.Config;
using Python.Runtime;
using Serilog;

namespace PardofelisCore.Util;

public class ScriptFunctioInfo
{
    public string FuntionName;
    List<KeyValuePair<string, string>> Parameters;
}

public class ScriptReturnInfo
{
    public string ReturnValue;
}

public class PythonInstance
{
    private string PythonRootPath;

    private List<string> ScriptList = new();

    private PyModule GlobalScope;

    private nint ThreadState;

    public PythonInstance(string pythonRootPath) 
    {
        PythonRootPath = pythonRootPath;
    }

    public void ShutdownPythonEngine()
    {
        using (Py.GIL())
        {
            GlobalScope.Dispose();
        }
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        if (PythonEngine.IsInitialized)
        {
            PythonEngine.Shutdown();
        }
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
    }

    private void ThreadProcess(CancellationTokenSource cancellationToken)
    {
        Runtime.PythonDLL = Path.Join(PythonRootPath, "python39.dll");
        PythonEngine.Initialize();

        ThreadState = PythonEngine.BeginAllowThreads();

        using (Py.GIL())
        {
            GlobalScope = Py.CreateScope();
            GlobalScope.SetAttr("__file__", new PyString(Path.Join(CommonConfig.PardofelisAppDataPath, @"VoiceModel\VoiceOutput\infer.py")));
            foreach (var script in ScriptList)
            {
                GlobalScope.Exec(script);
            }
            //PyObject InferenceAudioFromTextFunction = GlobalScope.GetAttr("InferenceAudioFromText");
            //PyObject[] arg = new PyObject[] { new PyString("你好！"), new PyInt(0), new PyFloat(1.0), new PyFloat(0.2), new PyFloat(0.33), new PyFloat(0.4) };
            //PyObject result = InferenceAudioFromTextFunction.Invoke(arg);

            //dynamic bytesIO = result;
            //PyObject readMethod = bytesIO.GetAttr("read");
            //PyObject bytes = readMethod.Invoke();
            //var audioData = bytes.As<byte[]>();

            //Task.Run(() => { PlayAudio(audioData); });

            //System.IO.File.WriteAllBytes("output.wav", audioData);
        }

        Log.Information("Python Thread Finished.");
    }

    public ResultWrap StartPythonEngine(CancellationTokenSource cancellationToken, List<string> scriptList)
    {
        Log.Information("Start Python Thread.");

        ScriptList = scriptList;

        Thread thread = new(() => ThreadProcess(cancellationToken));
        thread.Start();
        thread.Join();

        Log.Information("Python Thread started.");

        return new ResultWrap(true, "Python Thread started.");
    }

    public PyObject InvokeMethod(string functionName, PyObject[] arg)
    {
        PyObject result = null;
        PyObject InferenceAudioFromTextFunction = GlobalScope.GetAttr(functionName);
        result = InferenceAudioFromTextFunction.Invoke(arg);
        return result;
    }
}
