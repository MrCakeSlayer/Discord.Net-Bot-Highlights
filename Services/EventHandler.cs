using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Timers;
using Discord;
using Discord.WebSocket;
using DNetBotHighlight.Models;
using Serilog;
using Timer = System.Timers.Timer;

namespace DNetBotHighlight.Services;

public class RotationHandler
{
	private readonly DiscordSocketClient _client;
	
	private const ulong DailyMessageId = 966175717488476170;
	private const ulong WeeklyMessageId = 966175716779626496;
	private readonly BackgroundWorker _dailyWorker;
	private readonly BackgroundWorker _weeklyWorker;
	
	public RotationHandler(DiscordSocketClient discord)
	{
		_client = discord;

		_dailyWorker = new BackgroundWorker();
		_dailyWorker.DoWork += UpdateDailyHighlight;
		var dailyTimer = new Timer(30000);
		//var dailyTimer = new Timer(86400000);
		dailyTimer.Elapsed += dailyTimer_Elapsed;
		dailyTimer.Start();

		_weeklyWorker = new BackgroundWorker();
		_weeklyWorker.DoWork += UpdateWeeklyHighlight;
		var weeklyTimer = new Timer(30000);
		//var weeklyTimer = new Timer(604800000);
		weeklyTimer.Elapsed += weeklyTimer_Elapsed;
		weeklyTimer.Start();
	}

