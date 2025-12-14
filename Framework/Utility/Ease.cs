namespace Foster.Framework;

/// <summary>
/// Ease Delegates
/// </summary>
public readonly struct Ease
{
	public delegate float Easer(float t);

	public readonly Easer In;
	public readonly Easer Out;
	public readonly Easer InOut;

	public Ease(Easer @in, Easer? @out = null, Easer? inOut = null)
	{
		In = @in;
		Out = @out ?? Invert(In);
		InOut = inOut ?? Follow(In, Out);
	}

	public readonly float Apply(float t) =>
		t <= 0 ? 0 : (t >= 1 ? 1 : InOut(t));

	public static readonly Easer Linear = t => t;
	public static readonly Ease Quad = new(t => t * t);
	public static readonly Ease Cube = new(t => t * t * t);
	public static readonly Ease Quart = new(t => t * t * t * t);
	public static readonly Ease Quint = new(t => t * t * t * t * t);
	public static readonly Ease Sine = new(
		t => -MathF.Cos(Calc.HalfPI * t) + 1,
		t => MathF.Sin(Calc.HalfPI * t),
		t => -(MathF.Cos(MathF.PI * t) - 1f) / 2f);
	public static readonly Ease Expo = new(t => MathF.Pow(2, 10 * (t - 1)));
	public static readonly Ease Back = new(t => t * t * (2.70158f * t - 1.70158f));
	public static readonly Ease BigBack = new(t => t * t * (4f * t - 3f));
	public static readonly Ease Elastic = new(
		t =>
		{
			var ts = t * t;
			var tc = ts * t;
			return (33 * tc * ts + -59 * ts * ts + 32 * tc + -5 * ts);
		},
		t =>
		{
			var ts = t * t;
			var tc = ts * t;
			return (33 * tc * ts + -106 * ts * ts + 126 * tc + -67 * ts + 15 * t);
		}
	);

	private const float B1 = 1f / 2.75f;
	private const float B2 = 2f / 2.75f;
	private const float B3 = 1.5f / 2.75f;
	private const float B4 = 2.5f / 2.75f;
	private const float B5 = 2.25f / 2.75f;
	private const float B6 = 2.625f / 2.75f;

	public static readonly Ease Bounce = new(
		t =>
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
		t =>
		{
			if (t < B1)
				return 7.5625f * t * t;
			if (t < B2)
				return 7.5625f * (t - B3) * (t - B3) + .75f;
			if (t < B4)
				return 7.5625f * (t - B5) * (t - B5) + .9375f;
			return 7.5625f * (t - B6) * (t - B6) + .984375f;
		}
	);

	public static Easer Invert(Easer easer)
		=> t => 1 - easer(1 - t);

	public static Easer Follow(Easer first, Easer second)
		=> t => (t <= 0.5f) ? first(t * 2) / 2 : second(t * 2 - 1) / 2 + 0.5f;

	public static float UpDown(float eased)
		=> eased <= 0.5f ? (eased * 2) : (1 - (eased - .5f) * 2);
}
