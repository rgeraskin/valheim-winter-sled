using System;
using System.Collections.Generic;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// Sled physics on top of the vanilla Vagon (which still handles pulling, storage and mass).
    ///
    /// The runners carry a frictionless physics material and this component applies the
    /// friction itself, separately along and across each runner: along the runner it depends
    /// on the surface (almost nothing on snow and ice, a lot on bare ground), across it the
    /// runners bite and keep the sled on its line, which is what makes a sled feel like a sled
    /// rather than a puck. It runs only on the ZDO owner, like the rest of Vagon's physics.
    /// </summary>
    internal class SledController : MonoBehaviour
    {
        public const string RunnerPrefix = "runner_";

        private const float FrictionSolverScale = 0.8f;
        private const float ProbeInterval = 0.2f;
        private const float SprayInterval = 0.12f;
        private const float TrailStep = 1f;
        // Stability at 1: righting (1/s² per radian of tilt) and damping (1/s), a little under critical
        private const float StabilitySpring = 40f;
        private const float StabilityDamping = 9f;

        private static readonly HashSet<SledController> s_instances = new HashSet<SledController>();

        /// <summary>Every sled loaded in the world (for settings changed in game).</summary>
        public static IEnumerable<SledController> Instances => s_instances;

        /// <summary>The sled the local player is pulling, for the hauling status effect.</summary>
        public static SledController LocalPulled { get; private set; }

        public Surface Surface { get; private set; } = Surface.Ground;

        /// <summary>The surface as the player is told about it: steady where <see cref="Surface"/> flickers.</summary>
        public Surface ShownSurface => m_shownSurface.Shown;
        private readonly SurfaceHold m_shownSurface = new SurfaceHold();

        /// <summary>The runners touch something (for sound): contacts on the owner, a probe elsewhere.</summary>
        public bool Touching => m_nview != null && m_nview.IsValid() && m_nview.IsOwner() ? OnSolidGround : m_probeHit;
        private bool m_probeHit;

        /// <summary>The runners rest on something solid (not floating): ground, snow, ice, a floor.</summary>
        public bool OnSolidGround => Time.fixedTime - m_lastContactTime < 0.1f;
        private float m_lastContactTime = -1f;
        public float Speed { get; private set; }

        private ZNetView m_nview;
        private Vagon m_vagon;
        private Rigidbody m_body;
        private Floating m_floating;
        private SledSeat m_seat;
        private readonly HashSet<Collider> m_runners = new HashSet<Collider>();
        private readonly List<Transform> m_runnerTails = new List<Transform>();
        private PhysicsMaterial m_slideMaterial;
        private PhysicsMaterial m_restMaterial;
        private bool m_resting;

        private struct Contact
        {
            public Vector3 Point;
            public Vector3 Normal;
            public Surface Surface;
        }

        private readonly List<Contact> m_contacts = new List<Contact>();
        private readonly List<ContactPoint> m_contactBuffer = new List<ContactPoint>(32);
        private readonly int[] m_surfaceVotes = new int[Enum.GetValues(typeof(Surface)).Length];

        // rider input; on the owner it arrives by RPC, on the rider it is also kept locally
        private Vector3 m_controls;
        private long m_lastOwner;
        private float m_controlsTime;
        private Vector3 m_sentControls;
        private bool m_pushHeld;
        private float m_nextPush;
        private const float PushInterval = 0.35f;
        private float m_sentTime;

        private float m_probeTimer;
        private float m_sprayTimer;
        private Vector3 m_lastTrailPos;
        // what "level" is for the sled: the ground under the runners, smoothed
        private Vector3 m_groundUp = Vector3.up;
        private float m_debugTimer;
        private float m_debugAlong, m_debugAcross, m_debugTime;

        private static GameObject s_sprayPrefab;
        private static GameObject s_trailPrefab;

        private void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            m_vagon = GetComponent<Vagon>();
            m_body = GetComponent<Rigidbody>();
            m_floating = GetComponent<Floating>();
            m_seat = GetComponentInChildren<SledSeat>(true);
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                if (col.name.StartsWith(RunnerPrefix))
                {
                    m_runners.Add(col);
                }
            }
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "runner_tail")
                {
                    m_runnerTails.Add(t);
                }
            }
            if (m_nview == null || m_nview.GetZDO() == null)
            {
                enabled = false;
                return;
            }

            // a little behind the middle and down by the runners, so a bump that lifts one runner doesn't tip the sled
            m_body.centerOfMass = new Vector3(0f, 0.12f, -0.08f);
            m_body.linearDamping = 0.02f;
            m_body.angularDamping = 0.6f;

            m_slideMaterial = new PhysicsMaterial("sled_runner_slide")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            m_restMaterial = new PhysicsMaterial("sled_runner_resting")
            {
                dynamicFriction = 1f,
                staticFriction = 1f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            SetResting(true);
            m_lastTrailPos = transform.position;
        }

        private void Start()
        {
            if (m_nview != null && m_nview.IsValid())
            {
                m_nview.Register<Vector3>("WinterSled_Controls", RPC_Controls);
                m_nview.Register("WinterSled_Push", RPC_Push);
            }
        }

        private void OnEnable()
        {
            s_instances.Add(this);
        }

        private void OnDisable()
        {
            s_instances.Remove(this);
        }

        private void OnDestroy()
        {
            if (LocalPulled == this)
            {
                LocalPulled = null;
            }
        }

        public bool IsPulled()
        {
            return m_vagon != null && m_vagon.IsAttached();
        }

        public bool HasRider()
        {
            return m_seat != null && m_seat.HaveValidRider();
        }

        public float CargoWeight()
        {
            return m_vagon.m_container != null ? m_vagon.m_container.GetInventory().GetTotalWeight() : 0f;
        }

        // ---------------------------------------------------------------- rider input

        /// <summary>Called on the rider's client by the seat with the player's movement input.</summary>
        public void SetLocalControls(Vector3 moveDir)
        {
            m_controls = moveDir;
            m_controlsTime = Time.time;
            // one push-off per press of W, paid in stamina; holding it doesn't drive the sled
            bool forward = moveDir.z > 0.5f;
            if (forward && !m_pushHeld && Time.time >= m_nextPush)
            {
                TryPush();
            }
            m_pushHeld = forward;
            if (m_nview.IsOwner())
            {
                return;
            }
            // like a ship's rudder: tell the owner, but not every frame
            if ((moveDir - m_sentControls).sqrMagnitude > 0.01f || Time.time - m_sentTime > 0.25f)
            {
                m_sentControls = moveDir;
                m_sentTime = Time.time;
                m_nview.InvokeRPC("WinterSled_Controls", moveDir);
            }
        }

        private void TryPush()
        {
            var player = Player.m_localPlayer;
            // a push needs something slippery under the runners and some breath
            if (player == null || IsPulled() || !SurfaceProbe.IsSnowy(ShownSurface))
            {
                return;
            }
            float cost = Plugin.Cfg.PushStamina.Value;
            if (!player.HaveStamina(cost))
            {
                Hud.instance?.StaminaBarEmptyFlash();
                return;
            }
            player.UseStamina(cost);
            m_nextPush = Time.time + PushInterval;
            if (m_nview.IsOwner())
            {
                Push();
            }
            else
            {
                m_nview.InvokeRPC("WinterSled_Push");
            }
        }

        private void RPC_Push(long sender)
        {
            if (m_nview.IsOwner())
            {
                Push();
            }
        }

        private void Push()
        {
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (Vector3.Dot(m_body.linearVelocity, forward) >= Plugin.Cfg.PushMaxSpeed.Value)
            {
                return;
            }
            m_body.WakeUp();
            m_body.AddForce(forward * Plugin.Cfg.PushImpulse.Value, ForceMode.VelocityChange);
        }

        private void RPC_Controls(long sender, Vector3 moveDir)
        {
            m_controls = moveDir;
            m_controlsTime = Time.time;
        }

        private Vector3 CurrentControls()
        {
            // stale input (rider gone, packets lost) counts as no input
            return Time.time - m_controlsTime < 0.6f && HasRider() && !IsPulled() ? m_controls : Vector3.zero;
        }

        /// <summary>Called on the rider's client right after sitting down: take over the physics so steering is immediate.</summary>
        public void OnLocalRiderSeated()
        {
            if (!m_nview.IsOwner() && !IsPulled())
            {
                m_nview.InvokeRPC("RPC_RequestOwn");
            }
        }

        // ---------------------------------------------------------------- physics

        private void OnCollisionEnter(Collision collision) => GatherContacts(collision);

        private void OnCollisionStay(Collision collision) => GatherContacts(collision);

        private void GatherContacts(Collision collision)
        {
            if (!m_nview.IsValid() || !m_nview.IsOwner())
            {
                return;
            }
            int count = collision.GetContacts(m_contactBuffer);
            for (int i = 0; i < count; i++)
            {
                var cp = m_contactBuffer[i];
                if (!m_runners.Contains(cp.thisCollider))
                {
                    continue;
                }
                var normal = cp.normal;
                if (Vector3.Dot(normal, transform.up) < 0f)
                {
                    normal = -normal;
                }
                m_contacts.Add(new Contact
                {
                    Point = cp.point,
                    Normal = normal,
                    Surface = SurfaceProbe.Classify(cp.otherCollider, cp.point, normal),
                });
            }
        }

        private void FixedUpdate()
        {
            if (!m_nview.IsValid())
            {
                return;
            }
            Speed = m_body.linearVelocity.magnitude;
            if (!m_nview.IsOwner())
            {
                m_contacts.Clear();
                return;
            }

            if (m_contacts.Count > 0)
            {
                m_lastContactTime = Time.fixedTime;
            }
            bool active = IsPulled() || HasRider();
            // static friction: a sled left standing settles into the snow and stays put on a slope (the runners'
            // own friction is the sliding kind, far lower). Not on ice, where a bumped sled slides (and some ice,
            // like Seasonality's, has a material that would win over this one anyway).
            SetResting(!active && Speed < Plugin.Cfg.StaticFrictionSpeed.Value && Surface != Surface.Ice);
            if (active && m_body.IsSleeping())
            {
                // a resting sled falls asleep; a sleeping body ignores gravity and the rider's input
                m_body.WakeUp();
            }
            UpdateOwnerSurface();

            if (!m_resting)
            {
                var controls = CurrentControls();
                ApplyRunnerFriction(controls.z < -0.5f);
                ApplyAirDrag();
                ApplyStability();
                if (m_contacts.Count > 0)
                {
                    ApplyRiderControls(controls);
                }
            }

            if (Plugin.Cfg.DebugLog.Value && (m_debugTimer -= Time.fixedDeltaTime) < 0f)
            {
                m_debugTimer = 1f;
                float span = Mathf.Max(Time.fixedTime - m_debugTime, 1e-3f);
                float aAlong = m_debugAlong / m_body.mass / span, aAcross = m_debugAcross / m_body.mass / span;
                m_debugAlong = m_debugAcross = 0f;
                m_debugTime = Time.fixedTime;
                Plugin.Log.LogInfo($"sled {m_nview.GetZDO().m_uid}: friction along={aAlong:0.000} across={aAcross:0.000} m/s2, mu={SurfaceProbe.Friction(Surface):0.000}; surface={Surface} speed={Speed:0.00} contacts={m_contacts.Count} mass={m_body.mass:0} resting={m_resting} pulled={IsPulled()} rider={HasRider()}");
            }
            m_contacts.Clear();
        }

        private void SetResting(bool resting)
        {
            if (resting == m_resting && m_runners.Count > 0)
            {
                return;
            }
            m_resting = resting;
            var mat = resting ? m_restMaterial : m_slideMaterial;
            foreach (var col in m_runners)
            {
                col.sharedMaterial = mat;
            }
            if (!resting)
            {
                m_body.WakeUp();
            }
        }

        private void UpdateOwnerSurface()
        {
            if (m_contacts.Count == 0)
            {
                if (m_floating != null && m_floating.BeenFloating())
                {
                    Surface = Surface.Water;
                }
                return;
            }
            // what most of the runner contacts touch decides what the sled is "on"
            Array.Clear(m_surfaceVotes, 0, m_surfaceVotes.Length);
            foreach (var c in m_contacts)
            {
                m_surfaceVotes[(int)c.Surface]++;
            }
            int best = 0;
            for (int i = 1; i < m_surfaceVotes.Length; i++)
            {
                if (m_surfaceVotes[i] > m_surfaceVotes[best])
                {
                    best = i;
                }
            }
            Surface = (Surface)best;
        }

        private void ApplyRunnerFriction(bool braking)
        {
            int count = m_contacts.Count;
            if (count == 0)
            {
                return;
            }
            float dt = Time.fixedDeltaTime;
            // The sled's weight, shared by the contacts. PhysX's per-point contact impulses
            // can't be trusted for this (they repeat the whole pair's impulse on every point).
            float weight = m_body.mass * Physics.gravity.magnitude / count;
            float effectiveMass = m_body.mass / count * FrictionSolverScale;
            float brake = braking ? Plugin.Cfg.BrakeFriction.Value : 0f;

            foreach (var c in m_contacts)
            {
                float n = weight * Mathf.Max(0f, c.Normal.y);
                if (n <= 0f)
                {
                    continue;
                }
                var forward = Vector3.ProjectOnPlane(transform.forward, c.Normal);
                if (forward.sqrMagnitude < 1e-4f)
                {
                    continue;
                }
                forward.Normalize();
                var side = Vector3.Cross(c.Normal, forward);
                var v = m_body.GetPointVelocity(c.Point);
                float along = Vector3.Dot(v, forward);
                float across = Vector3.Dot(v, side);

                // Coulomb friction as an impulse: cancel the sliding velocity, but never more
                // than mu * N * dt. Small velocities get cancelled outright, which also gives
                // static friction: a sled across a slope stays put instead of creeping down it.
                float maxAlong = (SurfaceProbe.Friction(c.Surface) + brake) * n * dt;
                float maxAcross = SurfaceProbe.Grip(c.Surface) * n * dt;
                float jAlong = Mathf.Clamp(-along * effectiveMass, -maxAlong, maxAlong);
                float jAcross = Mathf.Clamp(-across * effectiveMass, -maxAcross, maxAcross);
                m_body.AddForceAtPosition(forward * jAlong + side * jAcross, c.Point, ForceMode.Impulse);
                m_debugAlong += jAlong;
                m_debugAcross += Mathf.Abs(jAcross);
            }
        }

        private void ApplyAirDrag()
        {
            var v = m_body.linearVelocity;
            float speed = v.magnitude;
            if (speed < 0.5f)
            {
                return;
            }
            float drag = Plugin.Cfg.AirDrag.Value * speed * speed;
            float max = Plugin.Cfg.MaxSpeed.Value;
            if (speed > max)
            {
                // a soft wall: enough to stop gaining speed on any slope
                drag += m_body.mass * (speed - max) * 6f;
            }
            m_body.AddForce(-v / speed * drag, ForceMode.Force);
        }

        /// <summary>
        /// Keeps the sled level with the ground under it: a spring towards the runners' contact normal (in the air,
        /// towards the last ground and slowly to level), damped on roll and pitch only. A light cart body on narrow
        /// runners has little else to hold it: without this, a bump that lifts one runner rolls it over, and a jump
        /// lands it on its nose.
        /// </summary>
        private void ApplyStability()
        {
            float k = Plugin.Cfg.Stability.Value;
            if (k <= 0f)
            {
                return;
            }
            if (m_contacts.Count > 0)
            {
                var normal = Vector3.zero;
                foreach (var c in m_contacts)
                {
                    normal += c.Normal;
                }
                m_groundUp = Vector3.Slerp(m_groundUp, normal.normalized, 0.2f);
            }
            else
            {
                m_groundUp = Vector3.Slerp(m_groundUp, Vector3.up, Time.fixedDeltaTime);
            }
            var up = transform.up;
            var w = m_body.angularVelocity;
            // roll and pitch; turning (yaw) is the rider's
            var tilting = w - Vector3.Dot(w, up) * up;
            m_body.AddTorque((Vector3.Cross(up, m_groundUp) * StabilitySpring - tilting * StabilityDamping) * k, ForceMode.Acceleration);
        }

        private void ApplyRiderControls(Vector3 controls)
        {
            if (controls == Vector3.zero)
            {
                return;
            }
            var up = transform.up;
            float speed = Vector3.Dot(m_body.linearVelocity, transform.forward);
            // leaning on a runner turns the sled; faster sleds answer quicker
            float steer = Mathf.Clamp(controls.x, -1f, 1f);
            if (Mathf.Abs(steer) > 0.1f)
            {
                float authority = Mathf.Clamp(Mathf.Abs(speed) / 3f, 0.35f, 1f);
                float target = steer * 1.4f * Plugin.Cfg.SteerStrength.Value * authority * Mathf.Sign(speed + 0.01f);
                float current = Vector3.Dot(m_body.angularVelocity, up);
                // the runners' grip fights any turn, so the lean has to push hard
                m_body.AddTorque(up * (target - current) * Plugin.Cfg.SteerResponse.Value, ForceMode.Acceleration);
            }
        }

        // ---------------------------------------------------------------- visuals, sound, hauling

        private void Update()
        {
            m_shownSurface.Update(Surface, Time.time);
            if (!m_nview.IsValid())
            {
                return;
            }
            float dt = Time.deltaTime;
            if (!m_nview.IsOwner() && (m_probeTimer -= dt) < 0f)
            {
                // non-owners don't get contacts; a downward probe is enough for sound and snow
                m_probeTimer = ProbeInterval;
                Surface = ProbeSurface();
            }
            UpdateRiderOwnership();
            UpdateAudio(dt);
            UpdateSnowEffects(dt);
            UpdateHauling();
        }

        private float m_nextOwnRequest;

        /// <summary>
        /// A rider steering a sled nobody pulls should simulate it: ask for it whenever it is someone else's
        /// (it stays theirs while they pull, and the vanilla RPC refuses then).
        /// </summary>
        private void UpdateRiderOwnership()
        {
            long owner = m_nview.GetZDO().GetOwner();
            if (owner != m_lastOwner)
            {
                // someone just took it over (a player about to pull attaches a frame later): don't snatch it back
                m_lastOwner = owner;
                m_nextOwnRequest = Mathf.Max(m_nextOwnRequest, Time.time + 2f);
            }
            if (m_seat == null || m_nview.IsOwner() || IsPulled() || Time.time < m_nextOwnRequest || !m_seat.IsLocalRider())
            {
                return;
            }
            m_nextOwnRequest = Time.time + 1f;
            m_nview.InvokeRPC("RPC_RequestOwn");
        }

        private Surface ProbeSurface()
        {
            m_probeHit = SurfaceProbe.ProbeBelow(transform, transform.position + Vector3.up * 0.6f, 0.75f, out var surface);
            if (m_probeHit)
            {
                return surface;
            }
            return m_floating != null && m_floating.BeenFloating() ? Surface.Water : Surface;
        }

        private void UpdateAudio(float dt)
        {
            // the cart's wheel creak stays silent; SledAudio plays the runners
            var loops = m_vagon.m_wheelLoops;
            if (loops == null)
            {
                return;
            }
            foreach (var sfx in loops)
            {
                if (sfx != null && sfx.GetVolumeModifier() > 0f)
                {
                    sfx.SetVolumeModifier(0f);
                }
            }
        }

        private void UpdateSnowEffects(float dt)
        {
            if (!SurfaceProbe.IsSnowy(Surface) || Surface == Surface.Ice)
            {
                return;
            }
            bool braking = CurrentControls().z < -0.5f || (m_nview.IsOwner() && m_controls.z < -0.5f && HasRider());
            // spray is only for eyes: none on a dedicated server
            if (Plugin.Cfg.SnowSpray.Value && Player.m_localPlayer != null && (Speed > 4f || (braking && Speed > 1.5f)) && (m_sprayTimer -= dt) < 0f)
            {
                m_sprayTimer = SprayInterval * (braking ? 0.5f : Mathf.Lerp(1.5f, 0.6f, Speed / 12f));
                var prefab = SprayPrefab();
                if (prefab != null)
                {
                    foreach (var tail in m_runnerTails)
                    {
                        Instantiate(prefab, tail.position, Quaternion.LookRotation(-transform.forward));
                    }
                }
            }
            // ploughing through Deep North powder leaves a trail, as walking does
            if (Surface == Surface.Powder && m_nview.IsOwner() && Vector3.Distance(m_lastTrailPos, transform.position) > TrailStep)
            {
                m_lastTrailPos = transform.position;
                var trail = TrailPrefab();
                if (trail != null)
                {
                    foreach (var tail in m_runnerTails)
                    {
                        Instantiate(trail, tail.position, transform.rotation);
                    }
                }
            }
        }

        private void UpdateHauling()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            if (m_vagon.IsAttached(player))
            {
                LocalPulled = this;
                SledHaulEffect.Ensure(player);
            }
            else if (LocalPulled == this)
            {
                LocalPulled = null;
                SledHaulEffect.Remove(player);
            }
        }

        private static GameObject SprayPrefab()
        {
            if (s_sprayPrefab == null && ZNetScene.instance != null)
            {
                s_sprayPrefab = ZNetScene.instance.GetPrefab("fx_footstep_snow_run");
            }
            return s_sprayPrefab;
        }

        private static GameObject TrailPrefab()
        {
            if (s_trailPrefab == null && ZNetScene.instance != null)
            {
                s_trailPrefab = ZNetScene.instance.GetPrefab("snow_walk_medium");
                if (s_trailPrefab == null && Player.m_localPlayer != null)
                {
                    s_trailPrefab = Player.m_localPlayer.m_deepSnowWalkObj;
                }
            }
            return s_trailPrefab;
        }
    }
}
