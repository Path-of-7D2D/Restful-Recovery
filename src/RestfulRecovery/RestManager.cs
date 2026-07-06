using UnityEngine;

namespace RestfulRecovery
{
    // Local-player rest state. Sitting is a visual pose plus a movement lock,
    // not a vehicle attach, so no vehicle controls/fuel/storage/physics get
    // pulled in.
    internal static class RestManager
    {
        public const string RestingBuffName = "buffRestfulRecoveryResting";

        // Vanilla bicycle seated pose; networked through the player's vehicle
        // pose stats. If it reads too bike-specific in testing, try 20/30/40.
        private const int SeatedPoseId = 1;

        // The resting buff has a 4s duration in XML so it self-expires if rest
        // ends abnormally; refresh it well inside that window.
        private const float BuffRefreshIntervalSeconds = 1f;

        // Ignore cancel inputs briefly so the activation click/release that
        // started the rest does not immediately end it.
        private const float InputGraceSeconds = 0.25f;

        private static bool isResting;
        private static Vector3i seatBlockPos;
        private static int seatBlockType;
        private static Vector3 seatAnchor;
        private static float seatYawDegrees;
        private static int healthAtLastUpdate;
        private static float restStartedTime;
        private static float lastBuffRefreshTime;

        public static bool IsResting => isResting;

        public static bool StartResting(WorldBase world, Vector3i blockPos, BlockValue blockValue, EntityPlayerLocal player)
        {
            if (isResting || world == null || player == null || player.IsDead() || player.AttachedToEntity != null)
            {
                return false;
            }

            if (!RestBlocks.TryGetSeatProfile(blockValue, out var profile))
            {
                return false;
            }

            var blockRotation = blockValue.Block.shape.GetRotation(blockValue);
            var blockCenter = blockPos.ToVector3() + new Vector3(0.5f, 0f, 0.5f);

            seatBlockPos = blockPos;
            seatBlockType = blockValue.type;
            seatAnchor = blockCenter + blockRotation * profile.LocalOffset;
            seatYawDegrees = blockRotation.eulerAngles.y + profile.YawOffsetDegrees;

            isResting = true;
            restStartedTime = Time.time;
            healthAtLastUpdate = player.Health;

            player.SetFirstPersonView(_bFirstPersonView: false, _bLerpPosition: true);
            player.m_characterController.Enable(false);
            player.motion = Vector3.zero;
            player.SetPosition(seatAnchor);
            player.SetRotationAndStopTurning(new Vector3(0f, seatYawDegrees, 0f));
            player.SetVehiclePoseMode(SeatedPoseId);

            player.Buffs.AddBuff(RestingBuffName);
            lastBuffRefreshTime = Time.time;

            return true;
        }

        public static void StopResting(EntityPlayerLocal player)
        {
            if (!isResting)
            {
                return;
            }

            isResting = false;

            if (player == null)
            {
                return;
            }

            player.SetVehiclePoseMode(-1);
            player.Buffs.RemoveBuff(RestingBuffName);
            player.m_characterController.Enable(true);

            // bPreferFirstPerson was untouched while we forced third person,
            // so this restores whatever the player used before resting.
            player.SwitchToPreferredCameraMode(_lerpPosition: true);
        }

        // Driven by the EntityPlayerLocal.OnUpdateLive postfix.
        public static void UpdateResting(EntityPlayerLocal player)
        {
            if (!isResting || player == null)
            {
                return;
            }

            if (player.IsDead() || player.AttachedToEntity != null || ShouldCancelFromWorld(player) || ShouldCancelFromInput(player))
            {
                StopResting(player);
                return;
            }

            if (player.Health < healthAtLastUpdate)
            {
                StopResting(player);
                return;
            }
            healthAtLastUpdate = player.Health;

            // Hold the seat: physics is disabled, so just pin the position.
            player.motion = Vector3.zero;
            player.SetPosition(seatAnchor, _bUpdatePhysics: false);

            if (Time.time - lastBuffRefreshTime >= BuffRefreshIntervalSeconds)
            {
                player.Buffs.AddBuff(RestingBuffName);
                lastBuffRefreshTime = Time.time;
            }
        }

        private static bool ShouldCancelFromWorld(EntityPlayerLocal player)
        {
            var world = player.world;
            if (world == null)
            {
                return true;
            }

            if (world.GetBlock(seatBlockPos).type != seatBlockType)
            {
                return true;
            }

            return player.playerUI != null && player.playerUI.windowManager.IsModalWindowOpen();
        }

        private static bool ShouldCancelFromInput(EntityPlayerLocal player)
        {
            if (Time.time - restStartedTime < InputGraceSeconds)
            {
                return false;
            }

            var input = player.playerInput;
            if (input == null)
            {
                return false;
            }

            return input.MoveForward.IsPressed
                || input.MoveBack.IsPressed
                || input.MoveLeft.IsPressed
                || input.MoveRight.IsPressed
                || input.Jump.IsPressed
                || input.Crouch.IsPressed
                || input.Primary.IsPressed
                || input.Secondary.IsPressed;
        }
    }
}
