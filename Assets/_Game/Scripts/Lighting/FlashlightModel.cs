using UnityEngine;

namespace Game
{
    // Simulation owns the clock. No wall-clock or rendering state is authoritative here.
    public sealed class FlashlightModel
    {
        public readonly FlashlightTuning Tuning;
        public float Charge { get; private set; } = 100;
        public bool SwitchedOn { get; private set; } = true;
        public bool Boosting { get; private set; }
        public float SwapRemaining { get; private set; }
        public float BoostCooldown { get; private set; }
        public float GraceRemaining { get; private set; }
        public bool Faulted { get; private set; }
        public bool Active => !Faulted && SwitchedOn && Charge > 0 && SwapRemaining <= 0;
        public float Range => Active ? CalculateRange(Charge, Tuning) * (Boosting ? Tuning.boostRange : 1) : 0;
        public float Intensity => Active ? (0.35f + 0.65f * Charge / 100) * (Boosting ? 1.35f : 1) : 0;
        float boostUsed, protectedSeconds;
        bool graceArmed = true, wasProtected;

        public FlashlightModel(FlashlightTuning tuning) { Tuning = tuning; }
        public static float CalculateRange(float charge, FlashlightTuning tuning)
            => tuning.minRange + (tuning.maxRange - tuning.minRange) * Mathf.Sqrt(Mathf.Clamp01(charge / 100));
        public void Toggle() { if (!Faulted && SwapRemaining <= 0) SwitchedOn = !SwitchedOn; }
        public bool BeginSwap(InventoryService inventory, bool instant = false)
        {
            if (Faulted || SwapRemaining > 0 || !inventory.ConsumeBattery()) return false;
            Boosting = false;
            if (instant) Charge = 100;
            else SwapRemaining = Tuning.swapSeconds;
            return true;
        }
        public void Restore(float charge, bool on)
        {
            Faulted=false;Charge = Mathf.Clamp(charge, 0, 100); SwitchedOn = on;
            SwapRemaining = boostUsed = BoostCooldown = GraceRemaining = protectedSeconds = 0;
            Boosting = wasProtected = false; graceArmed = true;
        }
        public void EmergencyFill() { if(!Faulted)Charge = Mathf.Max(Charge, Tuning.emergencyCharge); }
        public void FailCircuit()
        { Faulted=true;SwitchedOn=false;Charge=0;Boosting=false;SwapRemaining=boostUsed=BoostCooldown=GraceRemaining=0; }
        public bool RepairAtHouse(bool connectedToHouseCharger)
        {
            if(!Faulted||!connectedToHouseCharger)return false;
            Restore(Tuning.emergencyCharge,true);return true;
        }
        public void Tick(float dt, bool boostHeld)
        {
            if (dt <= 0) return;
            GraceRemaining = Mathf.Max(0, GraceRemaining - dt);
            BoostCooldown = Mathf.Max(0, BoostCooldown - dt);
            if (SwapRemaining > 0)
            {
                SwapRemaining = Mathf.Max(0, SwapRemaining - dt);
                if (SwapRemaining == 0) Charge = 100;
            }
            bool shouldBoost = Active && boostHeld && BoostCooldown <= 0;
            if (Boosting && !shouldBoost) { BoostCooldown = Tuning.boostRecovery; boostUsed = 0; }
            Boosting = shouldBoost;
            if (Boosting)
            {
                boostUsed += dt;
                if (boostUsed >= Tuning.boostSeconds)
                { Boosting = false; boostUsed = 0; BoostCooldown = Tuning.boostRecovery; }
            }
            if (Active) Charge = Mathf.Max(0, Charge - dt * 100 / Tuning.normalDurationSeconds * (Boosting ? Tuning.boostDrain : 1));
        }
        // Called with actual geometric protection, not simply the lamp's on/off state.
        public void ObserveProtection(bool protectedNow, float dt)
        {
            if (dt <= 0) return;
            if (protectedNow)
            {
                protectedSeconds += dt;
                if (protectedSeconds >= Tuning.graceRearmSeconds) graceArmed = true;
            }
            else
            {
                if (wasProtected && graceArmed)
                { GraceRemaining = Tuning.graceSeconds; graceArmed = false; }
                protectedSeconds = 0;
            }
            wasProtected = protectedNow;
        }
    }
}
