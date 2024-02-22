using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Moonflow.Tools.MFPrefabCleaner
{
    public class MFParticleHiddenMatClean : MFPrefabCleanFunction
    {
        public MFParticleHiddenMatClean()
        {
            showName = "粒子系统隐藏冗余材质球清理";
        }
        public override bool Process(GameObject go, string name)
        {
            go.TryGetComponent(out ParticleSystem ps);
            if (ps != null)
            {
                go.TryGetComponent(out ParticleSystemRenderer psr);
                var componentTrails = ps.trails;
                if (componentTrails.enabled == false)
                {
                    if (psr != null)
                    {
                        var materials = psr.sharedMaterials;
                        List<Material> mats = new List<Material>();
                        if (materials.Length > 2)
                        {
                    
                            foreach (var mat in materials)
                            {
                                if(mat!=null) mats.Add(mat);
                            }

                            if (mats.Count > 1)
                            {
                                mats = mats.Distinct().ToList();
                            }
                    
                            psr.materials = mats.ToArray();
                            Debug.Log($"{name} 可能修改了材质");
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}