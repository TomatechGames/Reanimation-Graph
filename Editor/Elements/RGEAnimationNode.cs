using Aarthificial.Reanimation.Cels;
using Aarthificial.Reanimation.Nodes;
using System.Collections;
using System.Collections.Generic;
using Tomatech.ReanimationGraph.Editor.Inspectors;
using UnityEditor;
using UnityEngine;

namespace Tomatech.ReanimationGraph.Editor.Elements
{
    public class RGEAnimationNode<TNode, TCel> : RGENodeBase 
        where TCel : ICel
        where TNode : AnimationNode<TCel>
    {
        TNode linkedNode;
        public override ReanimatorNode LinkedReanimNode => linkedNode;
        protected override string DefaultName => "New Animation Node";

        protected override void CreateNode(ReanimatorNode existingNode)
        {
            if(existingNode)
                linkedNode = existingNode as TNode;
            else
                linkedNode = ScriptableObject.CreateInstance<TNode>();
            NodeInspector = UnityEditor.Editor.CreateEditor(linkedNode, typeof(RGEAnimationNodeEditor));
        }
    }
}
