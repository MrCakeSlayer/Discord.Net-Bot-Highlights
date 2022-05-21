using System.Data.SQLite;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Discord.Commands;
using Serilog;
using IResult = Discord.Interactions.IResult;

namespace DNetBotHighlight.Services
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient _client;
		private readonly InteractionService _commands;
		private readonly IServiceProvider _services;
		private readonly RotationHandler _events;

		public CommandHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services, RotationHandler events)
		{
			_client = client;
			_commands = commands;
			_services = services;
			_events = events;
		}

		public async Task InitializeAsync()
		{
			try
			{
				// Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
				//Log.Debug("[Interactions] Loading modules...");
				await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
				// Another approach to get the assembly of a specific type is:
				// typeof(CommandHandler).Assembly
				
				// Process the InteractionCreated payloads to execute Interactions commands
				//Log.Debug("[Interactions] Registering event handlers...");
				_client.InteractionCreated += HandleInteraction;
				_client.MessageReceived += HandleMessage; //Only used for setup. Disable after setup.

				// Process the command execution results 
				_commands.SlashCommandExecuted += SlashCommandExecuted;
				//_commands.ContextCommandExecuted += ContextCommandExecuted;
				_commands.ComponentCommandExecuted += ComponentCommandExecuted;
				//_commands.AutocompleteHandlerExecuted += AutocompleteHandlerExecuted;
				//_commands.AutocompleteCommandExecuted += AutocompleteCommandExecuted;
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}

		private async Task HandleMessage(SocketMessage msg)
		{
			try
			{
				if (msg.Author.Id == 173439637388263425 && msg.Content == ">createdb")
				{
					var context = new SocketCommandContext(_client, (SocketUserMessage) msg);
					
					//Create database
					SQLiteConnection.CreateFile("Database.sqlite");
					await context.Channel.SendMessageAsync("Database created.");

					var dbConnection = new SQLiteConnection("Data Source=Database.sqlite;Version=3;");
					dbConnection.Open();
					await context.Channel.SendMessageAsync("Database connected.");

					string sql = "CREATE TABLE Bots (ID INTEGER primary key autoincrement, OwnerID DECIMAL(18,0) NOT NULL, BotID DECIMAL(18,0) NOT NULL, Avatar VARCHAR(255), TopGgUrl VARCHAR(255) NOT NULL, BotName VARCHAR(100) NOT NULL, BotDescription VARCHAR(1000) NOT NULL, InviteURL VARCHAR(255) NOT NULL, ServerCount INT NOT NULL, ImageBanner VARCHAR(255), Link1 VARCHAR(255), Link2 VARCHAR(255), Link3 VARCHAR(255), VerifiedStatus SMALLINT NOT NULL, ModeratorID DECIMAL(18,0), DenialReason VARCHAR(255))";
					SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
					command.ExecuteNonQuery();
					await context.Channel.SendMessageAsync($"Table created.");
				}
				
				if (msg.Author.Id == 173439637388263425 && msg.Content == ">createmessages")
				{
					var context = new SocketCommandContext(_client, (SocketUserMessage) msg);
					await context.Channel.SendMessageAsync($"Weekly highlight position");
					await context.Channel.SendMessageAsync($"Daily highlight position");
				}
				
				if (msg.Author.Id == 173439637388263425 && msg.Content == ">clearmessages")
				{
					var message = await _client.GetGuild(848176216011046962).GetTextChannel(966174454801657877).GetMessageAsync(966175717488476170) as IUserMessage;
					if (message == null) return;
					await message.ModifyAsync(x =>
					{
						x.Content = "TBD";
						x.Embed = null;
						x.Components = null;
					});
					message = await _client.GetGuild(848176216011046962).GetTextChannel(966174454801657877).GetMessageAsync(966175716779626496) as IUserMessage;
					if (message == null) return;
					await message.ModifyAsync(x =>
					{
						x.Content = "TBD";
						x.Embed = null;
						x.Components = null;
					});
				}

				if (msg.Author.Id == 173439637388263425 && msg.Content == ">resetembeds")
				{
					_events.ResetEmbeds();
				}
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}
		}

		# region Error Handling

		private Task ComponentCommandExecuted(ComponentCommandInfo commandInfo, IInteractionContext context, IResult result)
		{
			try
			{
				if (result.IsSuccess) return Task.CompletedTask;
				switch (result.Error)
				{
					case InteractionCommandError.UnmetPrecondition:
						context.Interaction.RespondAsync("**Error:**\nUnmet precondition: " + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.UnknownCommand:
						//context.Interaction.RespondAsync("**Error:**\nUnknownCommand" + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.BadArgs:
						context.Interaction.RespondAsync("**Error:**\nBad arguments.", ephemeral: true);
						break;
					case InteractionCommandError.Exception:
						context.Interaction.RespondAsync("**Error:**\nException: " + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.Unsuccessful:
						context.Interaction.RespondAsync("**Error:**\nUnsuccessful: " + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.ConvertFailed:
						context.Interaction.RespondAsync("**Error:**\nConvsion failed.", ephemeral: true);
						break;
					case InteractionCommandError.ParseFailed:
						context.Interaction.RespondAsync("**Error:**\nParsing failed.", ephemeral: true);
						break;
					default:
						context.Interaction.RespondAsync("**Error:**\nUnknown error occured: " + result.Error, ephemeral: true);
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}

			return Task.CompletedTask;
		}

		private Task SlashCommandExecuted(SlashCommandInfo commandInfo, IInteractionContext context, IResult result)
		{
			try
			{
				if (result.IsSuccess) return Task.CompletedTask;
				switch (result.Error)
				{
					case InteractionCommandError.UnmetPrecondition:
						context.Interaction.RespondAsync("**Error:**\nUnmet precondition: " + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.UnknownCommand:
						//context.Interaction.RespondAsync("**Error:**\nUnknownCommand" + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.BadArgs:
						context.Interaction.RespondAsync("**Error:**\nBad arguments.", ephemeral: true);
						break;
					case InteractionCommandError.Exception:
						context.Interaction.RespondAsync("**Error:**\nException: " + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.Unsuccessful:
						context.Interaction.RespondAsync("**Error:**\nUnsuccessful: " + result.ErrorReason, ephemeral: true);
						break;
					case InteractionCommandError.ConvertFailed:
						context.Interaction.RespondAsync("**Error:**\nConvsion failed.", ephemeral: true);
						break;
					case InteractionCommandError.ParseFailed:
						context.Interaction.RespondAsync("**Error:**\nParsing failed.", ephemeral: true);
						break;
					default:
						context.Interaction.RespondAsync("**Error:**\nUnknown error occured: " + result.Error, ephemeral: true);
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);
			}

			return Task.CompletedTask;
		}

		# endregion

		# region Execution
		private async Task HandleInteraction(SocketInteraction arg)
		{
			try
			{
				// Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
				var ctx = new SocketInteractionContext(_client, arg);
				//Log.Debug("[Interactions] Context created");
				
				//Disable interactions in DMs 
				if (ctx.Interaction.Channel is IPrivateChannel)
				{
					//Log.Debug("[Interactions] DM detected");
					await ctx.Interaction.RespondAsync("**Error:**\nYou can not use interactions in DMs.", ephemeral: true);
					return;
				}
				
				//Check if it's an archived thread
				if (ctx.Interaction.Channel is SocketThreadChannel { IsArchived: true }) return;

				//Log.Debug("[Interactions] Executing");
				await _commands.ExecuteCommandAsync(ctx, _services);
				//Log.Debug("[Interactions] Executed");
				//Log.Debug($"[Interactions] Result: {result.IsSuccess} / {result.ErrorReason} / {(result.Error.HasValue ? result.Error.Value.ToString() : "No error")}");
			}
			catch (Exception ex)
			{
				Log.Error("{Msg}\n{Stack}", ex.Message, ex.StackTrace);

				// If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
				// response, or at least let the user know that something went wrong during the command execution.
				if (arg.Type == InteractionType.ApplicationCommand)
				{
					var msg = await arg.GetOriginalResponseAsync();
					await msg.DeleteAsync();
				}
			}
		}

		# endregion
	}
}