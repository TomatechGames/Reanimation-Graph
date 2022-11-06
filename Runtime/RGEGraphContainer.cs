using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Aarthificial.Reanimation;
using Aarthificial.Reanimation.Nodes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tomatech.ReanimationGraph
{
    [CreateAssetMenu(menuName = "Tomatech/RGE/Graph", fileName = "New Reanimator Graph")]
    public class RGEGraphContainer : ReanimatorNode
    {
        [SerializeField]
        ReanimatorNode rootNode;

        public override TerminationNode Resolve(IReadOnlyReanimatorState previousState, ReanimatorState nextState)
        {
            return rootNode.Resolve(previousState, nextState);
        }

#if UNITY_EDITOR

        [SerializeField]
        List<NodeInstanceData> nodeData = new();
        [SerializeField]
        List<DriverInstanceData> driverData = new();
        [SerializeField]
        string[] driverBlackboard = new string[0];

        public Vector2 rootPos = Vector2.one*250;

        public ReanimatorNode RootNode => rootNode;
        public List<NodeInstanceData> NodeData => nodeData;
        public List<DriverInstanceData> DriverData => driverData;
        public string[] DriverBlackboard => driverBlackboard;

        public List<TerminationNode> GetTerminationNodes()
        {
            return NodeData.Select(x=>x.node).Where(x=>x is TerminationNode).Cast<TerminationNode>().ToList();
        }

        public void SetRootNode(ReanimatorNode rootNode)
        {
            this.rootNode = rootNode;
        }

        public void CreateOrUpdateNode(NodeInstanceData nodeInstanceData)
        {
            int removed = nodeData.RemoveAll(n => n.node == nodeInstanceData.node);
            nodeData.Add(nodeInstanceData);
            if (removed==0)
            {
                AssetDatabase.AddObjectToAsset(nodeInstanceData.node, this);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        public void RemoveNode(ReanimatorNode node)
        {
            NodeInstanceData[] toRemove = nodeData.Where(n => n.node == node).ToArray();
            foreach (var item in toRemove)
            {
                AssetDatabase.RemoveObjectFromAsset(item.node);
                DestroyImmediate(item.node);
            }
            if (toRemove.Length>0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
            nodeData.RemoveAll(n => n.node == node);
        }

        public void CreateOrUpdateDriver(DriverInstanceData driverInstanceData)
        {
            RemoveDriver(driverInstanceData.guid);
            driverData.Add(driverInstanceData);
        }

        public void RenameDriverInstances(string oldName, string newName)
        {
            List<DriverInstanceData> renamed = new();
            foreach (var item in driverData.Where(d => d.name == oldName))
            {
                DriverInstanceData renamedDriver = item;
                renamedDriver.name = newName;
                renamed.Add(renamedDriver);
            }
            driverData.RemoveAll(d => d.name == oldName);
            driverData.AddRange(renamed);
        }

        public void RemoveDriversWithName(string name) =>
            driverData.RemoveAll(d => d.name == name);

        public void RemoveDriver(string guid)=>
            driverData.RemoveAll(d => d.guid == guid);

        public void SetDriverBlackboard(string[] driverBlackboard) =>
            this.driverBlackboard = driverBlackboard;

        [Serializable]
        public struct NodeInstanceData
        {
            [SerializeReference]
            public ReanimatorNode node;
            public Vector2 position;
        }

        [Serializable]
        public struct DriverInstanceData
        {
            public string guid;
            public string name;
            public string[] connectionNames;
            public Vector2 position;
        }

#endif

    }
}
