using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// The place to sit on the sled. It is a doodad controller like a ship's rudder, so the
    /// rider's movement keys steer the sled instead of standing the rider up, and the seat is
    /// claimed through the sled's owner the same way the rudder is.
    /// </summary>
    internal class SledSeat : MonoBehaviour, Interactable, Hoverable, IDoodadController
    {
        public Transform m_attachPoint;
        public Vector3 m_detachOffset = new Vector3(0f, 0.05f, 0f);
        public float m_useDistance = 3f;

        private static readonly int s_rider = "wintersled_rider".GetStableHashCode();

        private ZNetView m_nview;
        private SledController m_sled;

        private void Start()
        {
            m_nview = GetComponentInParent<ZNetView>();
            m_sled = GetComponentInParent<SledController>();
            if (m_nview == null || !m_nview.IsValid())
            {
                enabled = false;
                return;
            }
            m_nview.Register<long>("WinterSled_RequestSeat", RPC_RequestSeat);
            m_nview.Register<long>("WinterSled_ReleaseSeat", RPC_ReleaseSeat);
            m_nview.Register<bool>("WinterSled_SeatResponse", RPC_SeatResponse);
            if (m_nview.IsOwner())
            {
                // the rider is saved with the sled: one who logged out seated would hold the seat on the next load
                m_nview.GetZDO().Set(s_rider, 0L);
            }
        }

        // ---------------------------------------------------------------- Interactable / Hoverable

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || m_nview == null || !m_nview.IsValid())
            {
                return false;
            }
            var player = user as Player;
            if (player == null || player.IsEncumbered() || player.IsAttached() || !InUseDistance(player))
            {
                return false;
            }
            m_nview.InvokeRPC("WinterSled_RequestSeat", player.GetPlayerID());
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            if (Player.m_localPlayer == null || !InUseDistance(Player.m_localPlayer))
            {
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
            }
            return Localization.instance.Localize("$piece_wintersled\n[<color=yellow><b>$KEY_Use</b></color>] $wintersled_ride");
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("$piece_wintersled");
        }

        // ---------------------------------------------------------------- IDoodadController

        public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
        {
            if (m_sled != null)
            {
                m_sled.SetLocalControls(moveDir);
            }
        }

        public void OnUseStop(Player player)
        {
            if (m_nview != null && m_nview.IsValid())
            {
                m_nview.InvokeRPC("WinterSled_ReleaseSeat", player.GetPlayerID());
            }
            player.AttachStop();
            StepOff(player);
        }

        /// <summary>
        /// The rider stands up where they sat, on the deck, and steps or jumps off from there; moving with the sled,
        /// so a sled in motion doesn't throw them backwards. (The attach detach offset doesn't stick on a moving
        /// rigidbody, so the body is placed directly.)
        /// </summary>
        private void StepOff(Player player)
        {
            var pos = m_attachPoint.position + Vector3.up * 0.05f;
            player.transform.position = pos;
            if (player.m_body != null)
            {
                player.m_body.position = pos;
                player.m_body.linearVelocity = m_sled != null ? m_sled.GetComponent<Rigidbody>().GetPointVelocity(pos) : Vector3.zero;
            }
        }

        public Component GetControlledComponent()
        {
            return m_sled;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public bool IsValid()
        {
            return this != null && m_nview != null && m_nview.IsValid();
        }

        // ---------------------------------------------------------------- seat ownership

        /// <summary>Whether the local player sits in this seat.</summary>
        public bool IsLocalRider()
        {
            var player = Player.m_localPlayer;
            return player != null && player.IsAttached() && ReferenceEquals(player.GetDoodadController(), this);
        }

        public long GetRider()
        {
            return m_nview != null && m_nview.IsValid() ? m_nview.GetZDO().GetLong(s_rider, 0L) : 0L;
        }

        public bool HaveValidRider()
        {
            long rider = GetRider();
            if (rider == 0L)
            {
                return false;
            }
            // the rider's character must still be around and sitting here
            foreach (var p in Player.GetAllPlayers())
            {
                if (p.GetPlayerID() == rider)
                {
                    return Vector3.Distance(p.transform.position, m_attachPoint.position) < 2f;
                }
            }
            return false;
        }

        private void RPC_RequestSeat(long sender, long playerID)
        {
            if (!m_nview.IsOwner())
            {
                return;
            }
            bool granted = GetRider() == playerID || !HaveValidRider();
            if (granted)
            {
                m_nview.GetZDO().Set(s_rider, playerID);
            }
            m_nview.InvokeRPC(sender, "WinterSled_SeatResponse", granted);
        }

        private void RPC_ReleaseSeat(long sender, long playerID)
        {
            if (m_nview.IsOwner() && GetRider() == playerID)
            {
                m_nview.GetZDO().Set(s_rider, 0L);
            }
        }

        private void RPC_SeatResponse(long sender, bool granted)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            if (!granted)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_inuse");
                return;
            }
            player.StartDoodadControl(this);
            // the sled's colliders are ignored by the rider, so sitting inside it doesn't shove it around
            player.AttachStart(m_attachPoint, m_sled.gameObject, hideWeapons: false, isBed: false, onShip: false,
                Plugin.Cfg.SeatAnimation.Value, m_detachOffset);
            player.Message(MessageHud.MessageType.Center, Translations.ControlsHint);
            m_sled.OnLocalRiderSeated();
        }

        private bool InUseDistance(Humanoid human)
        {
            return Vector3.Distance(human.transform.position, m_attachPoint.position) < m_useDistance;
        }

        public float GetHoverOffset()
        {
            return 0f;
        }
    }
}
