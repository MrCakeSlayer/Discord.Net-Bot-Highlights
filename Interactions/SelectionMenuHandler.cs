using System.Data.SQLite;
using Discord;
using Discord.Interactions;
using Serilog;

namespace DNetBotHighlight.Interactions
{
	public class SelectionMenuHandler : InteractionModuleBase<SocketInteractionContext>
	{
		[ComponentInteraction("select_deny_reason_*_*")]
		public async Task DenyReason(string botId, string messageId, string selectedValue)
		{
			var conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
			conn.Open();

			try
			{
				await DeferAsync();

				var reason = selectedValue switch
				{
					"reason_1" => DenialReason.Nsfw,
					"reason_2" => DenialReason.NotUnique,
					"reason_3" => DenialReason.InvalidServers,
					"reason_4" => DenialReason.NotVerified,
					"reason_5" => DenialReason.NotPublic,
					"reason_6" => DenialReason.ShortDescription,
					"reason_7" => DenialReason.InvalidOwner,
					"reason_8" => DenialReason.NotDNet,
					"reason_9" => DenialReason.Other,
					_ => DenialReason.None
				};

				//Update bot status in database
				var sql = $"UPDATE Bots SET DenialReason='{reason}', ModeratorID='{Context.User.Id}' WHERE BotID='{botId}'";
				var command = new SQLiteCommand(sql, conn);
				command.ExecuteNonQuery();

				//Update message to disable buttons + show denial reason
				ulong.TryParse(messageId, out var id);
				var message = await Context.Channel.GetMessageAsync(id) as IUserMessage;
				if (message == null)
				{
					await FollowupAsync("Unable to find original message.", ephemeral: true);
					return;
				}
				
				await message.ModifyAsync(x => x.Content = $"Denied by: {Context.User.Username} ({Context.User.Id}) - <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:R> for: `{GetDenialReasonAsText(reason)}`");

				//Notify user via DM
				sql = $"SELECT OwnerID FROM Bots WHERE BotID={botId}";
				command = new SQLiteCommand(sql, conn);
				var owner = command.ExecuteScalar();
				ulong.TryParse(owner.ToString(), out var ownerId);
				
				try
				{
					var dm = await Context.Guild.GetUser(ownerId).CreateDMChannelAsync();
					await dm.SendMessageAsync("Your bot has been denied due to the following reason: `" + GetDenialReasonAsText(reason) + "`");
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

		private string GetDenialReasonAsText(DenialReason reason)
		{
			var reasontext = reason switch
			{
				DenialReason.None => "None",
				DenialReason.Nsfw => "The bot, profile picture description, or URLs are NSFW.",
				DenialReason.NotUnique => "The bot does not contain any unique features.",
				DenialReason.InvalidServers => "Server count is either unavilable, incorrect, or too low.",
				DenialReason.NotVerified => "The bot is not verified by Discord yet.",
				DenialReason.NotPublic => "The bot is not publicly invitable.",
				DenialReason.ShortDescription => "The description is not long enough.",
				DenialReason.InvalidOwner => "The user applying is not the bot owner.",
				DenialReason.NotDNet => "The bot is not using the DNet library.",
				DenialReason.Other => "Moderator's discretion/unlisted reason.",
				_ => "Unknown"
			};

			return reasontext;
		}
	}
}