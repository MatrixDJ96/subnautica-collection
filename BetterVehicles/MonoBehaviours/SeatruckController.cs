#if BELOWZERO
using BetterSubnautica.Extensions;
using UnityEngine;
using UWE;

namespace BetterVehicles.MonoBehaviours
{
    public class SeatruckController : MonoBehaviour
    {
        protected SeaTruckSegment segment = null;
        protected SeaTruckMotor motor = null;

        public bool ForceAction { get; set; } = false;
        public bool DirectExit { get; set; } = false;
        public bool DirectEnter { get; set; } = false;
        public bool EnterHatch { get; set; } = false;

        protected void Awake()
        {
            segment = gameObject.GetComponent<SeaTruckSegment>();
            motor = gameObject.GetComponent<SeaTruckMotor>();

            if (segment == null || !segment.IsMainSegment() || motor == null)
            {
                Destroy(this);
                return;
            }

            Reset();
        }

        protected void Update()
        {
            if (!Player.main.GetPDA().isInUse && !FreezeTime.HasFreezers() && UWE.Utils.lockCursor)
            {
                if (Input.GetKeyDown(Core.SeatruckSettings.ForceAction))
                {
                    ForceAction = true;
                }

                if (Input.GetKeyUp(Core.SeatruckSettings.ForceAction))
                {
                    ForceAction = false;
                }

                if (motor.IsPiloted() && Input.GetKeyDown(Core.SeatruckSettings.DetachSegments))
                {
                    segment.Detach();
                }
            }
        }

        public void Reset()
        {
            ForceAction = false;
            DirectEnter = false;
            DirectExit = false;
            EnterHatch = false;
        }
    }
}
#endif
