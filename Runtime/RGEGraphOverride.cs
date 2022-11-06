using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Aarthificial.Reanimation;
using Aarthificial.Reanimation.Nodes;

namespace Tomatech.ReanimationGraph
{
    [CreateAssetMenu(menuName = "Tomatech/RGE/Graph Override", fileName = "New Reanimator Graph Override")]
    public class RGEGraphOverride : ReanimatorNode
    {
        [SerializeField]
        RGEGraphContainer graphNode;
        [SerializeField] 
        private List<OverridePair> overrides = new();

        private readonly Dictionary<TerminationNode, TerminationNode> _map = new();

        private void OnEnable()
        {
            _map.Clear();
            foreach (var pair in overrides)
            {
                if (pair.fromNode == null || pair.toNode == null) continue;
                _map[pair.fromNode] = pair.toNode;
            }
        }

        public override TerminationNode Resolve(IReadOnlyReanimatorState previousState, ReanimatorState nextState)
        {
            AddTrace(nextState);
            var node = graphNode.Resolve(previousState, nextState);
            if (!_map.ContainsKey(node))
                return node;

            var overrideNode = _map[node];
#if UNITY_EDITOR
            nextState.AddTrace(overrideNode);
#endif
            return overrideNode;
        }
    }
}
