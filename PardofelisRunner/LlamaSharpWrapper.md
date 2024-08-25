兼容 OpenAI API: 提供与 OpenAI / Azure OpenAI 类似的 API，方便迁移和集成。
多模型支持: 支持配置和切换不同的模型，满足不同场景的需求。
流式响应: 支持流式响应，提高大型响应的处理效率。
嵌入支持: 提供文本嵌入功能，支持多种嵌入模型。
对话模版: 提供了一些常见的对话模版。
自动释放: 支持自动释放已加载模型。
函数调用: 支持函数调用。
API Key 认证: 支持 API Key 认证。
Gradio UI Demo: 提供了一个基于 Gradio.NET 的 UI 演示。

支持函数调用，目前在配置文件中提供了三个模板，已经测试了 Phi-3，Qwen2 和 Llama3.1 的函数调用效果
函数调用兼容 OpenAI 的 API，参考Pardofelis.http

API:
/v1/chat/completions: 对话完成请求
/v1/completions: 提示完成请求
/v1/embeddings: 创建嵌入
/models/info: 返回模型的基本信息
/models/config: 返回已配置的模型信息
/models/{modelId}/switch: 切换到指定模型
