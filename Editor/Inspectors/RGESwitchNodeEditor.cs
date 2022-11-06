using Aarthificial.Reanimation.Editor.Nodes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tomatech.ReanimationGraph.Editor.Inspectors
{
    public class RGESwitchNodeEditor : ReanimatorNodeEditor
    {
        protected void OnEnable()
        {
            if (!serializedObject.targetObject)
                return;
            AddCustomProperty("drivers");
        }
    }
}
