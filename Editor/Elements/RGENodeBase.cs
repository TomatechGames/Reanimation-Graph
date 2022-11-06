using Aarthificial.Reanimation.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tomatech.ReanimationGraph.Editor.Elements
{
    public abstract class RGENodeBase : Node
    {
        public string NodeName { get; set; }
        public abstract ReanimatorNode LinkedReanimNode { get; }
        public UnityEditor.Editor NodeInspector { get; protected set; }
        protected Port referencePort;
        protected Port controlDriverPort;
        protected TextField nameTextField;
        protected RGEGraphView graphView;

        public Port ReferencePort => referencePort;
        public Port ControlDriverPort => controlDriverPort;
        protected virtual string DefaultName => "New Node";

        public static implicit operator RGEGraphContainer.NodeInstanceData(RGENodeBase thisNode)
        {
            return new() { node = thisNode.LinkedReanimNode, position = RGEGraphView.NodePosition(thisNode)};
        }

        public virtual void Initialize(Vector2 position, RGEGraphView parentGraph, ReanimatorNode existingNode = null)
        {
            graphView = parentGraph;
            SetPosition(new Rect(position, Vector2.zero));
            if (existingNode)
                NodeName = existingNode.name;
            else
                NodeName = graphView.GetNewNodeName(DefaultName);
            CreateNode(existingNode);
            LinkedReanimNode.name = NodeName;
            if (!existingNode)
                graphView.editorWindow.Target.CreateOrUpdateNode(this);
        }

        protected abstract void CreateNode(ReanimatorNode existingNode);

        protected SerializedObject serializedNode;

        protected virtual SerializedObject FindOrGetSerializedNode()
        {
            if (serializedNode == null)
                serializedNode = new SerializedObject(LinkedReanimNode);
            return serializedNode;
        }

        public virtual bool ControlDriversMatch(string name)
        {
            return FindOrGetSerializedNode().FindProperty("controlDriver").FindPropertyRelative("name").stringValue == name;
        }

        public virtual void SetControlDriverName(string name)
        {
            FindOrGetSerializedNode().FindProperty("controlDriver").FindPropertyRelative("name").stringValue = name;
            serializedNode.ApplyModifiedProperties();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            graphView?.SetInspectedNode(this);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            graphView?.ClearInspectedNode(this);
        }

        protected void Rename(string newName)
        {
            string oldName = NodeName;
            NodeName = newName; 
            graphView.ApplyRenamedNode(oldName);
        }

        public void UpdateName(string newName)
        {
            nameTextField.value = newName;
        }

        public void ApplyNameToAsset()
        {
            //string[] labels = AssetDatabase.GetLabels(LinkedReanimNode);
            //AssetDatabase.ClearLabels(LinkedReanimNode);
            LinkedReanimNode.name = NodeName; 
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(LinkedReanimNode), ImportAssetOptions.ForceUpdate);
            //AssetDatabase.SetLabels(LinkedReanimNode, new string[] { NodeName });
            //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(LinkedReanimNode));
            //AssetDatabase.ClearLabels(LinkedReanimNode);
            ForceUpdateProjectWindows();
        }
        private static void ForceUpdateProjectWindows()
        {
            string tempAssetPath = "Assets/temp-987654321.anim";
            AssetDatabase.CreateAsset(new AnimationClip(), tempAssetPath);
            AssetDatabase.DeleteAsset(tempAssetPath);
        }

        public virtual void Draw()
        {
            nameTextField = new()
            {
                value = NodeName,
                isDelayed = true
            };

            nameTextField.RegisterValueChangedCallback(newName=>Rename(newName.newValue));

            titleContainer.Insert(0, nameTextField);

            referencePort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            referencePort.portName = "Reference";
            inputContainer.Add(referencePort);

            controlDriverPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(int));
            controlDriverPort.portName = "Control Driver";
            inputContainer.Add(controlDriverPort);
        }

        public virtual void DisconnectAllPorts()
        {
            graphView.DeleteElements(referencePort.connections);
            graphView.DeleteElements(controlDriverPort.connections);
        }
    }
}
