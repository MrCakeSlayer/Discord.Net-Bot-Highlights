using System.Data.SQLite;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace DNetBotHighlight.Interactions
{
	public class ButtonHandler : InteractionModuleBase<SocketInteractionContext>
	{
		[ComponentInteraction("accept_bot_*")]
		public async Task AcceptBot(string id)
		{
			var conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
			conn.Open();
			
			try
			{
				await DeferAsync();
				
				//Check if user is an admin
				var adminRole = Context.Guild.GetRole(848176717196820532);
				if (!((SocketGuildUser)Context.User).Roles.Contains(adminRole))
				{
					await FollowupAsync("Only admins can approve bots.", ephemeral: true);
					return;
				}
				
				//Modify message in queue to disable buttons
				var comps = new ComponentBuilder()
					.WithButton("Top.GG", null, ButtonStyle.Link, url: $"https://top.gg/bot/{id}")
					.WithButton("Approved", "approved", ButtonStyle.Success, disabled: true)
					.Build();

				await Context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					x.Components = comps;
					x.Content = $"Approved by: {Context.User.Username} ({Context.User.Id}) - <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:R>";
				});
				
				//Update bot status in database
				var sql = $"UPDATE Bots SET VerifiedStatus=1 WHERE BotID='{id}'";
				var command = new SQLiteCommand(sql, conn);
				command.ExecuteNonQuery();
				
				//Notify moderator
				await FollowupAsync("Successfully accepted the bot and added it to the list.", ephemeral: true);
				
				//Send DM to user
				sql = $"SELECT OwnerID FROM Bots WHERE BotID={id}";
				command = new SQLiteCommand(sql, conn);
				var owner = command.ExecuteScalar();
				ulong.TryParse(owner.ToString(), out var ownerId);
				try
				{
					var dm = await Context.Guild.GetUser(ownerId).CreateDMChannelAsync();
					await dm.SendMessageAsync("Your bot has been accepted and added to the list.");
				}
				catch (Exception)
				{
					Log.Debug("Failed to send DM to user. User ID: {Id}", ownerId);
				}
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
		
		[ComponentInteraction("deny_bot_*")]
		public async Task DenyBot(string id)
		{
			var conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
			conn.Open();
			
			try
			{
				await DeferAsync();
				
				//Check if user is an admin
				var adminRole = Context.Guild.GetRole(848176717196820532);
				if (!((SocketGuildUser)Context.User).Roles.Contains(adminRole))
				{
					await FollowupAsync("Only admins can deny bots.", ephemeral: true);
					return;
				}

				//Modify message in queue to disable buttons

				var originalMessage = await Context.Interaction.GetOriginalResponseAsync();
	
				var comps = new ComponentBuilder()
					.WithButton("Top.GG", null, ButtonStyle.Link, url: $"https://top.gg/bot/{id}")
					.WithButton("Denied", "denied", ButtonStyle.Danger, disabled: true)
					.Build();

				await originalMessage.ModifyAsync(x => x.Components = comps);
				
				//Update bot status in database
				var sql = $"UPDATE Bots SET VerifiedStatus=2 WHERE BotID='{id}'";
				var command = new SQLiteCommand(sql, conn);
				command.ExecuteNonQuery();
				
				//Notify moderator
				var comps2 = new ComponentBuilder()
					.WithSelectMenu(new SelectMenuBuilder()
							.WithCustomId($"select_deny_reason_{id}_{originalMessage.Id}")
							.WithMinValues(1)
							.WithMaxValues(1)
							.WithOptions(new List<SelectMenuOptionBuilder>
							{
								new SelectMenuOptionBuilder()
									.WithLabel("Inappropriate/NSFW")
									.WithDescription("The bot, profile picture description, or URLs are NSFW.")
									.WithValue("reason_1"),
								new SelectMenuOptionBuilder()
									.WithLabel("The bot is not unique.")
									.WithDescription("The bot does not contain any unique features.")
									.WithValue("reason_2"),
								new SelectMenuOptionBuilder()
									.WithLabel("Invalid server count")
									.WithDescription("Server count is either unavilable, incorrect, or too low.")
									.WithValue("reason_3"),
								new SelectMenuOptionBuilder()
									.WithLabel("Not Verified")
									.WithDescription("The bot is not verified by Discord yet.")
									.WithValue("reason_4"),
								new SelectMenuOptionBuilder()
									.WithLabel("Not Public")
									.WithDescription("The bot is not publicly invitable.")
									.WithValue("reason_5"),
								new SelectMenuOptionBuilder()
									.WithLabel("Short Description")
									.WithDescription("The description is not long enough.")
									.WithValue("reason_6"),
								new SelectMenuOptionBuilder()
									.WithLabel("Invalid Owner")
									.WithDescription("The user applying is not the bot owner.")
									.WithValue("reason_7"),
								new SelectMenuOptionBuilder()
									.WithLabel("Doesn't use DNet")
									.WithDescription("The bot is not using the DNet library.")
									.WithValue("reason_8"),
								new SelectMenuOptionBuilder()
									.WithLabel("Other/Misc")
									.WithDescription("Moderator's discretion/unlisted reason.")
									.WithValue("reason_9")
							})).Build();
				
				//Prompt moderator
				await FollowupAsync("Please select a reason for denial: ", ephemeral: true, components: comps2);
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