namespace DNetBotHighlight.Models;

public class BotStats
{
	public int server_count { get; set; }
	public List<int>? shards { get; set; }
	public int? shard_count { get; set; }
}