using System;

namespace Foster.Framework;

/// <summary>
/// Ease Delegates
/// </summary>
public struct Ease
{
	public delegate float Easer(float t);

	#region Ease
	public Easer In { get; set; }
	public Easer Out { get; set; }
	public Easer InOut { get; set; }

	public Ease(Easer @in, Easer? @out = null, Easer? inOut = null)
	{
		In = @in;
		if (@out != null)
			Out = @out;
		else
			Out = Invert(@in);
		if (inOut != null)
			InOut = inOut;
		else
			InOut = Follow(In, Out);
	}

	public float Apply(float t)
	{
		if (t <= 0)
			return 0;
		if (t >= 1)
			return 1;
		return InOut(t);
	}

	#endregion

	#region Easers

	public static readonly Ease Linear = new(t => t);
	public static readonly Ease Quad = new(t => t * t);
	public static readonly Ease Cube = new(t => t * t * t);
	public static readonly Ease Quart = new(t => t * t * t * t);
	public static readonly Ease Quint = new(t => t * t * t * t * t);

	public static readonly Ease Sine = new(
		(float t) => -(float)Math.Cos(Calc.HalfPI * t) + 1,
		(float t) => (float)Math.Sin(Calc.HalfPI * t),
		(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f);

	public static readonly Ease Expo = new(t => (float)Math.Pow(2, 10 * (t - 1)));

	public static readonly Ease Back = new(t => t * t * (2.70158f * t - 1.70158f));
	public static readonly Ease BigBack = new(t => t * t * (4f * t - 3f));

	public static readonly Ease Elastic = new(
	(float t) =>
	{
		var ts = t * t;
		var tc = ts * t;
		return (33 * tc * ts + -59 * ts * ts + 32 * tc + -5 * ts);
	},
	(float t) =>
	{
		var ts = t * t;
		var tc = ts * t;
		return (33 * tc * ts + -106 * ts * ts + 126 * tc + -67 * ts + 15 * t);
	});

	private const float B1 = 1f / 2.75f;
	private const float B2 = 2f / 2.75f;
	private const float B3 = 1.5f / 2.75f;
	private const float B4 = 2.5f / 2.75f;
	private const float B5 = 2.25f / 2.75f;
	private const float B6 = 2.625f / 2.75f;

	public static readonly Ease Bounce = new((float t) =>
	{
		t = 1 - t;
		if (t < B1)
			return 1 - 7.5625f * t * t;
		if (t < B2)
			return 1 - (7.5625f * (t - B3) * (t - B3) + .75f);
		if (t < B4)
			return 1 - (7.5625f * (t - B5) * (t - B5) + .9375f);
		return 1 - (7.5625f * (t - B6) * (t - B6) + .984375f);
	},
	(float t) =>
	{
		if (t < B1)
			return 7.5625f * t * t;
		if (t < B2)
			return 7.5625f * (t - B3) * (t - B3) + .75f;
		if (t < B4)
			return 7.5625f * (t - B5) * (t - B5) + .9375f;
		return 7.5625f * (t - B6) * (t - B6) + .984375f;
	});

	public static Easer Invert(Easer easer)
	{
		return (float t) => { return 1 - easer(1 - t); };
	}

	public static Easer Follow(Easer first, Easer second)
	{
		return (float t) => { return (t <= 0.5f) ? first(t * 2) / 2 : second(t * 2 - 1) / 2 + 0.5f; };
	}

	public static float UpDown(float eased)
	{
		if (eased <= .5f)
			return eased * 2;
		else
			return 1 - (eased - .5f) * 2;
	}

	#endregion
}
