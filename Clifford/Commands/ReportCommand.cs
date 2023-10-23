using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using Octokit;

namespace Clifford.Commands
{
    //Not the most beautiful way, but it's sorted and enough for me!
    internal class ReportCommand
    {
        private DiscordSocketClient discordClient;

        public ReportCommand(DiscordSocketClient client)
        {
            discordClient = client;
        }

        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (!command.CommandName.Equals("report"))
                return;

            string   argument = (string)command.Data.Options.First().Value;
            string[] url      = argument.Split("/");
            if (string.IsNullOrEmpty(argument) || url.Length < 3)
                await command.RespondAsync("Please use a valid message url");

            IGuild          guild   = discordClient.GetGuild((ulong)command.GuildId);
            IMessageChannel channel = await guild.GetTextChannelAsync(Convert.ToUInt64(url.GetValue(url.Length - 2)));
            IMessage        message = await channel.GetMessageAsync(Convert.ToUInt64(url.Last()));
            Console.WriteLine(message.Content);
            Match version = Regex.Match(message.Content, "\\d+(\\.\\d+){1,2}-\\d+(\\.\\d+){1,3}\\w?");

            TextInputBuilder title = new TextInputBuilder().WithLabel("Title").WithCustomId("title")
                                                           .WithPlaceholder("Turtle crashes Server while eating a cat");
            TextInputBuilder description = new TextInputBuilder()
                                           .WithLabel("Description").WithStyle(TextInputStyle.Paragraph)
                                           .WithCustomId("description")
                                           .WithPlaceholder("When eating a cat while dancing, turtles start to crash servers.")
                                           .WithValue(message.Content);

            TextInputBuilder versionTB = new TextInputBuilder()
                                         .WithLabel("Version").WithCustomId("versions")
                                         .WithPlaceholder("1.18.2-0.7.19r")
                                         .WithValue(version.Success ? version.Value : "");

            TextInputBuilder urls = new TextInputBuilder().WithLabel("Logs and co.").WithRequired(false)
                                                          .WithCustomId("urls")
                                                          .WithPlaceholder("https://tenor.com/search/ligma-gifs");

            TextInputBuilder context = new TextInputBuilder()
                                       .WithLabel("Context").WithCustomId("context")
                                       .WithPlaceholder("A discord message url or something else")
                                       .WithValue(argument);

            TextInputBuilder reporter = new TextInputBuilder()
                                        .WithLabel("Reporter").WithCustomId("reporter")
                                        .WithPlaceholder("Who has written the text and found the issue")
                                        .WithValue(message.Author.Username);

            ModalBuilder modalBuilder = new ModalBuilder()
                                        .WithTitle("Report to Github")
                                        .WithCustomId("github_report")
                                        .AddTextInput(title)
                                        .AddTextInput(description)
                                        .AddTextInput(versionTB)
                                        .AddTextInput(urls)
                                        .AddTextInput(reporter);

            await command.RespondWithModalAsync(modalBuilder.Build());
        }

        public async Task Submitted(SocketModal modal)
        {
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            string title = components
                           .First(x => x.CustomId == "title").Value;
            string description = components
                                 .First(x => x.CustomId == "description").Value;
            string version = components
                             .First(x => x.CustomId == "versions").Value;
            string urls = components
                          .First(x => x.CustomId == "urls").Value;
            string reporter = components
                              .First(x => x.CustomId == "reporter").Value;

            string message =
                "**Description**: \n"                           +
                $"{description} \n"                             +
                " \n"                                           +
                "Issue discovered and described by reporter \n" +
                " \n"                                           +
                "**Versions:** \n"                              +
                $"{version} \n"                                 +
                " \n"                                           +
                "**Logs and co.:** \n"                          +
                (string.IsNullOrEmpty(urls) ? "Nothing" : urls) + " \n" +
                " \n"                                           +
                "**Reported by:** \n"                           +
                $"{reporter} \n"                                +
                " \n"                                           +
                "Uploaded with Clifford, mabe by [Srendi](https://github.com/Seniorendi)";

            var createIssue = new NewIssue(title)
                              {
                                  Body = message,
                              };
            var issue = await Program.GithubClient.Issue.Create("Seniorendi", "AdvancedPeripherals", createIssue);

            var embed = new EmbedBuilder
                        {
                            Title = "#" + issue.Number + " - " + issue.Title,
                        };

            embed.WithAuthor(modal.User)
                 .WithFooter(footer => footer.Text = "Uploaded with Clifford, made by srendi.")
                 .WithColor(Discord.Color.Green)
                 .WithUrl(issue.HtmlUrl)
                 .WithCurrentTimestamp();

            var builder = new ComponentBuilder()
                          .WithButton("Delete",      "delete-" + issue.Number, ButtonStyle.Danger)
                          .WithButton("Open Issue",  style: ButtonStyle.Link,  url: issue.HtmlUrl)
                          .WithButton("Close Issue", "close-" + issue.Number,  ButtonStyle.Primary);

            await modal.RespondAsync(embed: embed.Build(), components: builder.Build());
        }

        public async Task ButtonSubmitted(SocketMessageComponent component)
        {
            string[] id = component.Data.CustomId.Split("-");
            if (id[0].Equals("close"))
            {
                var issue = await Program.GithubClient.Issue.Get("Seniorendi", "Advancedperipherals",
                                                                 Convert.ToInt32(id[1]));
                await Program.GithubClient.Issue.Update("Seniorendi", "Advancedperipherals", Convert.ToInt32(id[1]),
                                                        new IssueUpdate() { State = ItemState.Closed });

                var embed = new EmbedBuilder
                            {
                                Title = "Closed issue #" + id[1],
                            };

                embed.WithAuthor(component.User)
                     .WithFooter(footer => footer.Text = "Closed with Clifford, made by srendi.")
                     .WithColor(Discord.Color.Red)
                     .WithUrl(issue.HtmlUrl)
                     .WithCurrentTimestamp();

                var builder = new ComponentBuilder()
                              .WithButton("Delete",     "delete-" + issue.Number, ButtonStyle.Danger)
                              .WithButton("Open Issue", style: ButtonStyle.Link,  url: issue.HtmlUrl)
                              .WithButton("Reopen",     "reopen-" + issue.Number, ButtonStyle.Primary);

                await component.RespondAsync(embed: embed.Build(), components: builder.Build());
            }

            if (id[0].Equals("delete"))
            {
                await component.RespondAsync("Delete not implemented (:");
            }

            if (id[0].Equals("reopen"))
            {
                var issue = await Program.GithubClient.Issue.Get("Seniorendi", "Advancedperipherals",
                                                                 Convert.ToInt32(id[1]));
                await Program.GithubClient.Issue.Update("Seniorendi", "Advancedperipherals", Convert.ToInt32(id[1]),
                                                        new IssueUpdate() { State = ItemState.Open });

                var embed = new EmbedBuilder
                            {
                                Title = "Reopened issue #" + id[1],
                            };

                embed.WithAuthor(component.User)
                     .WithFooter(footer => footer.Text = "Reopened with Clifford, made by srendi.")
                     .WithColor(Discord.Color.Green)
                     .WithUrl(issue.HtmlUrl)
                     .WithCurrentTimestamp();

                var builder = new ComponentBuilder()
                              .WithButton("Delete",      "delete-" + issue.Number, ButtonStyle.Danger)
                              .WithButton("Open Issue",  style: ButtonStyle.Link,  url: issue.HtmlUrl)
                              .WithButton("Close Issue", "close-" + issue.Number,  ButtonStyle.Primary);

                await component.RespondAsync(embed: embed.Build(), components: builder.Build());
            }
        }
    }
}