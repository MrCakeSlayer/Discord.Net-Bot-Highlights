using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

namespace DNetBotHighlight.Services
{
	public sealed class StartupService
	{
		private readonly DiscordSocketClient _client;
		
		public StartupService(DiscordSocketClient discord)
		{
			_client = discord;
			_client.JoinedGuild += OnJoin;
			_client.Ready += OnReadyAsync;
			_client.Log += OnLogAsync;
		}

		private static async Task OnLogAsync(LogMessage message)
		{
			try
			{
				var severity = message.Severity switch
				{
					LogSeverity.Critical => LogEventLevel.Fatal,
					LogSeverity.Error => LogEventLevel.Error,
					LogSeverity.Warning => LogEventLevel.Warning,
					LogSeverity.Info => LogEventLevel.Information,
					LogSeverity.Verbose => LogEventLevel.Verbose,
					LogSeverity.Debug => LogEventLevel.Debug,
					_ => LogEventLevel.Information
				};

				Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
				await Task.CompletedTask;
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}
		
		private async Task OnReadyAsync()
		{
			try
			{
				Log.Debug($"Running guild startup checks...");
				foreach (var guild in _client.Guilds)
				{
					if (guild.Id != 848176216011046962)
					{
						await guild.LeaveAsync();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}
		
		public async Task StartAsync()
		{
			try
			{
				//Authorize bot using bot token
				await _client.LoginAsync(TokenType.Bot, "TOKEN_HERE");

				//Start the bot
				await _client.StartAsync();

				//Set game presence
				await _client.SetGameAsync("with bot highlights. <3");

				//lol
				await _client.SetStatusAsync(UserStatus.DoNotDisturb);
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}

		private static async Task OnJoin(SocketGuild guild)
		{
			try
			{
				Log.Debug($"Joined new server. Running guild check...");
				if (guild.Id != 848176216011046962)
				{
					await guild.LeaveAsync();
				}
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}
	}
}