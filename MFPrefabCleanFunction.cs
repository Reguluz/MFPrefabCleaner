
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class MFPrefabCleanFunction
{
    public string showName;
    public bool needProcess;
    public abstract bool Process(GameObject go, string name);
}
