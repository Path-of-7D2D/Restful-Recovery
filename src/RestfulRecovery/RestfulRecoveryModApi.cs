using System.Reflection;
using HarmonyLib;
using UnityEngine.Scripting;

namespace RestfulRecovery
{
    [Preserve]
    public class RestfulRecoveryModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony("com.pathof7d2d.restfulrecovery");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Out("[RestfulRecovery] Loaded rest activation patches for chairs and couches.");
        }
    }
}
