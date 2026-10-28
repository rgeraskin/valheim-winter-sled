using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// With a mod that hitches vehicles to tame animals (Beasts of Burden, OdinHorse, Draft Animals): keeps the hitch on
    /// the animal level with the ground. The animal mods put it a fixed height above the animal's feet and some way
    /// behind it, but on a slope the ground there isn't at the animal's feet: downhill it is higher, so the hitch sits in
    /// the snow and the vehicle noses down; uphill it is lower, so the hitch floats and lifts the vehicle's front off the
    /// ground (Beasts of Burden: a lox's hitch is 2.6 m behind it, 1 m off on 22°). The same height above the ground
    /// under the hitch keeps the shafts where the mod meant them. Sleds and carts alike; players are untouched.
    /// It runs on the vehicle's owner, where the joints are.
    /// </summary>
    internal class AnimalHitch : MonoBehaviour
    {
        // a hitch is moved at most this far (m) from where the animal mod put it: not down a cliff, not up a wall
        public const float MaxShift = 1.5f;

        private static int s_groundMask;

        private ZNetView m_nview;
        private readonly List<ConfigurableJoint> m_joints = new List<ConfigurableJoint>(2);
        // the animal mod's own hitch point on the animal (its local space), and the one set here instead
        private ConfigurableJoint m_joint;
        private Vector3 m_modAnchor;
        private Vector3 m_levelled;

        private void Awake()
        {
            m_nview = GetComponent<ZNetView>();
        }

        // The mod may set its point again at any time (Draft Animals does every frame), so the point it last set is the
        // one levelled, right before each physics step.
        private void FixedUpdate()
        {
            if (!Plugin.Cfg.LevelAnimalHitch.Value || m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner())
            {
                return;
            }
            GetComponents(m_joints);
            ConfigurableJoint joint = null;
            Character animal = null;
            foreach (var j in m_joints)
            {
                if (j != null && j.connectedBody != null && j.connectedBody.TryGetComponent<Character>(out var character) && !character.IsPlayer())
                {
                    joint = j;
                    animal = character;
                    break;
                }
            }
            if (joint == null)
            {
                m_joint = null;
                return;
            }
            if (joint != m_joint || joint.connectedAnchor != m_levelled)
            {
                // a new hitch, or the mod put its point back
                m_joint = joint;
                m_modAnchor = joint.connectedAnchor;
            }
            var feet = animal.transform.position;
            var hitch = animal.transform.TransformPoint(m_modAnchor);
            if (s_groundMask == 0)
            {
                // the ground, not the vehicle's own shafts (vehicle) nor the animal (character)
                s_groundMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            }
            // the slope between the animal and its hitch, from the ground under both: never from the animal's own
            // height, or an animal off the ground (a jump, a bump) would be lifted further by its own hitch
            if (!GroundBelow(feet, out float groundAtFeet) || !GroundBelow(hitch, out float groundAtHitch))
            {
                return;
            }
            hitch.y = LevelHeight(feet.y, m_modAnchor.y, groundAtFeet, groundAtHitch, MaxShift);
            m_levelled = animal.transform.InverseTransformPoint(hitch);
            joint.connectedAnchor = m_levelled;
        }

        private static bool GroundBelow(Vector3 point, out float height)
        {
            var from = point + Vector3.up * (MaxShift + 2f);
            bool found = Physics.Raycast(from, Vector3.down, out var hit, 2f * MaxShift + 6f, s_groundMask);
            height = found ? hit.point.y : 0f;
            return found;
        }

        /// <summary>
        /// The height of a hitch the animal mod put <paramref name="height"/> above the animal's feet: raised or lowered by
        /// how much higher the ground under the hitch is than the ground under the animal, at most <paramref name="maxShift"/>.
        /// </summary>
        public static float LevelHeight(float feetY, float height, float groundAtFeet, float groundAtHitch, float maxShift)
        {
            return feetY + height + Mathf.Clamp(groundAtHitch - groundAtFeet, -maxShift, maxShift);
        }
    }

    /// <summary>Every vehicle (the sled, the cart; the catapult and ram too) keeps an animal's hitch level with the ground.</summary>
    [HarmonyPatch(typeof(Vagon), "Awake")]
    internal static class AddAnimalHitch
    {
        private static void Postfix(Vagon __instance)
        {
            if (!__instance.GetComponent<AnimalHitch>())
            {
                __instance.gameObject.AddComponent<AnimalHitch>();
            }
        }
    }
}
