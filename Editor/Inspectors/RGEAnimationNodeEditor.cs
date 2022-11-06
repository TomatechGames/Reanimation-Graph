using Aarthificial.Reanimation.Editor.Nodes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tomatech.ReanimationGraph.Editor.Inspectors
{
    public class RGEAnimationNodeEditor : AnimationNodeEditor
    {
        protected override void OnEnable()
        {
            if (!serializedObject.targetObject)
                return;
            AddCustomProperty("drivers");
            Cels = AddCustomProperty("cels");
        }
    }
}
