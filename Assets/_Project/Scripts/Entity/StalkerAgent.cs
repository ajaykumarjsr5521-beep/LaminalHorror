using System;
using UnityEngine;
using UnityEngine.AI;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// The Stilt-Walker in the scene. Moves on the NavMesh and feeds EntityBrain only what it could perceive: noises from the
    /// NoiseHub (through HearingModel) and a sight check that walls block. It never reads the player's position for anything
    /// except that sight check and the catch distance.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class StalkerAgent : MonoBehaviour
    {
        public NoiseHub Hub;
        public Transform Player;
        public Transform Eye;
        public Transform[] PatrolPoints = new Transform[0];
        public float PlayerWalkSpeed = 3f;
        public float CatchDistance = 1.2f;
        public float StrideMeters = 1.5f;
        public bool PlayerLit = true;   // F-14: replaced by a real light check when lighting data is available
        public AudioClip[] NormalSteps = new AudioClip[0];
        public AudioClip[] WoodSteps = new AudioClip[0];

        public EntityBrain Brain { get; } = new EntityBrain();
        public HearingModel Hearing { get; } = new HearingModel();
        public WoodenLegRhythm Rhythm { get; } = new WoodenLegRhythm();
        public event Action CaughtPlayer;

        NavMeshAgent _agent;
        AudioSource _audio;
        AudioLowPassFilter _lowPass;
        System.Random _rng = new System.Random();
        int _patrolIndex;
        float _wait;
        float _travelled;
        Vector3 _lastPos;
        bool _caught;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f; _audio.minDistance = 2f; _audio.maxDistance = 40f;
            _audio.rolloffMode = AudioRolloffMode.Logarithmic;
            _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            _lastPos = transform.position;
        }

        void OnEnable() { if (Hub != null) Hub.Bus.Emitted += Hearing.Report; }
        void OnDisable() { if (Hub != null) Hub.Bus.Emitted -= Hearing.Report; }

        public void SetSeed(int seed) => _rng = new System.Random(seed);

        void Update()
        {
            if (_caught || !_agent.isOnNavMesh) return;
            float dt = Time.deltaTime, now = Time.time;
            var eye = Eye != null ? Eye.position : transform.position + Vector3.up * 2.2f;

            var input = new BrainInput { Roll = (float)_rng.NextDouble(), Arrived = Arrived() };
            if (Hearing.TryHear(transform.position, now, WallsBetween, out var noise, out var score))
            {
                input.Heard = true; input.NoisePos = noise.Position; input.NoiseScore = score;
                Hearing.MarkHandled(noise.Time);
            }
            if (Player != null)
            {
                var head = Player.position + Vector3.up * 1.4f;
                bool blocked = Physics.Linecast(eye, head, out var hit, ~0, QueryTriggerInteraction.Ignore)
                               && !hit.transform.IsChildOf(Player) && hit.transform != Player;
                if (SightModel.CanSee(eye, head, PlayerLit, blocked)) { input.Sees = true; input.SeenPos = Player.position; }
            }

            var before = Brain.State;
            Brain.Step(dt, input);
            Drive(before, dt);
            Step(dt);

            if (Brain.State == EntityState.Chase && Player != null &&
                Vector3.Distance(transform.position, Player.position) <= CatchDistance)
            {
                _caught = true; _agent.isStopped = true;
                CaughtPlayer?.Invoke();
            }
        }

        bool Arrived() =>
            !_agent.pathPending && _agent.hasPath && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f;

        void Drive(EntityState before, float dt)
        {
            float mult = EntityBrain.SpeedMultiplier(Brain.State);
            _agent.speed = PlayerWalkSpeed * mult;
            _agent.isStopped = mult <= 0f;

            switch (Brain.State)
            {
                case EntityState.Patrol:
                    if (PatrolPoints.Length == 0) break;
                    if (_wait > 0f) { _wait -= dt; _agent.isStopped = true; break; }
                    if (before != EntityState.Patrol || !_agent.hasPath || Arrived())
                    {
                        if (before == EntityState.Patrol && Arrived()) { _wait = 2f + (float)_rng.NextDouble() * 3f; _patrolIndex = (_patrolIndex + 1) % PatrolPoints.Length; }
                        _agent.SetDestination(PatrolPoints[_patrolIndex].position);
                    }
                    break;
                case EntityState.Listen:
                case EntityState.Watch:
                    FaceTowards(Brain.HasTarget ? Brain.Target : (Player != null ? Player.position : transform.position + transform.forward), dt);
                    break;
                case EntityState.Investigate:
                case EntityState.Chase:
                    if (Brain.HasTarget) _agent.SetDestination(Brain.Target);
                    break;
                case EntityState.Search:
                    if (before != EntityState.Search) _agent.ResetPath();
                    if (!_agent.hasPath || Arrived()) _agent.SetDestination(RandomNear(Brain.Target, 5f));
                    break;
            }
            if (!_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f) FaceDirection(_agent.velocity, dt);
        }

        Vector3 RandomNear(Vector3 centre, float radius)
        {
            var p = centre + new Vector3((float)(_rng.NextDouble() * 2 - 1), 0, (float)(_rng.NextDouble() * 2 - 1)) * radius;
            return NavMesh.SamplePosition(p, out var hit, radius, NavMesh.AllAreas) ? hit.position : centre;
        }

        void FaceTowards(Vector3 point, float dt)
        {
            var d = point - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) FaceDirection(d, dt);
        }

        void FaceDirection(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 180f * dt);
        }

        void Step(float dt)
        {
            var delta = transform.position - _lastPos; delta.y = 0f; _lastPos = transform.position;
            if (_agent.isStopped) { _travelled = 0f; return; }
            _travelled += delta.magnitude;
            if (_travelled < StrideMeters) return;
            _travelled -= StrideMeters;
            if (Player == null) return;
            float dist = Vector3.Distance(transform.position, Player.position);
            var step = Rhythm.Next(dist, (float)_rng.NextDouble());
            var bank = step.Foot == Foot.Wood ? WoodSteps : NormalSteps;
            if (bank.Length == 0) return;
            _lowPass.cutoffFrequency = step.LowPassHz;
            _audio.PlayOneShot(bank[step.Variant % bank.Length], step.Gain);
            LastStep = step;
        }

        public EntityStep LastStep { get; private set; }

        /// <summary>Walls between a noise and the entity: one for each solid collider crossed, doors that are open excluded.</summary>
        int WallsBetween(Vector3 from, Vector3 to)
        {
            int walls = 0;
            var hits = Physics.RaycastAll(from + Vector3.up, (to + Vector3.up) - (from + Vector3.up),
                Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (Player != null && h.transform.IsChildOf(Player)) continue;
                if (h.transform.IsChildOf(transform)) continue;
                var door = h.collider.GetComponentInParent<NocturneAnnex.Interaction.Door>();
                if (door != null && door.IsOpen) continue;
                walls++;
            }
            return walls;
        }
    }
}
