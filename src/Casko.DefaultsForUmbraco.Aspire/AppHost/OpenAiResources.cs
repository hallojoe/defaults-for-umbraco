using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.OpenAI;

internal static class OpenAiResourceExtensions
{
    public static OpenAiResources AddOpenAiResources(this IDistributedApplicationBuilder builder)
    {
        var openAi = builder.AddOpenAI("openai");
        openAi.AddModel("chat", "gpt-4o-mini");
        openAi.AddModel("embeddings", "text-embedding-3-small");

        return new OpenAiResources(openAi);
    }
}

internal sealed record OpenAiResources(IResourceBuilder<OpenAIResource> OpenAi);
