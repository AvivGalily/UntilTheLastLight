using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public enum MonsterState { Dormant, Watching, Stalking, Approaching, Hunting, AttackWindup, Repelled, Dead }
    public sealed class MonsterBrain : MonoBehaviour
    {
        public Transform body;
        public AudioSource breath;
        public AudioSource warningSource;
        public AudioClip warningClip;
        public MonsterState State { get; private set; } = MonsterState.Dormant;
        public readonly MonsterSenses Senses = new MonsterSenses();
        public readonly MonsterAttack Attack = new MonsterAttack();
        public readonly MonsterPathPlanner Planner = new MonsterPathPlanner();
        public readonly Queue<string> Trace = new Queue<string>();
        public bool HasSpawned { get; private set; }
        public bool EverRepelled { get; private set; }
        public float Distance { get; private set; } = 100;
        public float ActualSpeed {get;private set;}
        public int EncounterCount {get;private set;}
        public bool Relentless {get;set;}
        public float WindupDuration {get;private set;}=1.2f;
        float planning, cooldown, huntTime, retreatTime, life;
        float blockedTime;
        public int Detours {get;private set;}
        public bool NavigationReady {get;private set;}
        int patrol;
        public void Initialize(Vector3[] spawns, PlayerMotor player, LightProtectionService light, MonsterTuning tuning, bool restore)
        {
            Relentless=false;gameObject.SetActive(false); HasSpawned = false; State = MonsterState.Dormant;
            Senses.Reset(); Planner.Clear(); Attack.ResetEncounter();
            planning=huntTime=retreatTime=life=blockedTime=0;Distance=100;ActualSpeed=0;EncounterCount=Detours=0;patrol=0;NavigationReady=false;
            foreach (Vector3 point in spawns)
            {
                float d = Vector3.Distance(player.transform.position, point);
                if (d < tuning.spawnMin || d > tuning.spawnMax) continue;
                if (!NavMesh.SamplePosition(point, out var nav, 2, NavMesh.AllAreas)) continue;
                if(!MonsterPathPlanner.StandingClear(nav.position,tuning,LayerMask.GetMask("WorldSolid","LightBlocker")))continue;
                var initialPath=new NavMeshPath();
                if(!NavMesh.CalculatePath(nav.position,player.transform.position,NavMesh.AllAreas,initialPath)||initialPath.status!=NavMeshPathStatus.PathComplete)continue;
                if (light.Capsule(nav.position, tuning.radius, tuning.height) != Protection.None) continue;
                bool inView = Vector3.Angle(player.view.transform.forward, nav.position + Vector3.up * 1.5f - player.view.transform.position) < player.view.fieldOfView * .8f;
                if (inView && light.Clear(player.view.transform.position, nav.position + Vector3.up * 1.5f)) continue;
                // One placement at creation/load; never called as an AI movement strategy.
                transform.position = nav.position; HasSpawned = true;NavigationReady=true; gameObject.SetActive(true);
                float closest=float.PositiveInfinity;for(int i=0;i<ForestLayout.Patrol.Length;i++){float near=Vector3.Distance(transform.position,ForestLayout.Patrol[i]);if(near<closest){closest=near;patrol=i;}}
                cooldown = restore ? tuning.restoreGraceSeconds : 2;EncounterCount=1;Senses.Remember(player.transform.position,25);Transition(MonsterState.Watching, "initial spawn"); break;
            }
        }
        void Transition(MonsterState state, string reason)
        {
            if (State == state) return;
            State = state;
            if(state==MonsterState.AttackWindup && warningSource && warningClip) warningSource.PlayOneShot(warningClip);
            if (Trace.Count >= 32) Trace.Dequeue();
            Trace.Enqueue($"{life:F2} {state}: {reason}");
        }
        public void RestoreTutorial(bool value) { EverRepelled=value; }
        public void Disappear()
        {
            Attack.Cancel();Planner.Clear();Senses.Reset();HasSpawned=false;ActualSpeed=0;Distance=100;
            foreach(var source in GetComponentsInChildren<AudioSource>())source.Stop();
            Transition(MonsterState.Dormant,"vanished out of view");gameObject.SetActive(false);
        }
        public void Tick(float dt, GameSession game)
        {
            if(dt<=0)return;
            Vector3 before=transform.position;SimulateBrain(dt,game);
            ActualSpeed=Vector3.Distance(before,transform.position)/dt;
        }
        public void BeginEncounter(Vector3 position,Vector3 target)
        {
            gameObject.SetActive(false);transform.position=position;HasSpawned=true;ActualSpeed=0;
            Attack.ResetEncounter();Planner.Clear();Senses.Reset();Senses.Remember(target,28);
            planning=huntTime=retreatTime=blockedTime=0;cooldown=2;NavigationReady=true;Distance=Vector3.Distance(position,target);EncounterCount++;
            gameObject.SetActive(true);if(breath&&breath.clip)breath.Play();Transition(MonsterState.Approaching,"distant return");
        }
        // Cinematic contact can leave the wide collision capsule outside the navigation surface.
        // Resolve this once, at ownership handoff, to a clear connected point beside the body.
        public bool ResumePursuit(GameSession game)
        {
            var path=new NavMeshPath();Vector3 origin=transform.position,target=game.player.transform.position;
            var t=game.tuning.monster;float best=float.PositiveInfinity;Vector3 selected=origin;bool found=false;
            for(int ring=0;ring<=6;ring++)for(int n=0;n<(ring==0?1:24);n++)
            {
                float angle=n*Mathf.PI*2/24;Vector3 candidate=origin+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*ring*.65f;
                if(!NavMesh.SamplePosition(candidate,out var nav,.45f,NavMesh.AllAreas))continue;
                if(!MonsterPathPlanner.StandingClear(nav.position,t,game.SolidMask))continue;
                if(!NavMesh.CalculatePath(nav.position,target,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete||path.corners.Length<2)continue;
                if(!MonsterPathPlanner.PhysicalStep(nav.position,Vector3.MoveTowards(nav.position,path.corners[1],.3f),t,game.SolidMask))continue;
                float cost=Vector3.Distance(origin,nav.position)+Vector3.Distance(nav.position,target)*.03f;
                if(cost<best){best=cost;selected=nav.position;found=true;}
            }
            if(!found){NavigationReady=false;return false;}
            BeginEncounter(selected,target);cooldown=.35f;Relentless=true;NavigationReady=true;return true;
        }
        void SimulateBrain(float dt, GameSession game)
        {
            if (dt <= 0 || !HasSpawned || State == MonsterState.Dead) return;
            life += dt;
            var tuning = game.tuning.monster; var light = game.Protection; var player = game.player;
            WindupDuration=tuning.windupSeconds;
            Vector3 playerFeet = player.transform.position;
            Distance = Vector3.Distance(transform.position, playerFeet);
            cooldown = Mathf.Max(0, cooldown - dt); retreatTime = Mathf.Max(0, retreatTime - dt);
            Senses.Tick(dt, transform.position, player, tuning, light);
            Protection protection = light.Capsule(transform.position, tuning.radius, tuning.height);
            bool safe = light.InShelter(player.view.transform.position);
            bool pursuing=game.ThreatEnabled&&!game.mechanicsTest&&!safe;
            if(Relentless||pursuing)Senses.Remember(playerFeet,28);
            game.Flashlight.ObserveProtection(protection != Protection.None || safe, dt);
            if (protection == Protection.Full || retreatTime > 0)
            {
                Attack.Cancel(); Planner.Clear(); EverRepelled = true;
                Transition(MonsterState.Repelled, "illuminated or hit");
                Retreat(dt, game); return;
            }
            if (State == MonsterState.Repelled)
            { cooldown = Mathf.Max(cooldown, 2); Transition(MonsterState.Watching, "left protection"); }
            bool attackLegal = !safe && cooldown <= 0 && game.Flashlight.GraceRemaining <= 0 && Senses.ObservedNow
                && !light.AttackBlocked(transform.position, tuning.radius, tuning.height, playerFeet + Vector3.up * 1.3f)
                && game.Condition.Invulnerability <= 0 && game.ThreatEnabled;
            bool warning = Distance <= tuning.warningRange && Senses.ObservedNow;
            bool strike = Attack.Tick(dt, warning, attackLegal, Distance <= tuning.attackRange, tuning);
            if (Attack.WindingUp)
            {
                Transition(MonsterState.AttackWindup, "warned and in range");
                if (body) body.localRotation = Quaternion.Euler(-12 * Mathf.Clamp01(Attack.WindupElapsed / tuning.windupSeconds), 0, 0);
                if (strike && !light.AttackBlocked(transform.position, tuning.radius, tuning.height, playerFeet + Vector3.up * 1.3f))
                {
                    game.creature?.CloseOnContact();game.ReceiveAttack(true);
                    Attack.Cancel(); retreatTime = 4; cooldown = tuning.encounterCooldown;
                }
                return;
            }
            if (body) body.localRotation = Quaternion.identity;
            if (Distance > 16) Attack.ResetEncounter();
            if(!safe&&cooldown<=0&&!Senses.HasMemory&&!game.mechanicsTest){Patrol(dt,game);return;}
            if (safe || cooldown > 0 || !Senses.HasMemory)
            { Transition(MonsterState.Watching, "shelter, respite or no memory"); return; }
            if (game.Flashlight.GraceRemaining > 0) { Transition(MonsterState.Watching, "power-loss grace"); return; }
            // First encounter cannot strike; it holds at a clearly visible warning distance.
            if (!game.ThreatEnabled && Distance < 7) { Transition(MonsterState.Watching, "tutorial limit"); return; }
            MonsterState desired = !game.ThreatEnabled ? MonsterState.Stalking : Relentless || Distance < 10 && Senses.ObservedNow ? MonsterState.Hunting : MonsterState.Approaching;
            if (desired == MonsterState.Hunting)
            {
                huntTime += dt;
                if (!Relentless && !pursuing && huntTime >= tuning.huntSeconds) { cooldown = tuning.encounterCooldown; huntTime = 0; Transition(MonsterState.Watching, "hunt timeout"); return; }
            }
            else huntTime = 0;
            Transition(desired, "sensory target");
            planning -= dt;
            if (planning <= 0)
            {
                planning = 1 / tuning.planningHz;
                Planner.Plan(transform.position, Senses.LastKnown, light, tuning, !game.ThreatEnabled);
            }
            float speed = desired == MonsterState.Hunting ? tuning.huntSpeed : desired == MonsterState.Stalking ? tuning.stalkSpeed : tuning.approachSpeed;
            Vector3 next = Planner.Next(transform.position, speed * dt);
            if(Move(next, game, false))blockedTime=0;
            else
            {
                blockedTime+=dt;
                // A short legal side step clears a corner; it never warps a visible pursuer.
                if(blockedTime>.4f&&TryDetour(speed*dt,game)){blockedTime=0;Detours++;}
            }
        }
        void Patrol(float dt,GameSession game)
        {
            // Physical exploration of the expanded trail network, without tracking an unseen player.
            Vector3 target=ForestLayout.Patrol[patrol];
            if(Vector3.Distance(transform.position,target)<6){patrol=(patrol+1)%ForestLayout.Patrol.Length;target=ForestLayout.Patrol[patrol];planning=0;}
            Transition(MonsterState.Stalking,"searching service trails");planning-=dt;
            if(planning<=0){planning=1/game.tuning.monster.planningHz;Planner.Plan(transform.position,target,game.Protection,game.tuning.monster,false);}
            Move(Planner.Next(transform.position,game.tuning.monster.approachSpeed*dt),game,false);
        }
        void Retreat(float dt, GameSession game)
        {
            var tuning = game.tuning.monster;
            Vector3 from = transform.position;
            Vector3 away = from - game.player.transform.position; away.y = 0; away.Normalize();
            float best = float.NegativeInfinity; Vector3 destination = from;
            for (int n = 0; n < 12; n++)
            {
                Vector3 direction = Quaternion.Euler(0, n * 30, 0) * away;
                Vector3 candidate = from + direction * tuning.retreatSpeed * dt;
                if (!NavMesh.SamplePosition(candidate, out var hit, .35f, NavMesh.AllAreas)) continue;
                candidate = hit.position;
                if (Vector3.Distance(candidate, from) > tuning.retreatSpeed * dt + .04f) continue;
                if (!game.Protection.StepAllowed(from, candidate, tuning.radius, tuning.height, game.player.transform.position, true)) continue;
                if (!MonsterPathPlanner.PhysicalStep(from, candidate, tuning, game.SolidMask)) continue;
                float score = Vector3.Distance(candidate, game.player.transform.position) - game.Protection.Exposure(candidate, tuning.radius, tuning.height);
                if (score > best) { best = score; destination = candidate; }
            }
            Move(destination, game, true);
        }
        bool TryDetour(float distance,GameSession game)
        {
            Vector3 toward=Vector3.ProjectOnPlane(game.player.transform.position-transform.position,Vector3.up).normalized;
            foreach(float angle in new[]{35f,-35f,65f,-65f,90f,-90f,120f,-120f})
            {
                Vector3 candidate=transform.position+Quaternion.Euler(0,angle,0)*toward*distance;
                if(!NavMesh.SamplePosition(candidate,out var nav,.10f,NavMesh.AllAreas)||Vector3.Distance(nav.position,transform.position)>distance+.02f)continue;
                if(Move(nav.position,game,false)){planning=0;return true;}
            }
            return false;
        }
        bool Move(Vector3 next, GameSession game, bool retreat)
        {
            var t = game.tuning.monster; Vector3 from = transform.position;
            if (!game.Protection.StepAllowed(from, next, t.radius, t.height, game.player.transform.position, retreat)
                || !MonsterPathPlanner.PhysicalStep(from, next, t, game.SolidMask)) { planning = 0; return false; }
            Vector3 direction = next - from;
            transform.position = next;
            if (direction.sqrMagnitude > .00001f) transform.rotation = Quaternion.LookRotation(direction.normalized);
            return direction.sqrMagnitude>.000001f;
        }
    }
}
