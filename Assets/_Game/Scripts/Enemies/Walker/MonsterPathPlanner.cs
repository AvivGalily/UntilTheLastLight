using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public sealed class MonsterPathPlanner
    {
        public Vector3[] Corners { get; private set; } = new Vector3[0];
        int corner;
        // Unity native objects must be created on the main thread, after deserialization.
        NavMeshPath candidate;
        public void Clear() { Corners = new Vector3[0]; corner = 0; }
        public void Plan(Vector3 from, Vector3 lastKnown, LightProtectionService light, MonsterTuning tuning, bool stalking, bool ignoreFlashlight = false)
        {
            if (candidate == null) candidate = new NavMeshPath();
            float best = float.PositiveInfinity;
            Vector3[] selected = null;
            for (int n = 0; n <= tuning.pathCandidates; n++)
            {
                float angle = n * Mathf.PI * 2 / tuning.pathCandidates;
                float ring = stalking ? 12 : 5;
                Vector3 goal = n == 0 ? lastKnown : lastKnown + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * ring;
                if (!NavMesh.SamplePosition(goal, out var hit, 2, NavMesh.AllAreas)) continue;
                if (light.Capsule(hit.position, tuning.radius, tuning.height, ignoreFlashlight) == Protection.Full) continue;
                if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, candidate) || candidate.status != NavMeshPathStatus.PathComplete) continue;
                var points = candidate.corners;
                bool legal = true; float cost = 0;
                for (int j = 1; j < points.Length; j++)
                {
                    if (!light.StepAllowed(points[j - 1], points[j], tuning.radius, tuning.height, lastKnown, false, ignoreFlashlight)
                        || !PhysicalStep(points[j-1],points[j],tuning,LayerMask.GetMask("WorldSolid","LightBlocker"))) { legal = false; break; }
                    cost += Vector3.Distance(points[j - 1], points[j]);
                }
                // Approaches favor progress, watching favors keeping a readable silhouette at distance.
                cost += Mathf.Abs(Vector3.Distance(hit.position, lastKnown) - (stalking ? 12 : 0)) * 3;
                if (legal && cost < best) { best = cost; selected = points; }
            }
            Corners = selected ?? new Vector3[0]; corner = 1;
        }
        public Vector3 Next(Vector3 position, float distance)
        {
            while (corner < Corners.Length && Vector3.Distance(position, Corners[corner]) < .18f) corner++;
            return corner >= Corners.Length ? position : Vector3.MoveTowards(position, Corners[corner], distance);
        }
        public static bool PhysicalStep(Vector3 from, Vector3 to, MonsterTuning tuning, int solids)
        {
            if((to-from).sqrMagnitude<.000001f)return true;
            if (NavMesh.Raycast(from, to, out _, NavMesh.AllAreas)) return false;
            Vector3 delta = to - from;
            Vector3 lower = from + Vector3.up * (tuning.radius + .06f), upper = from + Vector3.up * (tuning.height - tuning.radius);
            return !Physics.CapsuleCast(lower, upper, tuning.radius * .95f, delta.normalized, delta.magnitude, solids, QueryTriggerInteraction.Ignore);
        }
        public static bool StandingClear(Vector3 point,MonsterTuning tuning,int solids)
        {
            return !Physics.CheckCapsule(point+Vector3.up*(tuning.radius+.06f),point+Vector3.up*(tuning.height-tuning.radius),tuning.radius*.95f,solids,QueryTriggerInteraction.Ignore);
        }
    }
}
