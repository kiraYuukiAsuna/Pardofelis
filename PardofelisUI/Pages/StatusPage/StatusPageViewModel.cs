using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Newtonsoft.Json;
using Serilog;
using Path = System.IO.Path;
using PardofelisCore.Config;
using PardofelisCore;
using PardofelisCore.Util;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel;
using PardofelisCore.VoiceOutput;
using PardofelisCore.VoiceInput;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using File = System.IO.File;
using PardofelisCore.LlmController.OpenAiModel;
using System.Text;
using System.Collections.Concurrent;
using System.Windows.Input;
using Avalonia.Controls.Notifications;
using PardofelisCore.Api;
using PardofelisUI.Utilities;
using ReactiveUI;
using SukiUI.Dialogs;
using SukiUI.Toasts;

#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0001

namespace PardofelisUI.Pages.StatusPage;

public partial class StatusPageViewModel : PageBase
{
    public StatusPageViewModel() : base("当前运行状态", MaterialIconKind.PlayCircle, int.MinValue)
    {
        RescanConfig();
        RunningState = false;
        RunCodeProtection = false;

        RunButtonText = "点击开始运行!";

        InfoBarTitle = "当前状态：";
        InfoBarMessage = "未启动...";
        InfoBarSeverity = NotificationType.Information;
        StatusBrush = new SolidColorBrush(Color.FromRgb(33, 71, 192));

        HandleEnterKeyCommand = ReactiveCommand.Create<string>(HandleEnterKey);
    }

    public void StopIfRunning()
    {
        if (RunningState)
        {
            Run();
        }
    }

    [ObservableProperty] private AvaloniaList<string> _steps;

    [ObservableProperty] private int _stepperIndex;

    [ObservableProperty] AvaloniaList<string> _modelParameterConfigList = [];
    [ObservableProperty] private string _selectedModelParameterConfig;
    [ObservableProperty] private string _selectedCharacterPresetConfig;

    [ObservableProperty] private SolidColorBrush _statusBrush = new SolidColorBrush(Color.FromRgb(33, 71, 192));

    public ICommand HandleEnterKeyCommand { get; }

    partial void OnSelectedModelParameterConfigChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        try
        {
            var applicationConfig = ApplicationConfig.ReadConfig(m_ApplicationConfigFilePath);

            m_CurrentModelParameter =
                ModelParameterConfig.ReadConfig(m_ModelConfigRootPath + "/" + SelectedModelParameterConfig);
            ModelTypeProperty = m_CurrentModelParameter.ModelType != ModelType.Local;

            applicationConfig.LastSelectedModelConfigFileName = SelectedModelParameterConfig;
            applicationConfig.LastSelectedModelType = m_CurrentModelParameter.ModelType;
            applicationConfig.LastSelectedTextInputMode = m_CurrentModelParameter.TextInputMode;
            ApplicationConfig.WriteConfig(m_ApplicationConfigFilePath, applicationConfig);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("加载配置文件失败！没有选择有效的大语言模型配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    partial void OnSelectedCharacterPresetConfigChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        try
        {
            var applicationConfig = ApplicationConfig.ReadConfig(m_ApplicationConfigFilePath);

            m_CurrentCharacterPreset =
                CharacterPreset.ReadConfig(m_CharactPresetConfigRootPath + "/" + SelectedCharacterPresetConfig);
            TextInputModeProperty = m_CurrentModelParameter.TextInputMode != TextInputMode.Text;

            applicationConfig.LastSelectedCharacterPresetConfigFileName = SelectedCharacterPresetConfig;
            ApplicationConfig.WriteConfig(m_ApplicationConfigFilePath, applicationConfig);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("加载配置文件失败！没有选择有效的人物设定配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    [ObservableProperty] AvaloniaList<string> _characterPresetConfigList = [];

    private string m_ModelConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "ModelConfig");

    string m_CharactPresetConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "CharacterPreset");

    private string m_LocalModelRootPath = CommonConfig.LocalLlmModelRootPath;

    private string m_ApplicationConfigFilePath =
        Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/ApplicationConfig.json");


    [ObservableProperty] private bool _runningState;
    ModelParameterConfig m_CurrentModelParameter;
    CharacterPreset m_CurrentCharacterPreset;

    [ObservableProperty] private bool _modelTypeProperty;
    [ObservableProperty] private bool _textInputModeProperty;

    partial void OnModelTypePropertyChanged(bool value)
    {
        if (SelectedModelParameterConfig == "")
        {
            return;
        }

        var configFile = m_ModelConfigRootPath + "/" + SelectedModelParameterConfig;
        if (!File.Exists(configFile))
        {
            return;
        }

        try
        {
            var config = ModelParameterConfig.ReadConfig(configFile);
            config.ModelType = value ? ModelType.Online : ModelType.Local;
            ModelParameterConfig.WriteConfig(configFile, config);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("更新配置文件失败!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    partial void OnTextInputModePropertyChanged(bool value)
    {
        if (SelectedModelParameterConfig == "")
        {
            return;
        }

        var configFile = m_ModelConfigRootPath + "/" + SelectedModelParameterConfig;
        if (!File.Exists(configFile))
        {
            return;
        }

        try
        {
            var config = ModelParameterConfig.ReadConfig(configFile);
            config.TextInputMode = value ? TextInputMode.Voice : TextInputMode.Text;
            ModelParameterConfig.WriteConfig(configFile, config);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("更新配置文件失败!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    partial void OnSelectedLocalModelFileNameChanged(string value)
    {
        if (SelectedLocalModelFileName == "")
        {
            return;
        }

        var configFile = m_LocalModelRootPath + "/" + SelectedLocalModelFileName;
        if (!File.Exists(configFile))
        {
            return;
        }

        try
        {
            var applicationConfig = ApplicationConfig.ReadConfig(m_ApplicationConfigFilePath);
            applicationConfig.LastSelectedModelFileName = SelectedLocalModelFileName;
            ApplicationConfig.WriteConfig(m_ApplicationConfigFilePath, applicationConfig);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("更新配置文件失败!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    [ObservableProperty] public AvaloniaList<string> _localModelFileNames = [];
    [ObservableProperty] private string _selectedLocalModelFileName;

    [RelayCommand]
    private void RescanConfig()
    {
        m_ModelConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "ModelConfig");

        m_CharactPresetConfigRootPath = Path.Join(CommonConfig.ConfigRootPath, "CharacterPreset");

        m_LocalModelRootPath = CommonConfig.LocalLlmModelRootPath;

        m_ApplicationConfigFilePath =
            Path.Join(CommonConfig.ConfigRootPath, "ApplicationConfig/ApplicationConfig.json");

        SelectedModelParameterConfig = "";
        SelectedCharacterPresetConfig = "";
        ModelParameterConfigList.Clear();
        CharacterPresetConfigList.Clear();

        var modelConfigFileNames = ModelParameterConfig.GetAllConfigFileNames(m_ModelConfigRootPath);
        foreach (var fileName in modelConfigFileNames)
        {
            ModelParameterConfigList.Add(fileName);
        }

        var characterConfigFileNames = CharacterPreset.GetAllConfigFileNames(m_CharactPresetConfigRootPath);
        foreach (var fileName in characterConfigFileNames)
        {
            CharacterPresetConfigList.Add(fileName);
        }

        var applicationConfig = ApplicationConfig.ReadConfig(m_ApplicationConfigFilePath);

        bool foundModel = false;
        foreach (var configFileName in modelConfigFileNames)
        {
            if (applicationConfig.LastSelectedModelConfigFileName == configFileName)
            {
                SelectedModelParameterConfig = configFileName;
                foundModel = true;
                break;
            }
        }

        if (!foundModel)
        {
            if (ModelParameterConfigList.Count > 0)
            {
                SelectedModelParameterConfig = ModelParameterConfigList[0];
            }
        }

        bool foundCharacter = false;
        foreach (var configFileName in characterConfigFileNames)
        {
            if (applicationConfig.LastSelectedCharacterPresetConfigFileName == configFileName)
            {
                SelectedCharacterPresetConfig = configFileName;
                foundCharacter = true;
                break;
            }
        }

        if (!foundCharacter)
        {
            if (CharacterPresetConfigList.Count > 0)
            {
                SelectedCharacterPresetConfig = CharacterPresetConfigList[0];
            }
        }

        if (Directory.Exists(m_LocalModelRootPath))
        {
            var modelFiles = Directory.GetFiles(m_LocalModelRootPath);
            foreach (var modelFile in modelFiles)
            {
                if (Path.GetExtension(modelFile) == ".gguf")
                {
                    LocalModelFileNames.Add(Path.GetFileName(modelFile));
                }
            }

            bool foundModelFile = false;
            foreach (var fileName in modelFiles)
            {
                if (applicationConfig.LastSelectedModelFileName == fileName)
                {
                    SelectedLocalModelFileName = fileName;
                    foundModelFile = true;
                    break;
                }
            }

            if (!foundModelFile)
            {
                if (LocalModelFileNames.Count > 0)
                {
                    SelectedLocalModelFileName = LocalModelFileNames[0];
                }
            }
        }
    }

    private CancellationTokenSource CurrentCancellationToken;

    [ObservableProperty] private bool _runCodeProtection;

    private Thread idleAskThread;

    private List<Process> pluginInstances = new();


    private ISemanticTextMemory? SemanticTextMemory;
    private PythonInstance? PythonInstance;
    private VoiceInputController? VoiceInputController;
    private Thread? EmbeddingModelAndLocalLlmApiThread;
    private Kernel? SemanticKernel;
    private VoiceOutputController? VoiceOutputController;
    ChatHistory ChatMessages = new ChatHistory();

    DateTime LastInferenceTime;

    private BlockingCollection<string> _messageQueue = new BlockingCollection<string>();
    private Thread _messageProcessingThread;

    private Thread? ExternelApiServerThread;
    private ApiServer ExternelApiServer;

    private void StartMessageProcessing()
    {
        _messageQueue = new BlockingCollection<string>();
        _messageProcessingThread = new Thread(async () =>
        {
            foreach (var message in _messageQueue.GetConsumingEnumerable())
            {
                await ProcessMessage(message);
            }
        });
        _messageProcessingThread.IsBackground = true;
        _messageProcessingThread.Start();
    }

    private void StopMessageProcessing()
    {
        _messageQueue.CompleteAdding();
        _messageProcessingThread.Join();
    }

    private async Task ProcessMessage(string message)
    {
        await onLlmMessageInput(message);
    }

    public void QueueMessage(string text)
    {
        _messageQueue.Add(text);
    }

    private ChatContent ChatMessagesToChatMessage()
    {
        ChatContent chatContent = new();
        foreach (var message in ChatMessages)
        {
            switch (message.Role.Label)
            {
                case "system":
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatContent.Messages.Add(new PardofelisCore.Config.ChatMessage
                    (
                        Role.User,
                        message.Content
                    ));
                    break;
                }

                case "assistant":
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatContent.Messages.Add(new PardofelisCore.Config.ChatMessage
                    (
                        Role.Assistant,
                        message.Content
                    ));
                    break;
                }
                case "user":
                {
                    if (string.IsNullOrEmpty(message.Content))
                    {
                        break;
                    }

                    chatContent.Messages.Add(new PardofelisCore.Config.ChatMessage
                    (
                        Role.User,
                        message.Content
                    ));
                    break;
                }
            }
        }

        return chatContent;
    }

    private async Task onLlmMessageInput(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (SemanticTextMemory == null || PythonInstance == null || SemanticKernel == null ||
            VoiceOutputController == null)
        {
            return;
        }

        Log.Information("Start process llm message input.");
        
        Log.Information("Start vector search.");
        List<KeyValuePair<string, string>> lastlastvectorSearch = new();
        List<KeyValuePair<string, string>> lastvectorSearch = new();
        List<KeyValuePair<string, string>> vectorSearch = new();
        try
        {
            if(ChatMessages.Count >= 2)
            {
                var lastlastMessage = ChatMessages[ChatMessages.Count - 2].ToString();

                var lastMessage = ChatMessages[ChatMessages.Count - 1].ToString();

                lastlastvectorSearch = Rag.VectorSearch(SemanticTextMemory, "Memory", lastlastMessage).GetAwaiter().GetResult();

                lastvectorSearch = Rag.VectorSearch(SemanticTextMemory, "Memory", lastMessage).GetAwaiter().GetResult();
            }

            vectorSearch = Rag.VectorSearch(SemanticTextMemory, "Memory", text).GetAwaiter().GetResult();

        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            ShowMessageBox("查询向量数据库失败! 错误信息：" + e.Message, "确定")
                .GetAwaiter().GetResult();
        }

        Log.Information("Start build system prompt.");
        string systemPrompt =
            $"下面我们要进行角色扮演，你的名字叫{m_CurrentCharacterPreset.Name}，你的人物设定内容是：\n{m_CurrentCharacterPreset.PresetContent}\n你正在对话的人的名字是{m_CurrentCharacterPreset.YourName} ，现在是{DateTime.Now.ToString()}，你之后回复的有关时间的文本要符合常识，例如今天是9月15日，那么9月14日就要用昨天代替.\n" +
            $"当前使用向量搜索获取到的你的历史相关记忆信息如下，格式是类似于 2024/9/8 3:31:35 说话人（爱莉），对话人：（希儿）：早上好啊！ 的格式，括号里的内容是名字，你需要根据你所知道的内容去判断是谁说的话，在后续回复中你只需要回复你想说的话，不用带上（{m_CurrentCharacterPreset.Name}）等类似的表明说话人的信息，" +
            $"当然如果人物设定中出现了让你将人物心情用括号括起来的要求你可以去遵守，注意所有的回复不要带表情：\n";

        foreach (var message in lastlastvectorSearch)
        {
            systemPrompt += message.Value + message.Key + "\n";
        }
        foreach (var message in lastvectorSearch)
        {
            systemPrompt += message.Value + message.Key + "\n";
        }
        foreach (var message in vectorSearch)
        {
            systemPrompt += message.Value + message.Key + "\n";
        }

        systemPrompt +=
            "\n根据上述信息进行角色扮演，并在适当的时候调用工具，当没有显式说出调用工具的名称时不要去调用工具，调用工具的参数不能凭空捏造，在工具调用信息缺失时你会继续提问直到满足调用该工具的参数要求为止。";

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            ChatSystemPrompt = systemPrompt,
            Temperature = 0.0f,
        };

        Log.Information("\n" + text);
        ChatMessages.AddUserMessage(text);

        var chatContentBefore = ChatMessagesToChatMessage();
        File.WriteAllText(Path.Join(CommonConfig.MemoryRootPath, "ChatHistory.json"),
            JsonConvert.SerializeObject(chatContentBefore, Formatting.Indented));

        IChatCompletionService chatCompletionService = SemanticKernel.GetRequiredService<IChatCompletionService>();

        Log.Information("Start invoke chat api.");
        Task<IReadOnlyList<ChatMessageContent>>? result = null;
        try
        {
            result = chatCompletionService.GetChatMessageContentsAsync(
                ChatMessages,
                executionSettings: openAIPromptExecutionSettings,
                kernel: SemanticKernel);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            ShowMessageBox("请求大模型数据失败! 错误信息：" + e.Message, "确定")
                .GetAwaiter().GetResult();
        }

        LastInferenceTime = DateTime.Now;

        Log.Information("Assistant > ");

        string assistantMessage = "";
        string errorMessage = "";
        if (result != null)
        {
            try
            {
                assistantMessage = result.Result.FirstOrDefault()?.Content;
                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                errorMessage = e.Message;
                ShowMessageBox("请求大模型数据失败! 错误信息：" + e.Message, "确定")
                    .GetAwaiter().GetResult();
            }
        }

        if (assistantMessage == "")
        {
            assistantMessage = errorMessage;
        }

        string audioText = "";
        string processedMessage = assistantMessage;

        foreach (var pattern in m_CurrentCharacterPreset.ExceptTextRegexExpression)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                processedMessage = Regex.Replace(processedMessage, pattern, match =>
                {
                    Log.Information($"Removed: {match.Value}");
                    return string.Empty;
                });
            }

            processedMessage = processedMessage == "" ? assistantMessage : processedMessage;
        }

        audioText = processedMessage == "" ? assistantMessage : processedMessage;

        Log.Information("\n" + assistantMessage);
        ChatMessages.AddAssistantMessage(assistantMessage);

        HistoryTextBlock += m_CurrentCharacterPreset.YourName != ""
            ? ("<" + m_CurrentCharacterPreset.YourName + ">:")
            : "<(未知)>:";
        HistoryTextBlock += "\n" + text + "\n\n";

        HistoryTextBlock += m_CurrentCharacterPreset.Name != ""
            ? ("<" + m_CurrentCharacterPreset.Name + ">:")
            : "<(未知)>:";
        HistoryTextBlock += "\n" + assistantMessage + "\n\n";

        var chatContentAfter = ChatMessagesToChatMessage();
        File.WriteAllText(Path.Join(CommonConfig.MemoryRootPath, "ChatHistory.json"),
            JsonConvert.SerializeObject(chatContentAfter, Formatting.Indented));

        try
        {
            VoiceOutputController.Speak(audioText);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            ShowMessageBox("TTS语音输出失败! 错误信息：" + e.Message, "确定")
                .GetAwaiter().GetResult();
        }

        Rag.InsertTextChunkAsync(SemanticTextMemory, "Memory", text,
                $"{DateTime.Now.ToString()} [说话人({m_CurrentCharacterPreset.YourName}）] [对话人({m_CurrentCharacterPreset.Name}）]：")
            .GetAwaiter().GetResult();
        Rag.InsertTextChunkAsync(SemanticTextMemory, "Memory", assistantMessage,
                $"{DateTime.Now.ToString()} [说话人({m_CurrentCharacterPreset.Name}）] [对话人({m_CurrentCharacterPreset.YourName}）]：")
            .GetAwaiter().GetResult();
    }


    [RelayCommand]
    private void Run()
    {
        if (RunningState)
        {
            Thread stopThread = new Thread(async () =>
            {
                RunCodeProtection = true;

                // 停止运行
                StopMessageProcessing();

                if (CurrentCancellationToken != null && !CurrentCancellationToken.IsCancellationRequested)
                {
                    CurrentCancellationToken.Cancel();
                }

                foreach (var plugin in pluginInstances)
                {
                    if (plugin != null && !plugin.HasExited)
                    {
                        plugin.Kill();
                    }
                }

                // change status
                GlobalStatus.CurrentRunningStatus = RunningStatus.Stopped;
                GlobalStatus.CurrentStatus = SystemStatus.Idle;

                // clear resources
                SemanticTextMemory = null;
                if (PythonInstance != null)
                {
                    PythonInstance.ShutdownPythonEngine();
                    PythonInstance = null;
                }

                // EmbeddingModelAndLocalLlmApiThread = null;
                VoiceInputController = null;
                SemanticKernel = null;
                VoiceOutputController = null;

                // change ui
                RunButtonText = "点击开始运行!";

                InfoBarTitle = "当前状态：";
                InfoBarMessage = "未启动...";
                InfoBarSeverity = Avalonia.Controls.Notifications.NotificationType.Information;
                UpdateStatusColor(Color.FromRgb(33, 71, 192));

                StepperIndex = 0;
                Steps = new AvaloniaList<string>();

                ShowToast("停止成功!", "停止成功!", NotificationType.Success);

                // change running state
                RunningState = false;

                RunCodeProtection = false;
            });
            stopThread.Start();
            return;
        }

        if (!PardofelisAppDataPrefixChecker.Check())
        {
            return;
        }

        if (SelectedModelParameterConfig == "")
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("没有选择有效的大语言模型配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        if (SelectedModelParameterConfig == "")
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("没有选择有效的人物设定配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        try
        {
            m_CurrentModelParameter =
                ModelParameterConfig.ReadConfig(m_ModelConfigRootPath + "/" + SelectedModelParameterConfig);
            m_CurrentModelParameter.ModelType = ModelTypeProperty ? ModelType.Online : ModelType.Local;
            m_CurrentModelParameter.TextInputMode = TextInputModeProperty ? TextInputMode.Voice : TextInputMode.Text;
            ModelParameterConfig.WriteConfig(m_ModelConfigRootPath + "/" + SelectedModelParameterConfig,
                m_CurrentModelParameter);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("加载配置文件失败！没有选择有效的大语言模型配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        try
        {
            m_CurrentCharacterPreset =
                CharacterPreset.ReadConfig(m_CharactPresetConfigRootPath + "/" + SelectedCharacterPresetConfig);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("错误！")
                .WithContent("加载配置文件失败！没有选择有效的人物设定配置文件!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }

        if (m_CurrentModelParameter.ModelType == ModelType.Local)
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("本地模型支持已禁用，请使用在线模型!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
            return;
        }


        Thread startThread = new Thread(async () =>
        {
            // 开始启动
            RunCodeProtection = true;

            HistoryTextBlock = "";

            RunButtonText = "点击停止运行!";

            Log.Information("Start.");

            if (m_CurrentModelParameter.TextInputMode == TextInputMode.Text)
            {
                Steps = new AvaloniaList<string>()
                {
                    "启动向量数据库", "初始化Python环境", "加载Embedding模型", "启动外部ApiServer", "加载FunctionCall插件", "连接大语言模型", "启动TTS语音输出服务",
                };
                StepperIndex = 0;
            }
            else if (m_CurrentModelParameter.TextInputMode == TextInputMode.Voice)
            {
                Steps = new AvaloniaList<string>()
                {
                    "启动向量数据库", "初始化Python环境", "加载Embedding模型", "启动外部ApiServer", "加载FunctionCall插件", "连接大语言模型", "启动语音输入服务", "启动TTS语音输出服务",
                };
                StepperIndex = 0;
            }


            InfoBarTitle = "当前状态：正在启动！\n";
            InfoBarMessage = "当前输入模式：" +
                             (m_CurrentModelParameter.TextInputMode == TextInputMode.Text
                                 ? "(文本模式 + 自定义Api请求输入源)"
                                 : "(语音模式 + 文本模式 + 自定义Api请求输入源)\n") +
                             "当前大模型模式：" +
                             (m_CurrentModelParameter.ModelType == ModelType.Local
                                 ? "(本地大模型)"
                                 : "(在线大模型)");
            InfoBarSeverity = Avalonia.Controls.Notifications.NotificationType.Success;
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 0;


            // CancellationTokenSource
            CurrentCancellationToken = new CancellationTokenSource();


            // 启动向量数据库
            Log.Information("Start vector database.");
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 1;

            var memoryBuilder = new MemoryBuilder();
            memoryBuilder.WithOpenAITextEmbeddingGeneration("zpoint", "api key", "org id", new HttpClient()
            {
                BaseAddress = new Uri("http://127.0.0.1:14251/v1/embeddings")
            });

            IMemoryStore memoryStore =
                await SqliteMemoryStore.ConnectAsync(Path.Join(CommonConfig.MemoryRootPath, "MemStore.db"));
            memoryBuilder.WithMemoryStore(memoryStore);
            SemanticTextMemory = memoryBuilder.Build();

            // 初始化Python环境
            Log.Information("Start python environment.");
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 2;
            PythonInstance = new PythonInstance(CommonConfig.PythonRootPath);


            // 加载Embedding模型
            Log.Information("Load embedding model.");
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 3;
            if (EmbeddingModelAndLocalLlmApiThread == null)
            {
                EmbeddingModelAndLocalLlmApiThread = new Thread(() => { InvokeMethod.Run(); });
                EmbeddingModelAndLocalLlmApiThread.Start();
            }
            
            var request = new EmbeddingRequest
            {
                input = new[] { "你好！" },
                model = "zpoint",
                encoding_format = "float"
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClient client = new();
            client.Timeout = TimeSpan.FromMinutes(5);
            var response = await client.PostAsync("http://127.0.0.1:14251/v1/embeddings", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                Log.Information($"TestEmbeddingRequest Success: {responseString}");
            }
            else
            {
                Log.Information($"TestEmbeddingRequest Error: {response.StatusCode}");
                ShowMessageBox("加载Embedding模型失败! 错误信息：" + response.StatusCode.ToString() + "\n" + response.Content,
                    "确定").GetAwaiter().GetResult();
            }

            try
            {
                var queryResult = SemanticTextMemory.SearchAsync("Memory", "你好！", 1);
                await foreach (var item in queryResult)
                {
                    Log.Information("Memory search result test: {queryResult}", item.Metadata.Text);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                ShowMessageBox("启动向量数据库失败! 错误信息：" + e.Message, "确定").GetAwaiter().GetResult();
            }
            
            
            // ExternalApiServer
            Log.Information("Start external api server.");
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 4;
            if (ExternelApiServerThread == null)
            {
                ExternelApiServer = new ApiServer();
                ExternelApiServerThread = new Thread(() =>
                {
                    try
                    {
                        ExternelApiServer.StartBlocking((text) =>
                        {
                            if (!string.IsNullOrEmpty(text))
                            {
                                QueueMessage(text);
                            }
                        });
                    }catch (Exception e)
                    {
                        Log.Error(e.Message);
                        ShowMessageBox("启动外部Api服务失败! 错误信息：" + e.Message, "确定").GetAwaiter().GetResult();
                    }
                });
                ExternelApiServerThread.Start();
            }

            
            // 
            var builder = Kernel.CreateBuilder();
            builder.Services.AddLogging(c =>
                c.SetMinimumLevel(LogLevel.Trace).AddConsole()
                    .AddProvider(new FileLoggerProvider(Path.Join(CommonConfig.LogRootPath, "SemanticKernel.txt"))));
            
            
            // 加载FunctionCall插件
            Log.Information("Load function call plugin.");
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 5;
            FunctionCallPluginLoader.Clear();
            foreach (var pluginFolder in Directory.GetDirectories(CommonConfig.ToolCallPluginRootPath))
            {
                var pluginFiles = Directory.GetFiles(pluginFolder);

                foreach (var file in pluginFiles)
                {
                    if (Path.GetExtension(file) == ".dll")
                    {
                        FunctionCallPluginLoader.EnumeratePlugin(file);
                    }
                }
            }

            try
            {
                FunctionCallPluginLoader.SetCurrentPluginWorkingDirectory();
                FunctionCallPluginLoader.AddPlugin(builder);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                ShowMessageBox(e.Message, "确定").GetAwaiter().GetResult();
            }


            // 连接大语言模型
            Log.Information("Connect to large language model.");
            UpdateStatusColor(Color.FromRgb(117, 101, 192));
            StepperIndex = 6;
            if (m_CurrentModelParameter.ModelType == ModelType.Online)
            {
                if (String.IsNullOrEmpty(m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelUrl) ||
                    String.IsNullOrEmpty(m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelApiKey) ||
                    String.IsNullOrEmpty(m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelName))
                {
                    ShowMessageBox(
                            "注意！当前使用在线模式但选中的配置文件并未完整提供有关在线大模型的全部信息{在线大模型请求地址，Apikey密钥，使用的在线模型名称}，当前程序中内嵌了一个默认的在线模型（gpt-4o-mini），你可以用此进行体验但我们不保证该默认提供的Api长期有效！",
                            "我明白上述信息")
                        .GetAwaiter().GetResult();

                    builder.AddOpenAIChatCompletion("gpt-4o-mini",
                        "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc", "", "", new HttpClient()
                        {
                            BaseAddress = new Uri("https://chatapi.nloli.xyz/v1/chat/completions"),
                            Timeout = TimeSpan.FromSeconds(30)
                        });
                }
                else
                {
                    builder.AddOpenAIChatCompletion(m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelName,
                        m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelApiKey, "", "", new HttpClient()
                        {
                            BaseAddress = new Uri(m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelUrl),
                            Timeout = TimeSpan.FromSeconds(30)
                        });
                }
            }
            else if (m_CurrentModelParameter.ModelType == ModelType.Local)
            {
                // TODO: Local Llm Model
            }

            try
            {
                SemanticKernel = builder.Build();

                IChatCompletionService chatCompletionService =
                    SemanticKernel.GetRequiredService<IChatCompletionService>();
                var testLlmResult = chatCompletionService.GetChatMessageContentsAsync(
                    "你好！",
                    executionSettings: new OpenAIPromptExecutionSettings()
                    {
                        Temperature = 0.0f
                    },
                    kernel: SemanticKernel);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                ShowMessageBox("连接大语言模型失败! 错误信息：" + e.Message, "确定").GetAwaiter().GetResult();
            }


            // 启动语音输入服务
            Log.Information("Start voice input service.");
            if (m_CurrentModelParameter.TextInputMode == TextInputMode.Voice)
            {
                StepperIndex = 7;
                UpdateStatusColor(Color.FromRgb(117, 101, 192));
                VoiceInputController = new(async (string text) => { QueueMessage(text); });

                VoiceInputController.StartListening(CurrentCancellationToken);
            }


            // 启动TTS语音输出服务
            Log.Information("Start TTS voice output service.");
            if (m_CurrentModelParameter.TextInputMode == TextInputMode.Voice)
            {
                StepperIndex = 8;
                UpdateStatusColor(Color.FromRgb(117, 101, 192));
            }
            else
            {
                StepperIndex = 7;
                UpdateStatusColor(Color.FromRgb(117, 101, 192));
            }
            VoiceOutputController = new(PythonInstance);
            var voiceOutputInferenceCode =
                File.ReadAllText(Path.Join(CommonConfig.PardofelisAppDataPath, @"VoiceModel\VoiceOutput\infer.py"));
            var scripts = new List<string>();
            scripts.Add(voiceOutputInferenceCode);
            var pyRes = PythonInstance.StartPythonEngine(CurrentCancellationToken, scripts);
            if (!pyRes.Status)
            {
                Log.Error("Start TTS voice output service failed.");
                ShowMessageBox("启动TTS语音输出服务失败!" + pyRes.Message, "确定").GetAwaiter().GetResult();
            }
            

            // 启动空闲自动询问线程
            idleAskThread = new Thread(() =>
            {
                Log.Information("Idle ask thread started.");

                if (m_CurrentCharacterPreset.IdleAskMeTime >= 10)
                {
                    TimeSpan timeout = TimeSpan.FromSeconds(m_CurrentCharacterPreset.IdleAskMeTime);

                    while (!CurrentCancellationToken.IsCancellationRequested)
                    {
                        DateTime startTime = DateTime.Now;
                        startTime = LastInferenceTime;

                        // 检查是否已经超过了预定的最大执行时间
                        if (DateTime.Now - startTime > timeout)
                        {
                            Log.Information("Idle ask thread timeout. Send idle ask message.");

                            string idleAskMessage = m_CurrentCharacterPreset.IdleAskMeMessage;

                            QueueMessage(idleAskMessage);

                            LastInferenceTime = DateTime.Now;
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }

                Log.Information("Idle ask thread stopped.");
            });
            idleAskThread.Start();


            // 
            if (m_CurrentModelParameter.ModelType == ModelType.Local)
            {
                Log.Information("Local model mode enabled.");
            }
            else if (m_CurrentModelParameter.ModelType == ModelType.Online)
            {
                Log.Information("Online model mode enabled.");

                Log.Information("Online model name: {OnlineModelName}",
                    m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelName);
                Log.Information("Online model url: {OnlineModelUrl}",
                    m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelUrl);
                Log.Information("Online model api key: {OnlineModelApiKey}",
                    m_CurrentModelParameter.OnlineLlmCreateInfo.OnlineModelApiKey);
            }


            // 加载历史对话信息
            try
            {
                string historyFilePath = Path.Join(CommonConfig.MemoryRootPath, "ChatHistory.json");
                ChatContent chatContent = new();
                if (!File.Exists(historyFilePath))
                {
                    File.WriteAllText(historyFilePath,
                        JsonConvert.SerializeObject(new ChatContent(), Formatting.Indented));
                }
                else
                {
                    string historyJson = File.ReadAllText(historyFilePath);
                    var chatHistory = JsonConvert.DeserializeObject<ChatContent>(historyJson);
                    if (chatHistory.Messages != null)
                    {
                        chatContent.Messages = chatHistory.Messages;
                    }
                }

                chatContent.YourName = m_CurrentCharacterPreset.YourName;
                chatContent.CharacterName = m_CurrentCharacterPreset.Name;

                HistoryTextBlock += "当前人设信息：" + m_CurrentCharacterPreset.PresetContent + "\n";

                foreach (var message in chatContent.Messages)
                {
                    switch (message.Role)
                    {
                        case Role.System:
                        {
                            if (string.IsNullOrEmpty(message.Content))
                            {
                                break;
                            }

                            HistoryTextBlock += "<系统信息>:\n" + message.Content + "\n\n";
                            ChatMessages.AddSystemMessage(message.Content);
                            break;
                        }
                        case Role.Assistant:
                        {
                            if (string.IsNullOrEmpty(message.Content))
                            {
                                break;
                            }

                            HistoryTextBlock += m_CurrentCharacterPreset.Name != ""
                                ? ("<" + m_CurrentCharacterPreset.Name + ">:")
                                : "<(未知)>:";
                            HistoryTextBlock += "\n" + message.Content + "\n\n";
                            ChatMessages.AddAssistantMessage(message.Content);
                            break;
                        }
                        case Role.User:
                        {
                            if (string.IsNullOrEmpty(message.Content))
                            {
                                break;
                            }

                            HistoryTextBlock += m_CurrentCharacterPreset.YourName != ""
                                ? ("<" + m_CurrentCharacterPreset.YourName + ">:")
                                : "<(未知)>:";
                            HistoryTextBlock += "\n" + message.Content + "\n\n";
                            ChatMessages.AddUserMessage(message.Content);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                ShowMessageBox("加载历史聊天记录失败! 错误信息：" + e.Message, "确定");
            }


            // 启动消息处理线程
            StartMessageProcessing();


            // 加载额外插件
            foreach (var pluginName in m_CurrentCharacterPreset.EnabledPlugins)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Join(CommonConfig.PythonRootPath, "python.exe"),
                    Arguments = Path.Join(CommonConfig.PluginRootPath, pluginName, "main.py"),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.Join(CommonConfig.PluginRootPath, pluginName)
                };

                // Add the environment variable
                processStartInfo.EnvironmentVariables["QT_QPA_PLATFORM_PLUGIN_PATH"] =
                    Path.Join(CommonConfig.PythonRootPath, "Lib\\site-packages\\PyQt5\\Qt5\\plugins\\platforms");

                var process = new Process { StartInfo = processStartInfo };

                try
                {
                    if (!process.Start())
                    {
                        Log.Error("Failed to start plugin: " + pluginName);
                        ShowMessageBox("启动插件 " + pluginName + " 失败!", "确定").GetAwaiter().GetResult();
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    ShowMessageBox("启动插件 " + pluginName + " 失败! 错误信息：" + e.Message, "确定").GetAwaiter().GetResult(); ;
                    continue;
                }

                pluginInstances.Add(process);
            }


            // 启动成功
            InfoBarTitle = "当前状态：启动成功！\n";
            UpdateStatusColor(Color.FromRgb(36, 192, 81));

            ShowToast("启动成功!", "启动成功!", NotificationType.Success);

            LastInferenceTime = DateTime.Now;

            RunningState = true;

            RunCodeProtection = false;
        });
        startThread.Start();
    }

    private async Task ShowMessageBox(string message, string buttonText)
    {
        // 检查是否在UI线程上运行
        if (!Dispatcher.UIThread.CheckAccess())
        {
            // 将操作调度到UI线程
            await Dispatcher.UIThread.InvokeAsync(() => ShowMessageBox(message, buttonText));
        }
        else
        {
            // 如果已经在UI线程上，直接执行

            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent(message)
                .WithActionButton(buttonText, _ => { }, true)
                .TryShow();
        }
    }

    private async Task ShowToast(string title, string content, NotificationType toastType)
    {
        // 检查是否在UI线程上运行
        if (!Dispatcher.UIThread.CheckAccess())
        {
            // 将操作调度到UI线程
            await Dispatcher.UIThread.InvokeAsync(() => ShowToast(title, content, toastType));
        }
        else
        {
            // 如果已经在UI线程上，直接执行
            DynamicUIConfig.GlobalToastManager.CreateToast()
                .WithTitle(title)
                .WithContent(content)
                .OfType(toastType)
                .Dismiss().After(TimeSpan.FromSeconds(3))
                .Dismiss().ByClicking()
                .Queue();
        }
    }

    private async Task UpdateStatusColor(Color color)
    {
        // 检查是否在UI线程上运行
        if (!Dispatcher.UIThread.CheckAccess())
        {
            // 将操作调度到UI线程
            await Dispatcher.UIThread.InvokeAsync(() => StatusBrush = new SolidColorBrush(color));
        }
        else
        {
            // 如果已经在UI线程上，直接更新
            StatusBrush = new SolidColorBrush(color);
        }
    }

    [ObservableProperty] private string _infoBarTitle;
    [ObservableProperty] private string _infoBarMessage;
    [ObservableProperty] private Avalonia.Controls.Notifications.NotificationType _infoBarSeverity;
    [ObservableProperty] private string _runButtonText;
    [ObservableProperty] private string _historyTextBlock;
    [ObservableProperty] private string _inputTextBox;

    [RelayCommand]
    private void Infer()
    {
        if (!RunningState)
        {
            return;
        }

        if (string.IsNullOrEmpty(InputTextBox))
        {
            return;
        }

        if (SemanticTextMemory != null && PythonInstance != null && SemanticKernel != null &&
            VoiceOutputController != null)
        {
            if (m_CurrentModelParameter.ModelType == ModelType.Local ||
                m_CurrentModelParameter.ModelType == ModelType.Online)
            {
                QueueMessage(InputTextBox);
                InputTextBox = "";
            }
            else
            {
                DynamicUIConfig.GlobalDialogManager.CreateDialog()
                    .WithTitle("提示！")
                    .WithContent("配置文件中检测到未知的输入方式，请检查并修改配置文件后重新运行!")
                    .WithActionButton("确定", _ => { }, true)
                    .TryShow();
            }
        }
        else
        {
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("当前服务未启动或初始化失败，请检查服务状态后重试!")
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }


    [RelayCommand]
    private async Task ClearHistory()
    {
        ChatMessages.Clear();
        HistoryTextBlock = "";

        try
        {
            var historyFilePath = Path.Join(CommonConfig.MemoryRootPath, "ChatHistory.json");

            ChatContent chatContent = new();
            chatContent.YourName = m_CurrentCharacterPreset.YourName;
            chatContent.CharacterName = m_CurrentCharacterPreset.Name;

            File.WriteAllText(historyFilePath, JsonConvert.SerializeObject(chatContent, Formatting.Indented));

            HistoryTextBlock += "当前人设信息：" + m_CurrentCharacterPreset.PresetContent + "\n";

            foreach (var message in chatContent.Messages)
            {
                switch (message.Role)
                {
                    case Role.System:
                    {
                        HistoryTextBlock += "<系统信息>:\n" + message.Content + "\n\n";
                        ChatMessages.AddSystemMessage(message.Content);
                        break;
                    }
                    case Role.Assistant:
                    {
                        HistoryTextBlock += m_CurrentCharacterPreset.Name != ""
                            ? ("<" + m_CurrentCharacterPreset.Name + ">:")
                            : "<(未知)>:";
                        HistoryTextBlock += "\n" + message.Content + "\n\n";
                        ChatMessages.AddAssistantMessage(message.Content);
                        break;
                    }
                    case Role.User:
                    {
                        HistoryTextBlock += m_CurrentCharacterPreset.YourName != ""
                            ? ("<" + m_CurrentCharacterPreset.YourName + ">:")
                            : "<(未知)>:";
                        HistoryTextBlock += "\n" + message.Content + "\n\n";
                        ChatMessages.AddUserMessage(message.Content);
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            DynamicUIConfig.GlobalDialogManager.CreateDialog()
                .WithTitle("提示！")
                .WithContent("清空聊天记录失败! 错误信息：" + e.Message)
                .WithActionButton("确定", _ => { }, true)
                .TryShow();
        }
    }

    private void HandleEnterKey(string text)
    {
        Infer();
    }
}