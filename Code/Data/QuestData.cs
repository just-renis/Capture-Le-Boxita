using Sandbox.Enums;
namespace Sandbox.Data;

public sealed class QuestData
{
	public int RequiredProgress { get; set; }
	public int RewardAmount { get; set; }
	public string QuestText { get; set; }
	public EQuest QuestName { get; set; }
	
	public override string ToString() { return QuestName.ToString(); }
}
