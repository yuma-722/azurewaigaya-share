#pragma warning disable SKEXP0001 // 種類は、評価の目的でのみ提供されています。将来の更新で変更または削除されることがあります。続行するには、この診断を非表示にします。
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

var configuration=new ConfigurationBuilder().AddJsonFile("secrets.json").Build();

var endpoint=configuration["OpenAI:Endpoint"];
var deploymentName = configuration["OpenAI:DeploymentName"];
var apiKey = configuration["OpenAI:ApiKey"];

var kernel = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey,"gpt4o").
    AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey, "gpt4").
    Build();

#pragma warning disable SKEXP0110 // 種類は、評価の目的でのみ提供されています。将来の更新で変更または削除されることがあります。続行するには、この診断を非表示にします。
var yukichiAgent = new ChatCompletionAgent
{
    Name = "Yukichi",
    Description = "ゆきち",
    Arguments=new KernelArguments(new AzureOpenAIPromptExecutionSettings { ServiceId="gpt4o",Temperature=1}),
    Instructions = """
       	あなたはユーザー（姉）の5歳の妹「ゆきち」としてロールプレイを行うひらがなで返事するChatBotです。
        ※好きに変えてください
    """,
    Kernel = kernel
};
var yuzuAgent=new ChatCompletionAgent
{
    Name = "Yuzu",
    Description = "ゆず",
    Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings { ServiceId = "gpt4o", Temperature = 1 }),
    Instructions = """
           	あなたはユーザー（姉）の5歳の妹「ゆず」としてロールプレイを行うChatBotで、ひらがなだけで返答します。
        ※好きに変えてください
    """,
    Kernel = kernel
};

var judgeAgent = new ChatCompletionAgent
{
    Name = "Judge",
    Description = "Judge",
    Instructions = """
           	あなたはユーザー（姉）の5歳の双子の妹「ゆきち」と「ゆず」のどちらがユーザー（姉）と遊ぶのにふさわしいか、実況と審判を行うChatBotです。
        ※好きに変えてください
    """,
    Kernel = kernel
};


var agentChat = new AgentGroupChat(yukichiAgent, yuzuAgent, judgeAgent)
{
    ExecutionSettings = new Microsoft.SemanticKernel.Agents.Chat.AgentGroupChatSettings
    {
        SelectionStrategy = new SequentialSelectionStrategy()
    }
};
agentChat.ExecutionSettings.TerminationStrategy.MaximumIterations = 3;
agentChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "今日はおねえちゃんと誰が何をして遊ぶのか決めてほしいな。どんな遊びがいいかそれぞれ提案して話し合ってみて     ※好きに変えてください"));

while(true){

    await foreach (var message in agentChat.InvokeAsync())
    {
        Console.WriteLine($"{message.AuthorName}:{message.Content}");
    }
    var userMessage=Console.ReadLine();
    agentChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userMessage));
}