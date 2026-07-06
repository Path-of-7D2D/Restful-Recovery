using HarmonyLib;

namespace RestfulRecovery
{
    internal static class RestfulRecoveryPatches
    {
        private const string RestCommandName = "restfulRecoveryRest";

        // Chairs and couches are plain Block instances in V3.0 (no Class
        // override), so all four activation patches target Block itself and
        // bail out for non-restable blocks.

        [HarmonyPatch(typeof(Block), nameof(Block.GetActivationText))]
        private static class GetActivationTextPatch
        {
            private static void Postfix(
                BlockValue _blockValue,
                EntityAlive _entityFocusing,
                ref string __result)
            {
                if (!(_entityFocusing is EntityPlayerLocal) || !RestBlocks.IsRestable(_blockValue))
                {
                    return;
                }

                if (RestManager.IsResting)
                {
                    __result = Localization.Get("restfulRecoveryStandPrompt");
                    return;
                }

                // Blocks with vanilla commands (e.g. pickup-able chairs) keep
                // their own prompt; holding activate shows both commands.
                if (string.IsNullOrEmpty(__result))
                {
                    __result = string.Format(
                        Localization.Get("restfulRecoveryRestPrompt"),
                        _blockValue.Block.GetLocalizedBlockName());
                }
            }
        }

        [HarmonyPatch(typeof(Block), nameof(Block.HasBlockActivationCommands))]
        private static class HasBlockActivationCommandsPatch
        {
            private static void Postfix(BlockValue _blockValue, ref bool __result)
            {
                if (RestBlocks.IsRestable(_blockValue))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Block), nameof(Block.GetBlockActivationCommands))]
        private static class GetBlockActivationCommandsPatch
        {
            private static void Postfix(BlockValue _blockValue, ref BlockActivationCommand[] __result)
            {
                if (!RestBlocks.IsRestable(_blockValue))
                {
                    return;
                }

                // Append instead of replace so pickup-able chairs keep their
                // vanilla Take command.
                var commands = new BlockActivationCommand[__result.Length + 1];
                for (var i = 0; i < __result.Length; i++)
                {
                    commands[i] = __result[i];
                }

                commands[__result.Length] = new BlockActivationCommand(RestCommandName, "map_bed", _enabled: true);
                __result = commands;
            }
        }

        [HarmonyPatch(typeof(Block), nameof(Block.OnBlockActivated),
            typeof(string), typeof(WorldBase), typeof(Vector3i), typeof(BlockValue), typeof(EntityPlayerLocal))]
        private static class OnBlockActivatedPatch
        {
            private static bool Prefix(
                string _commandName,
                WorldBase _world,
                Vector3i _blockPos,
                BlockValue _blockValue,
                EntityPlayerLocal _player,
                ref bool __result)
            {
                if (_commandName != RestCommandName)
                {
                    return true;
                }

                if (RestManager.IsResting)
                {
                    RestManager.StopResting(_player);
                    __result = true;
                    return false;
                }

                __result = RestManager.StartResting(_world, _blockPos, _blockValue, _player);
                return false;
            }
        }

        [HarmonyPatch(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.OnUpdateLive))]
        private static class OnUpdateLivePatch
        {
            private static void Postfix(EntityPlayerLocal __instance)
            {
                RestManager.UpdateResting(__instance);
            }
        }
    }
}
