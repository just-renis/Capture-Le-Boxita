using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class SoundManager : GameObjectSystem
{
	public static SoundManager Instance { get; private set; }

	private Dictionary<ESound, SoundEvent> Sounds = new();
	
	public SoundManager(Scene InScene) : base(InScene) { Instance = this; }
	
	public void Play2D(ESound InSoundName)
	{
		if (!Sounds.TryGetValue(InSoundName, out SoundEvent SoundEvent)) return;
		Sound.Play(SoundEvent);
	}
	public void PlayAtPosition(ESound InSoundName, Vector3 InPosition)
	{
		BroadcastSoundToAllClients(InSoundName, InPosition);
	}
	public void PlayAttached(ESound InSoundName, GameObject InTarget)
	{
		if (InTarget == null) return;
		BroadcastSoundAttachedToAllClients(InSoundName, InTarget);
	}
	public void Init(Dictionary<ESound, SoundEvent> InSounds)
	{
		Sounds = InSounds;
	}
	public void OnDestroy()
	{
		Sounds.Clear();
		if (Instance == this) Instance = null;
	}

	[Rpc.Broadcast]
	private void BroadcastSoundToAllClients(ESound InSoundName, Vector3 InPosition)
	{
		if (!Sounds.TryGetValue(InSoundName, out SoundEvent SoundEvent)) return;
		Sound.Play(SoundEvent, InPosition);
	}
	[Rpc.Broadcast]
	private void BroadcastSoundAttachedToAllClients(ESound InSoundName, GameObject InTarget)
	{
		if (InTarget == null || !InTarget.IsValid()) return;
		if (!Sounds.TryGetValue(InSoundName, out SoundEvent SoundEvent)) return;
		InTarget.PlaySound(SoundEvent);
	}
}
