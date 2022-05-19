using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DNetBotHighlight.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DNetBotHighlight
{
	public class Program
	{
		private InteractionService _commands;
		
		public static void Main()
		{
			new Program().MainAsync().GetAwaiter().GetResult();
		}

		private async Task MainAsync()
		{
			try
			{
				//Setup SeriLog
				Log.Logger = new LoggerConfiguration()
					.MinimumLevel.Verbose()
					.Enrich.FromLogContext()
					.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}")
					.WriteTo.File("logs/log-.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10, retainedFileTimeLimit: TimeSpan.FromDays(7))
					.CreateLogger();
				
				//Configure Services
				var services = new ServiceCollection();
				ConfigureServices(services);
				var provider = services.BuildServiceProvider();

				await provider.GetRequiredService<CommandHandler>().InitializeAsync();
				var client = provider.GetRequiredService<DiscordSocketClient>();
				_commands = provider.GetRequiredService<InteractionService>();
				provider.GetRequiredService<RotationHandler>();

				//Start bot
				await provider.GetRequiredService<StartupService>().StartAsync();

				//Register slash commands/interactions
				client.Ready += RegisterCommands;
				
				await Task.Delay(-1);
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}

		private async Task RegisterCommands()
		{
			try
			{
				Log.Debug("[Interactions] Registering guild interactions...");
				await _commands.RegisterCommandsToGuildAsync(848176216011046962);
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}

		private void ConfigureServices(IServiceCollection services)
		{
			try
			{
				services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
				{
					LogLevel = LogSeverity.Verbose,
					MessageCacheSize = 0,
					TotalShards = 1,
					DefaultRetryMode = RetryMode.AlwaysFail,
					AlwaysDownloadUsers = false,
					HandlerTimeout = 3000,
					ConnectionTimeout = 30000,
					IdentifyMaxConcurrency = 1,
					MaxWaitBetweenGuildAvailablesBeforeReady = 10000,
					LargeThreshold = 250,
					GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages,
					UseInteractionSnowflakeDate = true,
					UseSystemClock = true,
					AlwaysResolveStickers = false,
					AlwaysDownloadDefaultStickers = false,
					LogGatewayIntentWarnings = false
				}));
				services.AddSingleton<StartupService>();
				services.AddSingleton<InteractionService>();
				services.AddSingleton<RotationHandler>();
				services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
				services.AddSingleton<CommandHandler>();
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}
	}
}