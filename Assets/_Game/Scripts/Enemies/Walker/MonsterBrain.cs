using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public enum MonsterState { Dormant, Watching, Stalking, Approaching, Hunting, AttackWindup, Repelled, RetreatingToFog, Dead }
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
        public bool RetreatingToFog {get;private set;}
        public float FogFade {get;private set;}
        public float FlashlightExposure {get;private set;}
        public float FlashlightTolerance {get;private set;}
        bool resistingFlashlight;
        Renderer[] fogSurfaces;
        UnityEngine.Rendering.ShadowCastingMode[] shadowModes;
        MaterialPropertyBlock fogBlock;
        NavMeshPath escapePath;
        int escapeCorner;
        public float WindupDuration {get;private set;}=3;
        float planning, cooldown, huntTime, retreatTime, life;
        float blockedTime;
        public int Detours {get;private set;}
        public bool NavigationReady {get;private set;}
        int patrol;
        public void Initialize(Vector3[] spawns, PlayerMotor player, LightProtectionService light, MonsterTuning tuning, bool restore)
        {
            Relentless=RetreatingToFog=false;escapePath=null;gameObject.SetActive(false); HasSpawned = false; State = MonsterState.Dormant;
            Senses.Reset(); Planner.Clear(); Attack.ResetEncounter();
            ResetLightResponse(tuning);
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
                transform.position = nav.position;Distance=d;SetFogFade(Mathf.InverseLerp(24,tuning.fogRetreatDistance,d));HasSpawned = true;NavigationReady=true; gameObject.SetActive(true);
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
            FlashlightExposure=0;resistingFlashlight=false;
            Attack.Cancel();Planner.Clear();Senses.Reset();RetreatingToFog=false;escapePath=null;HasSpawned=false;ActualSpeed=0;Distance=100;
            foreach(var source in GetComponentsInChildren<AudioSource>())source.Stop();
            Transition(MonsterState.Dormant,"vanished out of view");gameObject.SetActive(false);
        }
        public void Tick(float dt, GameSession game)
        {
            if(dt<=0)return;
            Vector3 before=transform.position;SimulateBrain(dt,game);
            ActualSpeed=Vector3.Distance(before,transform.position)/dt;
            bool scripted=game.forestEncounter&&game.forestEncounter.OwnsMonster;
            float distance=Vector3.Distance(transform.position,game.player.transform.position);
            SetFogFade(game.mechanicsTest||scripted?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(24,game.tuning.monster.fogRetreatDistance,distance)));
        }
        void SetFogFade(float value)
        {
            FogFade=value;
            if(fogSurfaces==null)
            {
                fogSurfaces=GetComponentsInChildren<Renderer>(true);shadowModes=new UnityEngine.Rendering.ShadowCastingMode[fogSurfaces.Length];fogBlock=new MaterialPropertyBlock();
                for(int i=0;i<fogSurfaces.Length;i++)shadowModes[i]=fogSurfaces[i].shadowCastingMode;
            }
            for(int i=0;i<fogSurfaces.Length;i++)
            {
                var r=fogSurfaces[i];r.GetPropertyBlock(fogBlock);fogBlock.SetFloat("_FogFade",value);r.SetPropertyBlock(fogBlock);
                r.shadowCastingMode=value>0?UnityEngine.Rendering.ShadowCastingMode.Off:shadowModes[i];
            }
        }
        public void BeginEncounter(Vector3 position,Vector3 target)
        {
            gameObject.SetActive(false);transform.position=position;HasSpawned=true;ActualSpeed=0;
            Attack.ResetEncounter();Planner.Clear();Senses.Reset();Senses.Remember(target,28);
            FlashlightExposure=FlashlightTolerance=0;resistingFlashlight=false;
            planning=huntTime=retreatTime=blockedTime=0;RetreatingToFog=false;escapePath=null;cooldown=2;NavigationReady=true;Distance=Vector3.Distance(position,target);EncounterCount++;
            // Scripted leap/feeding can own a new encounter before Tick runs.
            // The normal navigation tick applies distant mist concealment.
            SetFogFade(0);
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
            if(FlashlightTolerance<=0)ResetLightResponse(tuning);
            bool shelterLight=light.Capsule(transform.position,tuning.radius,tuning.height,true)==Protection.Full;
            if(protection==Protection.Full&&!shelterLight&&!safe)FlashlightExposure+=dt;
            else FlashlightExposure=0;
            resistingFlashlight=!shelterLight&&FlashlightExposure<FlashlightTolerance;
            if(resistingFlashlight)protection=Protection.None;
            bool pursuing=game.ThreatEnabled&&!game.mechanicsTest&&!safe;
            if(Relentless||pursuing)Senses.Remember(playerFeet,28);
            game.Flashlight.ObserveProtection(protection != Protection.None || safe, dt);
            if(RetreatingToFog&&!safe&&light.PointBlank(transform.position))
            {RetreatingToFog=false;escapePath=null;cooldown=0;}
            // Light wins this encounter: continue escaping after leaving the cone,
            // rather than immediately returning to the beam's outer edge.
            if(protection==Protection.Full&&!safe&&!game.mechanicsTest&&!(game.forestEncounter&&game.forestEncounter.OwnsMonster))RetreatingToFog=true;
            if(RetreatingToFog)
            {
                Attack.Cancel();Planner.Clear();EverRepelled=true;Transition(MonsterState.RetreatingToFog,"escaping light into fog");
                if(body)body.localRotation=Quaternion.identity;
                if(protection!=Protection.None)Retreat(dt,game);else EscapeToFog(dt,game);
                return;
            }
            if (protection == Protection.Full || retreatTime > 0)
            {
                Attack.Cancel(); Planner.Clear(); EverRepelled = true;
                Transition(MonsterState.Repelled, "illuminated or hit");
                Retreat(dt, game); return;
            }
            if (State == MonsterState.Repelled)
            { cooldown = Mathf.Max(cooldown, 2); Transition(MonsterState.Watching, "left protection"); }
            bool attackLegal = !safe && Senses.ObservedNow
                && !light.AttackBlocked(transform.position, tuning.radius, tuning.height, playerFeet + Vector3.up * 1.3f,resistingFlashlight)
                && game.ThreatEnabled;
            bool warning = Distance <= tuning.warningRange && Senses.ObservedNow;
            bool strike = Attack.Tick(dt, warning, attackLegal, Distance < tuning.attackRange, tuning);
            if (Attack.WindingUp)
            {
                Transition(MonsterState.AttackWindup, "warned and in range");
                if (body) body.localRotation = Quaternion.Euler(-12 * Mathf.Clamp01(Attack.WindupElapsed / tuning.windupSeconds), 0, 0);
                if (strike && !light.AttackBlocked(transform.position, tuning.radius, tuning.height, playerFeet + Vector3.up * 1.3f,resistingFlashlight))
                {
                    game.creature?.CloseOnContact();game.ReceiveAttack(true);
                    Attack.Cancel(); retreatTime = 4; cooldown = tuning.encounterCooldown;
                    return;
                }
            }
            else if (body) body.localRotation = Quaternion.identity;
            if (Distance > 16) Attack.ResetEncounter();
            if(!safe&&cooldown<=0&&!Senses.HasMemory&&!game.mechanicsTest){Patrol(dt,game);return;}
            if (safe || cooldown > 0 && !Attack.WindingUp || !Senses.HasMemory)
            { Transition(MonsterState.Watching, "shelter, respite or no memory"); return; }
            if (game.Flashlight.GraceRemaining > 0 && !Attack.WindingUp) { Transition(MonsterState.Watching, "power-loss grace"); return; }
            // First encounter cannot strike; it holds at a clearly visible warning distance.
            if (!game.ThreatEnabled && Distance < 7) { Transition(MonsterState.Watching, "tutorial limit"); return; }
            MonsterState desired = !game.ThreatEnabled ? MonsterState.Stalking : Relentless || Distance < 10 && Senses.ObservedNow ? MonsterState.Hunting : MonsterState.Approaching;
            if (desired == MonsterState.Hunting)
            {
                huntTime += dt;
                if (!Relentless && !pursuing && huntTime >= tuning.huntSeconds) { cooldown = tuning.encounterCooldown; huntTime = 0; Transition(MonsterState.Watching, "hunt timeout"); return; }
            }
            else huntTime = 0;
            if(!Attack.WindingUp)Transition(desired, "sensory target");
            planning -= dt;
            if (planning <= 0)
            {
                planning = 1 / tuning.planningHz;
                Planner.Plan(transform.position, Senses.LastKnown, light, tuning, !game.ThreatEnabled,resistingFlashlight);
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
        void ResetLightResponse(MonsterTuning tuning)
        {
            FlashlightExposure=0;resistingFlashlight=true;
            FlashlightTolerance=Random.Range(tuning.flashlightMinSeconds,tuning.flashlightMaxSeconds);
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
        void EscapeToFog(float dt,GameSession game)
        {
            var t=game.tuning.monster;Vector3 from=transform.position,target=game.player.transform.position;
            if(escapePath==null||escapeCorner>=escapePath.corners.Length||Vector3.Distance(escapePath.corners[escapePath.corners.Length-1],target)<t.fogRetreatDistance-2)
            {
                escapePath=null;Vector3 away=Vector3.ProjectOnPlane(from-target,Vector3.up).normalized;
                foreach(float angle in new[]{0f,30f,-30f,60f,-60f,90f,-90f,120f,-120f})
                {
                    Vector3 candidate=target+Quaternion.Euler(0,angle,0)*away*(t.fogRetreatDistance+3);
                    if(!NavMesh.SamplePosition(candidate,out var nav,2,NavMesh.AllAreas)||Vector3.Distance(nav.position,target)<t.fogRetreatDistance)continue;
                    if(!MonsterPathPlanner.StandingClear(nav.position,t,game.SolidMask)||game.Protection.Capsule(nav.position,t.radius,t.height)!=Protection.None)continue;
                    var route=new NavMeshPath();
                    if(!NavMesh.CalculatePath(from,nav.position,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete||route.corners.Length<2)continue;
                    // Never pick a route that starts by running back towards the player.
                    if(Vector3.Dot(route.corners[1]-from,away)<-.1f)continue;
                    escapePath=route;escapeCorner=1;break;
                }
            }
            if(escapePath==null){Retreat(dt,game);return;}
            var points=escapePath.corners;
            while(escapeCorner<points.Length&&Vector3.Distance(from,points[escapeCorner])<.15f)escapeCorner++;
            if(escapeCorner>=points.Length)return;
            if(!Move(Vector3.MoveTowards(from,points[escapeCorner],t.retreatSpeed*dt),game,true)){escapePath=null;Retreat(dt,game);}
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
            if (!game.Protection.StepAllowed(from, next, t.radius, t.height, game.player.transform.position, retreat,!retreat&&resistingFlashlight)
                || !MonsterPathPlanner.PhysicalStep(from, next, t, game.SolidMask)) { planning = 0; return false; }
            Vector3 direction = next - from;
            transform.position = next;
            if (direction.sqrMagnitude > .00001f) transform.rotation = Quaternion.LookRotation(direction.normalized);
            return direction.sqrMagnitude>.000001f;
        }
    }
}
