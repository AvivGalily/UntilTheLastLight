using UnityEngine;

namespace Game
{
    public enum Protection { None, Edge, Full }
    public sealed class LightProtectionService
    {
        public FlashlightModel Flashlight;
        public Transform Beam;
        public SafeLight[] Shelters;
        public int BlockerMask;
        public float MinimumRepelDistance;
        public bool PointBlank(Vector3 feet)
        {var offset=feet-Beam.position;offset.y=0;return MinimumRepelDistance>0&&offset.sqrMagnitude<MinimumRepelDistance*MinimumRepelDistance;}
        public LightProtectionService(FlashlightModel light, Transform beam, SafeLight[] shelters, int mask)
        { Flashlight = light; Beam = beam; Shelters = shelters; BlockerMask = mask; }
        public bool Clear(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            return !Physics.Raycast(from, delta.normalized, delta.magnitude, BlockerMask, QueryTriggerInteraction.Ignore);
        }
        public bool InShelter(Vector3 point)
        {
            foreach (var s in Shelters)
                if (s && s.Contains(point) && Clear(s.transform.position, point)) return true;
            return false;
        }
        public Protection Sample(Vector3 point, bool ignoreFlashlight = false)
        {
            if (InShelter(point)) return Protection.Full;
            if (ignoreFlashlight) return Protection.None;
            if (MinimumRepelDistance>0&&PointBlank(point)) return Protection.None;
            if (!Flashlight.Active || !Beam) return Protection.None;
            Vector3 offset = point - Beam.position;
            float d = offset.magnitude;
            // The light origin is not an omnidirectional shield (A26).
            if (d < 0.05f || d > Flashlight.Range || Vector3.Dot(offset, Beam.forward) <= 0) return Protection.None;
            float angle = Vector3.Angle(Beam.forward, offset);
            if (angle > Flashlight.Tuning.outerHalfAngle || !Clear(Beam.position, point)) return Protection.None;
            return angle <= Flashlight.Tuning.innerHalfAngle && d <= Flashlight.Range - Flashlight.Tuning.edgeMeters
                ? Protection.Full : Protection.Edge;
        }
        public Protection Capsule(Vector3 feet, float radius, float height, bool ignoreFlashlight = false)
        {
            if(PointBlank(feet))return InShelter(feet+Vector3.up*height*.6f)?Protection.Full:Protection.None;
            Protection result = Protection.None;
            for (int h = 0; h < 3; h++)
            {
                Vector3 p = feet + Vector3.up * (h == 0 ? radius : h == 1 ? height * .60f : height - radius);
                for (int side = 0; side < 5; side++)
                {
                    Vector3 edge = side == 0 ? Vector3.zero : side == 1 ? Vector3.right * radius : side == 2 ? Vector3.left * radius : side == 3 ? Vector3.forward * radius : Vector3.back * radius;
                    Protection sample = Sample(p + edge, ignoreFlashlight);
                    if (sample > result) result = sample;
                }
            }
            return result;
        }
        public float Exposure(Vector3 feet, float radius, float height)
        {
            if(PointBlank(feet))return InShelter(feet+Vector3.up*height*.6f)?30:0;
            float sum = 0;
            for (int h = 0; h < 3; h++)
            {
                Vector3 p = feet + Vector3.up * (h == 0 ? radius : h == 1 ? height * .60f : height - radius);
                for (int j = 0; j < 5; j++)
                {
                    Vector3 point = p + (j == 0 ? Vector3.zero : new Vector3(Mathf.Cos(j * Mathf.PI / 2), 0, Mathf.Sin(j * Mathf.PI / 2)) * radius);
                    if (InShelter(point)) { sum += 2; continue; }
                    if (Sample(point) == Protection.None) continue;
                    Vector3 delta = point - Beam.position;
                    sum += .1f + Mathf.Clamp01(1 - delta.magnitude / Mathf.Max(.01f, Flashlight.Range))
                        + Mathf.Clamp01(1 - Vector3.Angle(Beam.forward, delta) / Flashlight.Tuning.outerHalfAngle);
                }
            }
            return sum;
        }
        public bool StepAllowed(Vector3 from, Vector3 to, float radius, float height, Vector3 player, bool retreat, bool ignoreFlashlight = false)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / (radius * .5f)));
            float previousExposure = Exposure(from, radius, height);
            float previousDistance = Vector3.Distance(from, player);
            bool exiting = retreat && previousExposure > 0;
            for (int n = 1; n <= count; n++)
            {
                Vector3 p = Vector3.Lerp(from, to, (float)n / count);
                if (exiting)
                {
                    float e = Exposure(p, radius, height), d = Vector3.Distance(p, player);
                    if (e > previousExposure + .0001f || d < previousDistance - .0001f) return false;
                    if (e > 0 && e >= previousExposure - .000001f) return false;
                    previousExposure = e; previousDistance = d;
                    if (e == 0) exiting = false;
                }
                else if (Capsule(p, radius, height, ignoreFlashlight) == Protection.Full) return false;
            }
            return true;
        }
        public bool AttackBlocked(Vector3 monsterFeet, float radius, float height, Vector3 playerChest, bool ignoreFlashlight = false)
        {
            if(PointBlank(monsterFeet))return InShelter(playerChest)||InShelter(monsterFeet+Vector3.up*height*.6f)||!Clear(monsterFeet+Vector3.up*height*.6f,playerChest);
            if (InShelter(playerChest) || Capsule(monsterFeet, radius, height, ignoreFlashlight) != Protection.None) return true;
            Vector3 start = monsterFeet + Vector3.up * height * .6f;
            if (!Clear(start, playerChest)) return true;
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, playerChest) / (radius * .5f)));
            for (int i = 0; i < samples; i++)
            {
                Vector3 point = Vector3.Lerp(start, playerChest, (float)i / samples);
                // End at the player's body; never cross the lamp origin and sample in front of it.
                if (Vector3.Distance(point, playerChest) > .35f && Sample(point, ignoreFlashlight) != Protection.None) return true;
            }
            return false;
        }
    }
}
