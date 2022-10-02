using Aarthificial.Reanimation.Editor.Nodes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tomatech.ReanimationGraph.RGEditor.Inspectors
{
    public class RGESwitchNodeEditor : ReanimatorNodeEditor
    {
        protected void OnEnable()
        {
            AddCustomProperty("drivers");
        }
    }
}
