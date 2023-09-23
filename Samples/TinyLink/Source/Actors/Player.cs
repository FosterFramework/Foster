using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Player : Actor
{
	public enum States
	{
		Normal,
		Attack,
		Hurt,
		Start
	}

	public const int MaxHealth = 4;
	private const float MaxGroundSpeed = 60;
	private const float MaxAirSpeed = 70;
	private const float GroundAccel = 500;
	private const float AirAccel = 100;
	private const float Friction = 800;
	private const float AttackFriction = 150;
	private const float HurtFriction = 200;
	private const float Gravity = 450;
	private const float JumpForce = -105;
	private const float JumpTime = 0.18f;
	private const float HurtDuration = 0.5f;
	private const float DeathDuration = 1.5f;
	private const float InvincibleDuration = 1.5f;

	public int Health = MaxHealth;
	public States State;

	private float stateDuration = 0;
	private float jumpTimer = 0;
	private bool grounded = false;
	private bool ducking = false;

	public Player()
	{
		State = States.Start;
		Sprite = Assets.GetSprite("player");
		Hitbox = new(new RectInt(-4, -12, 8, 12));
		Mask = Masks.Player;
		IFrameTime = InvincibleDuration;
		grounded = true;
		Play("sword");
	}

	public override void Update()
	{
		base.Update();

		// update grounded state
		var nowGrounded = Velocity.Y >= 0 && Grounded();
		if (nowGrounded && !grounded)
			Squish = new Vector2(1.5f, 0.70f);
		grounded = nowGrounded;

		// increment state timer
		var wasState = State;

		// state control
		switch (State)
		{
			case States.Normal:
				NormalState();
				break;
			case States.Attack:
				AttackState();
				break;
			case States.Hurt:
				HurtState();
				break;
			case States.Start:
				StartState();
				break;
		}

		// ducking collider(s)
		if (ducking && State != States.Normal)
			ducking = false;
		if (ducking)
			Hitbox = new(new RectInt(-4, -6, 8, 6));
		else
			Hitbox = new(new RectInt(-4, -12, 8, 12));

		// variable jumping
		if (jumpTimer > 0)
		{
			Velocity.Y = JumpForce;
			jumpTimer -= Time.Delta;
			if (!Controls.Jump.Down)
				jumpTimer = 0;
		}

		// gravity
		if (!grounded)
		{
			float grav = Gravity;
			if (State == States.Normal && MathF.Abs(Velocity.Y) < 20 && Controls.Jump.Down)
				grav *= 0.40f;
			Velocity.Y += grav * Time.Delta;
		}

		// goto next room
		if (Health > 0)
		{
			if (Position.X > Game.Bounds.Right && !Game.Transition(Point2.Right))
			{
				Position.X = Game.Bounds.Right;
			}
			else if (Position.X < Game.Bounds.Left && !Game.Transition(Point2.Left))
			{
				Position.X = Game.Bounds.Left;
			}
			else if (Position.Y > Game.Bounds.Bottom + 12 && !Game.Transition(Point2.Down))
			{
				Health = 0;
				State = States.Hurt;
			}
			else if (Position.Y < Game.Bounds.Top)
			{
				if (Game.Transition(Point2.Up))
					Velocity.Y = -150;
				else
					Position.Y = Game.Bounds.Top;
			}
		}

		// detect getting hit
		if (OverlapsFirst(Masks.Enemy | Masks.Hazard) is Actor hit)
			hit.Hit(this);

		stateDuration += Time.Delta;
		if (State != wasState)
			stateDuration = 0.0f;
	}

	public void NormalState()
	{
		// update ducking state
		ducking = grounded && Controls.Move.IntValue.Y > 0;

		// get input
		var input = Controls.Move.IntValue.X;
		if (ducking)
			input = 0;

		// sprite
		if (grounded)
		{
			if (ducking)
				Play("duck");
			else if (input == 0)
				Play("idle");
			else
				Play("run");
		}
		else
		{
			Play("jump");
		} 

		// horizontal movement
		{
			// Acceleration
			Velocity.X += input * (grounded ? GroundAccel : AirAccel) * Time.Delta;

			// Max Speed
			var maxspd = grounded ? MaxGroundSpeed : MaxAirSpeed;
			if (MathF.Abs(Velocity.X) > maxspd)
				Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxspd, 2000 * Time.Delta);
			
			// Friction
			if (input == 0 && grounded)
				Velocity.X = Calc.Approach(Velocity.X, 0, Friction * Time.Delta);

			// Facing
			if (grounded && input != 0)
				Facing = input;
		}
		
		// Start jumping
		if (Controls.Jump.Pressed && grounded)
		{
			Controls.Jump.ConsumeBuffer();
			Squish = new Vector2(0.65f, 1.4f);
			StopX();
			Velocity.X = input * MaxAirSpeed;
			jumpTimer = JumpTime;
		}

		// Begin Attack
		if (Controls.Attack.Pressed)
		{
			Controls.Attack.ConsumeBuffer();
			State = States.Attack;
			if (grounded)
				StopX();
		}
	}

	public void AttackState()
	{
		Play("attack", false);

		RectInt? hitbox = null;

		if (stateDuration < 0.2f)
		{
			hitbox = new RectInt(-16, -12, 17, 8);
		}
		else if (stateDuration < 0.50f)
		{
			hitbox = new RectInt(8, -8, 16, 8);
		}

		if (hitbox != null)
		{
			var it = hitbox.Value;
			if (Facing == Facing.Left)
				it.X = -(it.X + it.Width);
			it += Position;

			if (Game.OverlapsFirst(it, Masks.Enemy | Masks.Hazard) is Actor hit)
				Hit(hit);
		}

		if (Grounded())
			Velocity.X = Calc.Approach(Velocity.X, 0, AttackFriction * Time.Delta);
		
		if (stateDuration >= Animation.Duration)
		{
			Play("idle");
			State = States.Normal;
		} 
	}

	public void HurtState()
	{
		if (stateDuration <= 0 && Health <= 0)
		{
			foreach (var actor in Game.Actors)
				if (actor != this)
					Game.Destroy(actor);
			Game.Shake(0.1f);
		}

		Velocity.X = Calc.Approach(Velocity.X, 0, HurtFriction * Time.Delta);

		if (stateDuration >= HurtDuration && Health > 0)
			State = States.Normal;

		if (stateDuration >= DeathDuration && Health <= 0)
			Game.ReloadRoom();
	}

	public void StartState()
	{
		if (stateDuration >= 1.0f)
			State = States.Normal;
	}

	public override void OnWasHit(Actor by)
	{
		Game.Hitstun(0.1f);
		Game.Shake(0.1f);

		Play("hurt");

		Velocity = new Vector2(-Facing * 100, -80);
		State = States.Hurt;
		Health--;
	}
}