namespace DNetBotHighlight.Models;

public class BotInfo
{
	public string defAvatar { get; set; }
	public string invite { get; set; }
	public string website { get; set; }
	public string support { get; set; }
	public string github { get; set; }
	public string longdesc { get; set; }
	public string shortdesc { get; set; }
	public string prefix { get; set; }
	public string lib { get; set; }
	public string clientid { get; set; }
	public string avatar { get; set; }
	public string id { get; set; }
	public string discriminator { get; set; }
	public string username { get; set; }
	public DateTime date { get; set; }
	public int server_count { get; set; }
	public int? shard_count { get; set; }
	public List<string> guilds { get; set; }
	public List<int>? shards { get; set; }
	public int monthlyPoints { get; set; }
	public int points { get; set; }
	public bool certifiedBot { get; set; }
	public List<string> owners { get; set; }
	public List<string> tags { get; set; }
	public string bannerUrl { get; set; }
	public object donatebotguildid { get; set; }
}