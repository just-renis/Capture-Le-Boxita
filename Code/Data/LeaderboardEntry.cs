using System;
namespace Sandbox.Data;

public sealed class LeaderboardEntry(Guid InId, string InName, int InMoney)
{
	public Guid Id = InId;
	public int Money = InMoney;
	public string Name = InName;
}