	private void dailyTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (!_dailyWorker.IsBusy) _dailyWorker.RunWorkerAsync();
	}

	private async void UpdateDailyHighlight(object? sender, DoWorkEventArgs e)
	{
		var conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
		conn.Open();

		try
		{
			Log.Debug("Updating daily highlight");
			var message = await _client.GetGuild(848176216011046962).GetTextChannel(966174454801657877).GetMessageAsync(DailyMessageId) as IUserMessage;

			if (message == null) return;

			//Get all approved bots
			var botData = "SELECT * FROM Bots WHERE VerifiedStatus = 1";
			await using var reader = new SQLiteCommand(botData, conn).ExecuteReader();
			List<DBBotInfo> bots = new List<DBBotInfo>();
			while (reader.Read())
			{
				ulong.TryParse(reader.GetDecimal(1).ToString(CultureInfo.InvariantCulture), out var ownerId);
				ulong.TryParse(reader.GetDecimal(2).ToString(CultureInfo.InvariantCulture), out var botId);
				ulong.TryParse(reader["ModeratorID"]?.ToString(), out var modId);
				var bot = new DBBotInfo
				{
					ID = reader.GetInt32(0),
					OwnerID = ownerId,
					BotID = botId,
					Avatar = reader["Avatar"]?.ToString(),
					TopGgUrl = reader["TopGgUrl"]?.ToString(),
					BotName = reader["BotName"]?.ToString(),
					BotDescription = reader["BotDescription"]?.ToString(),
					InviteUrl = reader["InviteURL"]?.ToString(),
					ServerCount = reader.GetInt32(8),
					ImageBanner = reader["ImageBanner"]?.ToString(),
					Link1 = reader["Link1"]?.ToString(),
					Link2 = reader["Link2"]?.ToString(),
					Link3 = reader["Link3"]?.ToString(),
					VerifiedStatus = reader.GetInt32(13),
					ModeratorID = modId,
					DenialReason = reader["DenialReason"]?.ToString(),
				};
				bots.Add(bot);
			}
			
			//Pick a random bot
			Random rng = new Random();
			var randomBot = rng.Next(0, bots.Count);
			var selectedBot = bots[randomBot];
			
			//Get that specific bot
			EmbedBuilder builder = new EmbedBuilder()
				.WithColor(Color.Purple)
				.WithTitle(selectedBot.BotName + " | Daily Highlight")
				.WithDescription(selectedBot.BotDescription)
				.WithThumbnailUrl($"https://images.discordapp.net/avatars/{selectedBot.BotID}/{selectedBot.Avatar}.png?size=128&w=128&q=75")
				.WithImageUrl(selectedBot.ImageBanner)
				.WithCurrentTimestamp()
				.AddField("Servers:", $"{selectedBot.ServerCount:N0}", true)
				.WithFooter($"Bot ID: {selectedBot.BotID} | Owner ID: {selectedBot.OwnerID}");

			var owner = _client.GetGuild(848176216011046962).GetUser(selectedBot.OwnerID);
			if (owner != null) builder.WithAuthor(owner);

			var components = new ComponentBuilder()
				.WithButton("Invite", null, ButtonStyle.Link, url: selectedBot.InviteUrl ?? "none", disabled: selectedBot.InviteUrl == null)
				.WithButton("Website", null, ButtonStyle.Link, url: selectedBot.Link1 ?? "none", disabled: selectedBot.Link2 == null)
				.WithButton("Github", null, ButtonStyle.Link, url: selectedBot.Link2 ?? "none", disabled: selectedBot.Link2 == null)
				.WithButton("Support", null, ButtonStyle.Link, url: "https://discord.gg/" + selectedBot.Link3, disabled: selectedBot.Link3 == null)
				.Build();

			await message.ModifyAsync(x =>
			{
				x.Embed = builder.Build();
				x.Components = components;
			});
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

	private void weeklyTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (!_weeklyWorker.IsBusy) _weeklyWorker.RunWorkerAsync();
	}
	
	private async void UpdateWeeklyHighlight(object? sender, DoWorkEventArgs e)
	{
		var conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
		conn.Open();

		try
		{
			Log.Debug("Updating weekly highlight");
			var message = await _client.GetGuild(848176216011046962).GetTextChannel(966174454801657877).GetMessageAsync(WeeklyMessageId) as IUserMessage;

			if (message == null) return;

			//Get all approved bots
			var botData = "SELECT * FROM Bots WHERE VerifiedStatus = 1";
			await using var reader = new SQLiteCommand(botData, conn).ExecuteReader();
			List<DBBotInfo> bots = new List<DBBotInfo>();
			while (reader.Read())
			{
				ulong.TryParse(reader.GetDecimal(1).ToString(CultureInfo.InvariantCulture), out var ownerId);
				ulong.TryParse(reader.GetDecimal(2).ToString(CultureInfo.InvariantCulture), out var botId);
				ulong.TryParse(reader["ModeratorID"]?.ToString(), out var modId);
				var bot = new DBBotInfo
				{
					ID = reader.GetInt32(0),
					OwnerID = ownerId,
					BotID = botId,
					Avatar = reader["Avatar"]?.ToString(),
					TopGgUrl = reader["TopGgUrl"]?.ToString(),
					BotName = reader["BotName"]?.ToString(),
					BotDescription = reader["BotDescription"]?.ToString(),
					InviteUrl = reader["InviteURL"]?.ToString(),
					ServerCount = reader.GetInt32(8),
					ImageBanner = reader["ImageBanner"]?.ToString(),
					Link1 = reader["Link1"]?.ToString(),
					Link2 = reader["Link2"]?.ToString(),
					Link3 = reader["Link3"]?.ToString(),
					VerifiedStatus = reader.GetInt32(13),
					ModeratorID = modId,
					DenialReason = reader["DenialReason"]?.ToString(),
				};
				bots.Add(bot);
			}
			
			//Pick a random bot
			Random rng = new Random();
			var randomBot = rng.Next(0, bots.Count);
			var selectedBot = bots[randomBot];
			
			//Get that specific bot
			EmbedBuilder builder = new EmbedBuilder()
				.WithColor(Color.Purple)
				.WithTitle(selectedBot.BotName + " | Weekly Highlight")
				.WithDescription(selectedBot.BotDescription)
				.WithThumbnailUrl($"https://images.discordapp.net/avatars/{selectedBot.BotID}/{selectedBot.Avatar}.png?size=128&w=128&q=75")
				.WithImageUrl(selectedBot.ImageBanner)
				.WithCurrentTimestamp()
				.AddField("Servers:", $"{selectedBot.ServerCount:N0}", true)
				.WithFooter($"Bot ID: {selectedBot.BotID} | Owner ID: {selectedBot.OwnerID}");

			var owner = _client.GetGuild(848176216011046962).GetUser(selectedBot.OwnerID);
			if (owner != null) builder.WithAuthor(owner);

			var components = new ComponentBuilder()
				.WithButton("Invite", null, ButtonStyle.Link, url: selectedBot.InviteUrl ?? "none", disabled: selectedBot.InviteUrl == null)
				.WithButton("Website", null, ButtonStyle.Link, url: selectedBot.Link1 ?? "none", disabled: selectedBot.Link2 == null)
				.WithButton("Github", null, ButtonStyle.Link, url: selectedBot.Link2 ?? "none", disabled: selectedBot.Link2 == null)
				.WithButton("Support", null, ButtonStyle.Link, url: "https://discord.gg/" + selectedBot.Link3, disabled: selectedBot.Link3 == null)
				.Build();

			await message.ModifyAsync(x =>
			{
				x.Embed = builder.Build();
				x.Components = components;
			});
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