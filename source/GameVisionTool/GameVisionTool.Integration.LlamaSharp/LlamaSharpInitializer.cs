using LLama.Common;
using LLama.Native;
using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Serilog;

namespace GameVisionTool.Integration.LlamaSharp;

public static class LlamaSharpInitializer
{
    private static readonly Lock Gate = new();
    private static bool _initialized;

    public static void Initialize(bool showLogs = false)
    {
        if (_initialized) return;
        
        // Initialization logic for LlamaSharp
        // This could include loading models, setting up configurations, etc.
        // Configure logging. Change this to `true` to see log messages from llama.cpp

        lock (Gate)
        {
            if (_initialized) return;

            NativeLibraryConfig
                .All
                .WithLogCallback((level, message) =>
                {
                    if (showLogs)
                    {
                        if (level == LLamaLogLevel.Error)
                            Log.Logger.Error($"[llama {level}]: {message.TrimEnd('\n')}");
                        else
                            Log.Logger.Debug($"[llama {level}]: {message.TrimEnd('\n')}");
                    }
                });

            // Configure native library to use. This must be done before any other llama.cpp methods are called!
            NativeLibraryConfig
                .All
                .WithCuda();

            // Calling this method forces loading to occur now.
            NativeApi.llama_empty_call();

            _initialized = true;
        }
    }

    public static Task InitializeAsync(bool showLogs = false) => Task.Run(() => Initialize(showLogs));


    public static IKernelMemory InitializeKernelMemory(string modelPath)
    {
        try
        {
            // Verify model file exists
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model file not found: {modelPath}");

            InferenceParams infParams = new() { AntiPrompts = ["\n\n"] };

            LLamaSharpConfig lsConfig = new(modelPath)
            {
                DefaultInferenceParams = infParams
            };

            SearchClientConfig searchClientConfig = new()
            {
                MaxMatchesCount = 1,
                AnswerTokens = 100,
            };

            TextPartitioningOptions parseOptions = new()
            {
                MaxTokensPerParagraph = 300,
                OverlappingTokens = 30
            };

            var kernelMemory = new KernelMemoryBuilder()
                .WithLLamaSharpDefaults(lsConfig)
                .WithSearchClientConfig(searchClientConfig)
                .With(parseOptions)
                .Build();

            return kernelMemory;
        }
        catch (Exception e)
        {
            Log.Logger.Error("Error with InitializeKernelMemory: {error}", e.Message);
            throw;
        }
    }
}