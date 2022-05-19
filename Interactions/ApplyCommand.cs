using System.Data.SQLite;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DNetBotHighlight.Models;
using RestSharp;
using Serilog;

namespace DNetBotHighlight.Interactions
{
	public class ApplyCommand : InteractionModuleBase<SocketInteractionContext>
	{
		[SlashCommand("highlight-apply", "Apply for bot highlight.")]
		public async Task HighlightApply([Summary(description: "The ID of the bot you are applying for.")] string botID, [Summary(description: "A feature unique to the bot.")] string uniqueFeature, [Summary(description: "A URL for an optional image banner for the post.")] string bannerUrl = null)
		{
			var conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
			conn.Open();
			
			try
			{
				await DeferAsync(true);

				//Verify provided bot ID is valid ulong
				if (!ulong.TryParse(botID, out var id))
				{
					await FollowupAsync("Invalid bot ID.", ephemeral: true);
					return;
				}

				//Check if the bot already exists in the database
				var sql = $"SELECT COUNT(*) as Count FROM Bots WHERE BotID = {id} AND VerifiedStatus != 2";
				var command = new SQLiteCommand(sql, conn);

				int.TryParse(command.ExecuteScalar().ToString(), out var count);

				if (count > 0)
				{
					await FollowupAsync("You have already applied for this bot.", ephemeral: true);
					return;
				}

				//Check if the user already has 2 bots applied for
				sql = $"SELECT COUNT(*) as Count FROM Bots WHERE OwnerID = {Context.User.Id} AND VerifiedStatus != 2";
				command = new SQLiteCommand(sql, conn);

				int.TryParse(command.ExecuteScalar().ToString(), out var count2);

				if (count2 >= 2)
				{
					await FollowupAsync("You already have 2 bots. You can not apply for any more.", ephemeral: true);
					return;
				}

				Regex regex = new Regex(@"^https?://(?:[a-z0-9\-]+\.)+[a-z]{2,6}(?:/[^/#?]+)+\.(?:jpg|gif|png)$");

				if (bannerUrl != null && !regex.IsMatch(bannerUrl))
				{
					await FollowupAsync("You must use a valid image URL if you are providing a banner URL.", ephemeral: true);
					return;
				}
				
				//Check if the provided ID is actually a listed bot
				var client = new RestClient("https://top.gg/api");
				client.AddDefaultHeader("Authorization", "TOKEN_HERE");
				
				var stats = await client.GetJsonAsync<BotStats>("bots/" + id + "/stats");
				
				if (stats == null)
				{
					await FollowupAsync("Unable to grab stats. Please make sure your bot is listed on <https://top.gg/>", ephemeral: true);
					return;
				}

				//Verify server count
				var serverCount = stats.server_count;
				if (serverCount < 100)
				{
					await FollowupAsync("Your bot must be verified and have atleast 100 servers to apply.", ephemeral: true);
					return;
				}

				var bot = await client.GetJsonAsync<BotInfo>("bots/" + id);
				if (bot == null)
				{
					await FollowupAsync("Bot not found. Please make sure it is listed on <https://top.gg/>", ephemeral: true);
					return;
				}

				if (!bot.owners.Contains(Context.User.Id.ToString()))
				{
					await FollowupAsync("You must be the owner of the bot to apply for it.", ephemeral: true);
					return;
				}
				
				//Add bot to database
				sql = $"INSERT INTO Bots (ID, OwnerID, BotID, Avatar, TopGgUrl, BotName, BotDescription, InviteURL, ServerCount, ImageBanner, Link1, Link2, Link3, VerifiedStatus) VALUES (NULL, @botId, @botId, @avatar, @topGgUrl, @botName, @botDescription, @inviteUrl, @serverCount, @imageBanner, @link1, @link2, @link3, '0')";
				command = new SQLiteCommand(sql, conn);
				command.Parameters.AddWithValue("@ownerId", Context.User.Id);
				command.Parameters.AddWithValue("@botId", id);
				command.Parameters.AddWithValue("@avatar", bot.avatar);
				command.Parameters.AddWithValue("@topGgUrl", $"https://top.gg/bot/{id}");
				command.Parameters.AddWithValue("@botName", bot.username);
				command.Parameters.AddWithValue("@botDescription", bot.shortdesc);
				command.Parameters.AddWithValue("@inviteUrl", bot.invite);
				command.Parameters.AddWithValue("@serverCount", serverCount);
				command.Parameters.AddWithValue("@imageBanner", bannerUrl);
				command.Parameters.AddWithValue("@link1", bot.website);
				command.Parameters.AddWithValue("@link2", bot.github);
				command.Parameters.AddWithValue("@link3", bot.support);
				command.ExecuteNonQuery();

				//Send message to bot queue
				if (Context.Guild.GetChannel(966143184403062824) is not SocketTextChannel botQueue)
				{
					await FollowupAsync("Unable to add bot to the queue. Please try again later or notify a moderator.", ephemeral: true);
					return;
				}

				var builder = new EmbedBuilder()
					.WithColor(Color.Purple)
					.WithTitle($"{bot.username}")
					.WithDescription(bot.shortdesc)
					.WithThumbnailUrl($"https://images.discordapp.net/avatars/{bot.id}/{bot.avatar}.png?size=128&w=128&q=75")
					.WithImageUrl(bannerUrl)
					.WithCurrentTimestamp()
					.AddField("Servers:", $"{serverCount:N0}", true)
					.AddField("Unique Feature:", uniqueFeature, true)
					.AddField("Website Link:", bot.website ?? "None")
					.AddField("Github Link:", bot.github ?? "None")
					.AddField("Support Server:", bot.support == null ? "None" : "https://discord.gg/" + bot.support)
					.WithFooter($"Bot ID: {id} | User ID: {Context.User.Id}")
					.WithAuthor(Context.User)
					.Build();

				var comps = new ComponentBuilder()
					.WithButton("Top.GG", null, ButtonStyle.Link, url: $"https://top.gg/bot/{id}")
					.WithButton("Approve Bot", "accept_bot_" + id, ButtonStyle.Success)
					.WithButton("Deny Bot", "deny_bot_" + id, ButtonStyle.Danger)
					.Build();

				await botQueue.SendMessageAsync("", embed: builder, components: comps);

				//Send final message
				await FollowupAsync("Successfully applied for bot highlights. Please ensure your DMs are open to receive updates regarding the application.", ephemeral: true);
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
			finally
			{
				conn.Close();
			}
		}
	}
}