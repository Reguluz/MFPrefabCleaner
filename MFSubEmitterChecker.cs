using Moonflow.Core;
using UnityEngine;

namespace Moonflow.Tools.MFPrefabCleaner
{
    public class MFSubEmitterChecker: MFPrefabCleanFunction
    {
        public MFSubEmitterChecker()
        {
            showName = "子发射器检测";
        }
        public override bool Process(GameObject go, string name)
        {
            go.TryGetComponent(out ParticleSystem ps);
            if (ps != null)
            {
                if (ps.subEmitters.enabled)
                {
                    Debug.Log($"{name} has enabled subemitter");
                }
                return ps.subEmitters.enabled;
            }
            return false;
        }
    }
}