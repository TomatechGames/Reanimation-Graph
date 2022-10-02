using Aarthificial.Reanimation.Editor.Nodes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tomatech.ReanimationGraph.RGEditor.Inspectors
{
    public class RGEAnimationNodeEditor : AnimationNodeEditor
    {
        protected override void OnEnable()
        {
            AddCustomProperty("drivers");
            Cels = AddCustomProperty("cels");
        }
    }
}
