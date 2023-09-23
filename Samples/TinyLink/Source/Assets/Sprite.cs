
using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Sprite
{
	public readonly record struct Frame(Subtexture Subtexture, float Duration);
	public readonly record struct Animation(string Name, int FrameStart, int FrameCount, float Duration);

	public readonly string Name;
	public readonly Vector2 Origin;
	public readonly List<Frame> Frames = new();
	public readonly List<Animation> Animations = new();

	public Sprite(string name, Vector2 origin)
	{
		Name = name;
		Origin = origin;
	}
	
	public Frame GetFrameAt(in Animation animation, float time, bool loop)
	{
		if (time >= animation.Duration && !loop)
			return Frames[animation.FrameStart + animation.FrameCount - 1];

		time %= animation.Duration;
		for (int i = animation.FrameStart; i < animation.FrameStart + animation.FrameCount; i ++)
		{
			time -= Frames[i].Duration;
			if (time <= 0)
				return Frames[i];
		}
		return Frames[animation.FrameStart];
	}

	public void AddAnimation(string name, int frameStart, int frameCount)
	{
		float duration = 0;
		for (int i = frameStart; i < frameStart + frameCount; i ++)
			duration += Frames[i].Duration;
		Animations.Add(new(name, frameStart, frameCount, duration));
	}

	public Animation? GetAnimation(string? name)
	{
		if (name != null)
		{
			foreach (var it in Animations)
				if (it.Name == name)
					return it;
		}

		return null;
	}
}