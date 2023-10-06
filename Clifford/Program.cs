using System;
using Clifford.Commands;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Octokit;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Clifford
{
    class Program
    {

        private static string basePath   = AppDomain.CurrentDomain.BaseDirectory;
        private static string configPath = Path.Combine(basePath, "config.json");
        
        public static readonly Config configuration = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
        
        private Credentials githubAuth;

        private DiscordSocketClient discordClient;
        public static GitHubClient githubClient;

        public async Task MainAsync()
        {
            string token = configuration.discordToken;
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMessages
            };
            discordClient          =  new DiscordSocketClient(config);
            discordClient.Log      += Log;
            discordClient.Ready    += Client_Ready;
            await discordClient.SetActivityAsync(new Game("Boop", ActivityType.Listening, ActivityProperties.None, "Bop"));
            
            githubClient = new GitHubClient(new ProductHeaderValue("clifford-discord"));
            githubClient.Credentials = new Credentials(configuration.githubToken);

            await discordClient.LoginAsync(TokenType.Bot, token);
            await discordClient.StartAsync();

            await Task.Delay(-1);
        }

        public async Task Client_Ready()
        {
            try
            {
                await AddCommands();
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Newtonsoft.Json.Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private async Task AddCommands()
        {
            // /report
            var reportCommand = new SlashCommandBuilder();
            reportCommand.WithName("report");
            reportCommand.WithDescription("Report a message to the repository");
            reportCommand.AddOption("message_url", ApplicationCommandOptionType.String, "Message URl which will be parsed and reported", true);

            await discordClient.CreateGlobalApplicationCommandAsync(reportCommand.Build());
            ReportCommand reportCMD = new ReportCommand(discordClient);
            discordClient.SlashCommandExecuted += reportCMD.SlashCommandHandler;
            discordClient.ModalSubmitted += reportCMD.Submitted;
            discordClient.ButtonExecuted += reportCMD.ButtonSubmitted;
            
        }

        public static Task Main(string[] args) => new Program().MainAsync();
        
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}