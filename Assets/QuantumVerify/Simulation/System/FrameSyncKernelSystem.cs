namespace Quantum {
  using Photon.Deterministic;

  public unsafe class FrameSyncKernelSystem : SystemMainThread {
    private static readonly FP Boundary = FP._1 * 8;
    private static readonly FP PassiveDrain = FP._0_01;
    private static readonly FP ActionDrain = FP._0_03;
    private static readonly FP BaseAcceleration = FP._0_05;
    private static readonly FP ActionAcceleration = FP._0_03;
    private static readonly FP Friction = FP._0_92;
    private static readonly FP MaxSpeed = FP._1 + FP._0_50;

    public override void OnInit(Frame f) {
      var state = f.Unsafe.GetOrAddSingletonPointer<FrameSyncKernelState>();
      state->Tick = 0;
      state->P0Position = new FPVector2(-FP._2, FP._1);
      state->P1Position = new FPVector2(FP._2, -FP._1);
      state->P0Velocity = FPVector2.Zero;
      state->P1Velocity = FPVector2.Zero;
      state->P0Energy = FP._1 * 10;
      state->P1Energy = FP._1 * 10;
      state->Accumulator = FP._0;
      state->LastDistance = FPVector2.Distance(state->P0Position, state->P1Position);
      state->BounceCount = 0;
      state->ActionCount = 0;
    }

    public override void Update(Frame f) {
      var state = f.Unsafe.GetPointerSingleton<FrameSyncKernelState>();
      state->Tick++;

      StepPlayer(f, (PlayerRef)0, ref state->P0Position, ref state->P0Velocity, ref state->P0Energy, state);

      if (f.MaxPlayerCount > 1) {
        StepPlayer(f, (PlayerRef)1, ref state->P1Position, ref state->P1Velocity, ref state->P1Energy, state);
      }

      state->LastDistance = FPVector2.Distance(state->P0Position, state->P1Position);

      var phase = state->Tick * FP._0_10;
      state->Accumulator += FPMath.Sin(phase) + FPMath.Cos(phase * FP._0_50);
      state->Accumulator += (state->LastDistance * FP._0_01);
      state->Accumulator += (state->ActionCount * FP._0_01);
      state->Accumulator += (state->BounceCount * FP._0_05);
    }

    private static void StepPlayer(Frame f, PlayerRef player, ref FPVector2 position, ref FPVector2 velocity, ref FP energy, FrameSyncKernelState* state) {
      var input = f.GetPlayerInput(player);
      var direction = DecodeDirection(input->EncodedMoveDirection);
      var actionPressed = (input->ActionMask & 1) != 0;

      var acceleration = BaseAcceleration;
      if (actionPressed) {
        acceleration += ActionAcceleration;
        state->ActionCount++;
      }

      if (direction != FPVector2.Zero) {
        velocity += direction * acceleration;
      }

      velocity *= Friction;

      var speed = velocity.Magnitude;
      if (speed > MaxSpeed && speed > FP._0) {
        velocity = velocity.Normalized * MaxSpeed;
      }

      position += velocity;
      ClampAndBounce(ref position, ref velocity, state);

      energy -= PassiveDrain;
      if (actionPressed) {
        energy -= ActionDrain;
      }

      if (energy < FP._0) {
        energy = FP._0;
      }
    }

    private static void ClampAndBounce(ref FPVector2 position, ref FPVector2 velocity, FrameSyncKernelState* state) {
      if (position.X > Boundary) {
        position.X = Boundary;
        velocity.X = -velocity.X * FP._0_50;
        state->BounceCount++;
      } else if (position.X < -Boundary) {
        position.X = -Boundary;
        velocity.X = -velocity.X * FP._0_50;
        state->BounceCount++;
      }

      if (position.Y > Boundary) {
        position.Y = Boundary;
        velocity.Y = -velocity.Y * FP._0_50;
        state->BounceCount++;
      } else if (position.Y < -Boundary) {
        position.Y = -Boundary;
        velocity.Y = -velocity.Y * FP._0_50;
        state->BounceCount++;
      }
    }

    private static FPVector2 DecodeDirection(byte encodedMoveDirection) {
      switch (encodedMoveDirection) {
        case 1:
          return new FPVector2(FP._0, FP._1);
        case 2:
          return new FPVector2(FP._1, FP._1).Normalized;
        case 3:
          return new FPVector2(FP._1, FP._0);
        case 4:
          return new FPVector2(FP._1, -FP._1).Normalized;
        case 5:
          return new FPVector2(FP._0, -FP._1);
        case 6:
          return new FPVector2(-FP._1, -FP._1).Normalized;
        case 7:
          return new FPVector2(-FP._1, FP._0);
        case 8:
          return new FPVector2(-FP._1, FP._1).Normalized;
        default:
          return FPVector2.Zero;
      }
    }
  }
}
