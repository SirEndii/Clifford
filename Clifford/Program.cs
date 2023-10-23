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

        private static string _basePath   = AppDomain.CurrentDomain.BaseDirectory;
        private static string _configPath = Path.Combine(_basePath, "config.json");
        
        public static readonly Config Configuration = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configPath));
        
        private DiscordSocketClient _discordClient;
        public static GitHubClient GithubClient;

        public async Task MainAsync()
        {
            string token = Configuration.discordToken;
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMessages
            };
            _discordClient          =  new DiscordSocketClient(config);
            _discordClient.Log      += Log;
            _discordClient.Ready    += Client_Ready;
            await _discordClient.SetActivityAsync(new Game("Boop", ActivityType.Listening, ActivityProperties.None, "Bop"));
            
            GithubClient = new GitHubClient(new ProductHeaderValue("clifford-discord"));
            GithubClient.Credentials = new Credentials(Configuration.githubToken);

            await _discordClient.LoginAsync(TokenType.Bot, token);
            await _discordClient.StartAsync();

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
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
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

            await _discordClient.CreateGlobalApplicationCommandAsync(reportCommand.Build());
            ReportCommand reportCmd = new ReportCommand(_discordClient);
            _discordClient.SlashCommandExecuted += reportCmd.SlashCommandHandler;
            _discordClient.ModalSubmitted += reportCmd.Submitted;
            _discordClient.ButtonExecuted += reportCmd.ButtonSubmitted;
            
        }

        public static Task Main(string[] args) => new Program().MainAsync();
        
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}