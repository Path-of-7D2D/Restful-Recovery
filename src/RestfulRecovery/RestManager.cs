using UnityEngine;

namespace RestfulRecovery
{
    // Local-player rest state. Sitting is a visual pose plus a movement lock,
    // not a vehicle attach, so no vehicle controls/fuel/storage/physics get
    // pulled in.
    internal static class RestManager
    {
        public const string RestingBuffName = "buffRestfulRecoveryResting";

        // Vanilla 4x4 tailgate-passenger seated pose (vehicles.xml seat4):
        // hands free with legs hanging at a bent-knee angle, unlike the
        // front-passenger pose (41) whose legs stretch into the footwell.
        // Networked through the player's vehicle pose stats.
        private const int SeatedPoseId = 44;

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
        // Third-person camera distance while resting. Vanilla is 1.2m close
        // up to 4.8m fully scrolled out; the vp camera's own collision
        // handling pulls it back in when something is in the way.
        private const float RestCameraDistance = 3f;

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
            var seatRotation = blockRotation * Quaternion.Euler(0f, profile.YawOffsetDegrees, 0f);

            seatBlockPos = blockPos;
            seatBlockType = blockValue.type;
            seatAnchor = blockCenter
                + Vector3.up * profile.Height
                + seatRotation * Vector3.forward * profile.ForwardOffset;
            seatYawDegrees = seatRotation.eulerAngles.y;

            isResting = true;
            restStartedTime = Time.time;
            healthAtLastUpdate = player.Health;

            player.SetFirstPersonView(_bFirstPersonView: false, _bLerpPosition: true);

            // Fully stop the local player controller, like MoveState.Off
            // does; leaving it enabled keeps the capsule colliding with the
            // furniture and pushes the player on top of it.
            var controller = player.vp_FPController;
            if (controller != null)
            {
                controller.Stop();
                controller.enabled = false;

                // The vp Driving state is what vehicles use while seated; it
                // stops vp_FPCamera from re-writing the player root's yaw
                // from the camera every frame, which would fight the seat
                // facing and jitter the model.
                controller.Player.Driving.Start();
            }
            player.m_characterController.Enable(false);
            player.motion = Vector3.zero;
            player.SetPosition(seatAnchor);

            // Snaps the camera once so the player starts out looking the way
            // they are seated; after this the camera orbits freely.
            player.SetRotationAndStopTurning(new Vector3(0f, seatYawDegrees, 0f));
            ApplySeatFacing(player);

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
            var controller = player.vp_FPController;
            if (controller != null)
            {
                controller.Player.Driving.Stop();
                controller.enabled = true;
                controller.Stop();
            }

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
            ApplySeatFacing(player);

            if (Time.time - lastBuffRefreshTime >= BuffRefreshIntervalSeconds)
            {
                player.Buffs.AddBuff(RestingBuffName);
                lastBuffRefreshTime = Time.time;
            }
        }

        // Faces the seated body toward the furniture's forward. SetRotation
        // is not enough for the local player: updateTransform re-reads yaw
        // from PhysicsTransform every frame, and nothing syncs the graphics
        // root's rotation while the vp controller owns the player, so both
        // transforms are written directly. SetRotation itself is avoided here
        // because its override snaps the camera, which should stay free to
        // orbit while resting.
        private static void ApplySeatFacing(EntityPlayerLocal player)
        {
            var euler = new Vector3(0f, seatYawDegrees, 0f);
            player.rotation = euler;
            player.qrotation = Quaternion.Euler(euler);
            player.transform.rotation = player.qrotation;

            var physicsTransform = player.PhysicsTransform;
            if (physicsTransform != null)
            {
                var physicsEuler = physicsTransform.eulerAngles;
                physicsEuler.y = seatYawDegrees;
                physicsTransform.eulerAngles = physicsEuler;
            }
        }

        // Driven by the EntityPlayerLocal.FrameUpdateCamera postfix, which
        // runs right after vanilla writes Position3rdPersonOffset each frame.
        // Overriding the offset is required because vanilla skips the scroll
        // zoom multiplier entirely whenever a ceiling is overhead, which is
        // exactly where most chairs are.
        public static void UpdateRestCamera(EntityPlayerLocal player)
        {
            if (!isResting || player == null || player.bFirstPersonView)
            {
                return;
            }

            var camera = player.vp_FPCamera;
            if (camera == null)
            {
                return;
            }

            var offset = camera.Position3rdPersonOffset;
            offset.z = RestCameraDistance;
            camera.Position3rdPersonOffset = offset;

            // With the vp Driving state active the third-person camera
            // orbits DrivingPosition instead of the controller position.
            // Vehicles feed it every frame; without one it goes stale and
            // the camera drops to the ground, so keep it on the sitter.
            camera.DrivingPosition = player.transform.position;
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
