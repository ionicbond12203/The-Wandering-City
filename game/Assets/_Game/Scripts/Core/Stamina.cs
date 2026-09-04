using System;

namespace WanderingCity
{
    // No Unity clock or input dependency. Drain and recovery consume the same simulated time.
    public sealed class Stamina
    {
        public float Current { get; private set; }
        public float Max { get; }
        public float DrainRate { get; private set; }
        public float RecoveryRate { get; }
        public float RecoveryDelay { get; }
        float delay;
        public bool Exhausted => Current <= 0;
        public Stamina(float max, float recovery, float recoveryDelay)
        {
            Max = Math.Max(1, max); RecoveryRate = Math.Max(0, recovery); RecoveryDelay = Math.Max(0, recoveryDelay); Current = Max;
        }
        public void Tick(float dt, float drain)
        {
            if (!float.IsFinite(dt) || !float.IsFinite(drain) || dt <= 0) return;
            DrainRate = Math.Max(0, drain);
            if (DrainRate > 0) { Current = Math.Max(0, Current - DrainRate * dt); delay = RecoveryDelay; return; }
            float recoveryTime = Math.Max(0, dt - delay); delay = Math.Max(0, delay - dt);
            Current = Math.Min(Max, Current + RecoveryRate * recoveryTime);
        }
        public void Refill() { Current = Max; delay = 0; DrainRate = 0; }
    }
}
