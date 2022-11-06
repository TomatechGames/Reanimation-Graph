using Aarthificial.Reanimation.Nodes;
using System.Collections;
using System.Collections.Generic;
using Tomatech.ReanimationGraph.Editor.Inspectors;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tomatech.ReanimationGraph.Editor.Elements
{
    public class RGESwitchNode : RGENodeBase
    {
        List<Port> outputPorts = new();
        
        protected override string DefaultName => "New Switch Node";
        SwitchNode linkedNode;
        public override ReanimatorNode LinkedReanimNode => linkedNode;

        protected override void CreateNode(ReanimatorNode existingNode)
        {
            if(existingNode)
                linkedNode = existingNode as SwitchNode;
            else
                linkedNode = ScriptableObject.CreateInstance<SwitchNode>();
            NodeInspector = UnityEditor.Editor.CreateEditor(linkedNode, typeof(RGESwitchNodeEditor));
            //NodeInspector = Editor.CreateEditor(linkedNode);
        }

        public override void Draw()
        {
            base.Draw();

            Button addElementButton = new()
            {
                text = "Add Element"
            };
            addElementButton.clicked += AddOutputPort;
            mainContainer.Insert(1, addElementButton);
            mainContainer.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

            int portCount = FindOrGetSerializedNode().FindProperty("nodes").arraySize;
            for (int i = 0; i < portCount; i++)
            {
                outputPorts.Add(CreateOutputPort(outputPorts.Count));
            }
        }

        public override void DisconnectAllPorts()
        {
            base.DisconnectAllPorts();

            foreach (Port port in outputPorts)
            {
                graphView.DeleteElements(port.connections);
            }
        }

        public void ReconnectPorts()
        {
            SerializedProperty nodeProp = FindOrGetSerializedNode().FindProperty("nodes");
            for (int i = 0; i < nodeProp.arraySize; i++)
            {
                var reaniNode = (nodeProp.GetArrayElementAtIndex(i).objectReferenceValue as ReanimatorNode);
                if (!reaniNode)
                    continue;
                string destNode = reaniNode.name;
                RGENodeBase rgeNodeBase = graphView.GetNodeByName(destNode);
                graphView.AddElement(outputPorts[i].ConnectTo(rgeNodeBase.ReferencePort));
            }
        }

        public void AssignReferenceToPortIndex(Port port, ReanimatorNode node)
        {
            if (outputPorts.Contains(port))
            {
                int index = outputPorts.IndexOf(port);
                FindOrGetSerializedNode().FindProperty("nodes").GetArrayElementAtIndex(index).objectReferenceValue = node;
                serializedNode.ApplyModifiedProperties();
            }
        }

        public void RemoveReferenceFromPortIndex(Port port)
        {
            if (outputPorts.Contains(port))
            {
                int index = outputPorts.IndexOf(port);
                FindOrGetSerializedNode().FindProperty("nodes").GetArrayElementAtIndex(index).objectReferenceValue = null as ReanimatorNode;
                serializedNode.ApplyModifiedProperties();
            }
        }

        Port CreateOutputPort(int index)
        {
            Port switchPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            switchPort.portName = "Element " + index;
            Button deleteElementButton = new()
            {
                text = "X"
            };
            deleteElementButton.clicked += () => RemoveOutputPort(switchPort);
            switchPort.Add(deleteElementButton);
            outputContainer.Add(switchPort);
            return switchPort;
        }

        void AddOutputPort()
        {
            outputPorts.Add(CreateOutputPort(outputPorts.Count));
            FindOrGetSerializedNode().FindProperty("nodes").arraySize = outputPorts.Count;
            serializedNode.ApplyModifiedProperties();
            RefreshExpandedState();
        }

        void RemoveOutputPort(Port port)
        {
            if (outputPorts.Contains(port))
            {
                int index = outputPorts.IndexOf(port);
                FindOrGetSerializedNode().FindProperty("nodes").DeleteArrayElementAtIndex(index);
                serializedNode.ApplyModifiedProperties();
                graphView.DeleteElements(port.connections);
                outputContainer.Remove(port);
                outputPorts.Remove(port);

                for (int i = 0; i < outputPorts.Count; i++)
                {
                    outputPorts[i].portName = "Element " + i;
                }
                RefreshExpandedState();
            }
        }
    }
}
