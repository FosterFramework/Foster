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

	public static readonly Easer Linear = t => t;
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

	#region Obsolete Compatibility

	// redirections to ease implementations
	[Obsolete("Use Sine.In instead")]
	public static readonly Easer SineIn = Sine.In;
	[Obsolete("Use Sine.Out instead")]
	public static readonly Easer SineOut = Sine.Out;
	[Obsolete("Use Sine.InOut instead")]
	public static readonly Easer SineInOut = Sine.InOut;

	[Obsolete("Use Expo.In instead")]
	public static readonly Easer ExpoIn = Expo.In;
	[Obsolete("Use Expo.Out instead")]
	public static readonly Easer ExpoOut = Expo.Out;
	[Obsolete("Use Expo.InOut instead")]
	public static readonly Easer ExpoInOut = Expo.InOut;

	[Obsolete("Use Quad.In instead")]
	public static readonly Easer QuadIn = Quad.In;
	[Obsolete("Use Quad.Out instead")]
	public static readonly Easer QuadOut = Quad.Out;
	[Obsolete("Use Quad.InOut instead")]
	public static readonly Easer QuadInOut = Quad.InOut;

	[Obsolete("Use Cube.In instead")]
	public static readonly Easer CubeIn = Cube.In;
	[Obsolete("Use Cube.Out instead")]
	public static readonly Easer CubeOut = Cube.Out;
	[Obsolete("Use Cube.InOut instead")]
	public static readonly Easer CubeInOut = Cube.InOut;

	[Obsolete("Use Back.In instead")]
	public static readonly Easer BackIn = Back.In;
	[Obsolete("Use Back.Out instead")]
	public static readonly Easer BackOut = Back.Out;
	[Obsolete("Use Back.InOut instead")]
	public static readonly Easer BackInOut = Back.InOut;

	[Obsolete("Use BigBack.In instead")]
	public static readonly Easer BigBackIn = BigBack.In;
	[Obsolete("Use BigBack.Out instead")]
	public static readonly Easer BigBackOut = BigBack.Out;
	[Obsolete("Use BigBack.InOut instead")]
	public static readonly Easer BigBackInOut = BigBack.InOut;

	[Obsolete("Use Elastic.In instead")]
	public static readonly Easer ElasticIn = Elastic.In;
	[Obsolete("Use Elastic.Out instead")]
	public static readonly Easer ElasticOut = Elastic.Out;
	[Obsolete("Use Elastic.InOut instead")]
	public static readonly Easer ElasticInOut = Elastic.InOut;

	[Obsolete("Use Bounce.In instead")]
	public static readonly Easer BounceIn = Bounce.In;
	[Obsolete("Use Bounce.Out instead")]
	public static readonly Easer BounceOut = Bounce.Out;
	[Obsolete("Use Bounce.InOut instead")]
	public static readonly Easer BounceInOut = Bounce.InOut;

	#endregion
}
