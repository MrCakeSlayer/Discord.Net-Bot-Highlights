namespace DNetBotHighlight.Models;

public class DBBotInfo
{
	public int ID { get; set; }
	public ulong OwnerID { get; set; }
	public ulong BotID { get; set; }
	public string? Avatar { get; set; }
	public string TopGgUrl { get; set; }
	public string BotName { get; set; }
	public string BotDescription { get; set; }
	public string InviteUrl { get; set; }
	public int ServerCount { get; set; }
	public string? ImageBanner { get; set; }
	public string? Link1 { get; set; }
	public string? Link2 { get; set; }
	public string? Link3 { get; set; }
	public int VerifiedStatus { get; set; }
	public ulong? ModeratorID { get; set; }
	public string? DenialReason { get; set; }
}