using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.UIElements;

namespace Tomatech.ReanimationGraph.Editor
{
    using Aarthificial.Reanimation.Cels;
    using Aarthificial.Reanimation.Nodes;
    using Elements;
    using System.Linq;
    using UnityEditor.UIElements;

    public class RGEGraphView : GraphView
    {

        public RGEEditorWindow editorWindow;
        Dictionary<string, RGENodeBase> registeredNodes = new();
        List<string> drivers = new();
        Dictionary<string, List<TokenNode>> driverInstances = new();
        VisualElement driverParent;
        RGENodeInspector nodeInspectorElement;
        Node rootNode;
        Port rootOutput;

        public RGENodeBase GetNodeByName(string key) => registeredNodes[key];

        public RGEGraphView(RGEEditorWindow editorWindow)
        {
            this.editorWindow = editorWindow;
            AddManipulators();
            AddGridBackground();
            styleSheets.Add(Resources.Load<StyleSheet>("RNGraphViewStyles")); 
            AddBlackboardAndInspector();
            ElementsDeletedCallback();
            graphViewChanged = OnGraphChange;
            CreateRootNode();
        }

        private void CreateRootNode()
        {
            rootNode = new();
            Label nameLabel = new("Root Node");
            rootNode.titleContainer.Insert(0, nameLabel);

            rootOutput = rootNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            rootOutput.portName = "Reference";
            rootNode.outputContainer.Add(rootOutput);
        }

        public void BuildGraph()
        {
            ClearGraph();

            foreach (var item in editorWindow.Target.DriverBlackboard)
            {
                AddNamedDriver(item);
            }

            for (int i = 0; i < editorWindow.Target.NodeData.Count; i++)
            {
                RGEGraphContainer.NodeInstanceData item = editorWindow.Target.NodeData[i];
                switch (item.node)
                {
                    case SwitchNode:
                        AddElement(CreateNode<RGESwitchNode>(item.position, item.node));
                        break;
                    case SimpleAnimationNode:
                        AddElement(CreateNode<RGEAnimationNode<SimpleAnimationNode, SimpleCel>>(item.position, item.node));
                        break;
                }
            }

            List<TokenNode> driversToUpdate = new();
            foreach (var item in editorWindow.Target.DriverData)
            {
                TokenNode driverNode = CreateDriverToken(item.name, item.guid, item.position);
                foreach (var connectorNode in item.connectionNames)
                {
                    if (!registeredNodes.ContainsKey(connectorNode))
                    {
                        Debug.LogWarning($"due to a previous internal naming error, a \"{item.name}\" driver instance failed to find a node named \"{connectorNode}\"");
                        driversToUpdate.Add(driverNode);
                        continue;
                    }
                    RGENodeBase rgeNodeBase = registeredNodes[connectorNode];
                    if (rgeNodeBase.ControlDriversMatch(item.name))
                    {
                        AddElement(driverNode.output.ConnectTo(rgeNodeBase.ControlDriverPort));
                    }
                }
                AddElement(driverNode);
            }
            foreach (var item in driversToUpdate)
                editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(item));

            foreach (var item in registeredNodes.Values.Where(n=>n is RGESwitchNode))
            {
                (item as RGESwitchNode).ReconnectPorts();
            }

            AddElement(rootNode);
            rootNode.SetPosition(new Rect(editorWindow.Target.rootPos, Vector2.zero));
            if (editorWindow.Target.RootNode && registeredNodes.ContainsKey(editorWindow.Target.RootNode.name))
                AddElement(rootOutput.ConnectTo(registeredNodes[editorWindow.Target.RootNode.name].ReferencePort));

            FrameAll();
            ClearSelection();
        }

        public void ClearGraph()
        {
            ClearSelection();
            rootOutput.DisconnectAll();
            RemoveElement(rootNode);
            DeleteElements(registeredNodes.Values);
            foreach (var item in driverInstances)
            {
                DeleteElements(item.Value);
                item.Value.Clear();
            }
            driverParent.Clear();
            DeleteElements(edges);
            registeredNodes.Clear();
            driverInstances.Clear();
            drivers.Clear();
        }


        public static Vector2 NodePosition(Node node) => new(node.style.left.value.value, node.style.top.value.value);

        private GraphViewChange OnGraphChange(GraphViewChange change)
        {
            if(change.edgesToCreate!=null)
                foreach (var item in change.edgesToCreate)
                {
                    if (item.input.portType == typeof(bool))
                    {
                        // assign reference from input to output
                        if (item.output.node == rootNode)
                        {
                            editorWindow.Target.SetRootNode((item.input.node as RGENodeBase).LinkedReanimNode);
                        }
                        else
                        {
                            RGESwitchNode switchNode = item.output.node as RGESwitchNode;
                            switchNode.AssignReferenceToPortIndex(item.output, (item.input.node as RGENodeBase).LinkedReanimNode);
                        }
                    }
                    if (item.input.portType == typeof(int))
                    {
                        // assign driver from output to input
                        RGENodeBase rgeNode = item.input.node as RGENodeBase;
                        TokenNode tokenNode = item.output.node as TokenNode;
                        rgeNode.SetControlDriverName(tokenNode.title);
                        item.output.Connect(item);
                        editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(tokenNode));
                    }
                }
            if(change.movedElements!=null)
                
                foreach (var item in change.movedElements)
                {
                    if (item == rootNode)
                    {
                        editorWindow.Target.rootPos = NodePosition(rootNode);
                        continue;
                    }
                    if (item is RGENodeBase rgeNodeItem)
                    {
                        editorWindow.Target.CreateOrUpdateNode(rgeNodeItem);
                        continue;
                    }
                    if (item is TokenNode rgeTokenItem)
                    {
                        editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(rgeTokenItem));
                        continue;
                    }
                }
            return change;
        }

        RGEGraphContainer.DriverInstanceData CreateTokenData(TokenNode rgeTokenItem)
        {
            RGEGraphContainer.DriverInstanceData tokenData = new();
            tokenData.position = NodePosition(rgeTokenItem);
            tokenData.guid = rgeTokenItem.name;
            tokenData.name = rgeTokenItem.title;
            tokenData.connectionNames = rgeTokenItem.output.connections.Select(c => (c.input.node as RGENodeBase).NodeName).ToArray();
            return tokenData;
        }

        private void AddBlackboardAndInspector()
        {
            Blackboard blackboard = new(this);

            blackboard.style.width = new Length(200);
            blackboard.style.height = new Length(350);
            blackboard.style.minWidth = new Length(200);
            blackboard.style.minHeight = new Length(200);
            blackboard.style.maxWidth = new Length(100, LengthUnit.Percent);
            blackboard.style.maxHeight = new Length(100, LengthUnit.Percent);

            blackboard.title = "Temp Name";
            blackboard.subTitle = "Drivers";

            blackboard.editTextRequested += ApplyRenamedDriver;
            blackboard.addItemRequested += AddDriverToBlackboard;

            blackboard.scrollable = true;

            //ScrollView scrollView = new();
            //scrollView.style.width = new Length(100,LengthUnit.Percent);
            //scrollView.style.height = new Length(100, LengthUnit.Percent);
            //scrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            //blackboard.Add(scrollView);

            driverParent = blackboard.contentContainer;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);

            nodeInspectorElement = new(this);

            Add(nodeInspectorElement);
            Add(blackboard);
        }

        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (DragAndDrop.GetGenericData("DragSelection") is List<ISelectable> selection && (selection.OfType<BlackboardField>().Count() >= 0))
            {
                DragAndDrop.visualMode = e.actionKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
            }
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            IEnumerable<BlackboardField> fields = selection.OfType<BlackboardField>();
            foreach (BlackboardField field in fields)
            {
                TokenNode driverNode = CreateDriverToken(field.text, GUID.Generate().ToString(), WindowToGraphPosition(e.localMousePosition));
                AddElement(driverNode);
                editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(driverNode));
            }
        }

        TokenNode CreateDriverToken(string name, string guid, Vector2 pos)
        {
            Port driverPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(int));
            TokenNode driverNode = new(null, driverPort);
            driverNode.SetPosition(new Rect(pos, Vector2.zero));
            driverNode.name = guid;
            driverNode.title = name;
            driverNode.output.portName = "";
            driverNode[0].style.height = 50;
            if (!driverInstances.ContainsKey(name))
                driverInstances.Add(name, new());
            driverInstances[name].Add(driverNode);
            return driverNode;
        }

        void AddDriverToBlackboard(Blackboard b)
        {
            if (!editorWindow.Target)
                return;
            string fieldName = IncrementName("New Driver", drivers);
            AddNamedDriver(fieldName);
        }

        void AddNamedDriver(string fieldName)
        {
            BlackboardField field = new(null, fieldName, "");
            drivers.Add(fieldName);
            driverParent.Add(field);
            field.RegisterCallback<ContextualMenuPopulateEvent>(CreateDriverContextMenu);
            editorWindow.Target.SetDriverBlackboard(drivers.ToArray());
        }

        public void SetInspectedNode(RGENodeBase node)
        {
            nodeInspectorElement.SetEditor(node.NodeInspector);
        }

        public void ClearInspectedNode(RGENodeBase oldNode)
        {
            //List<ISelectable> filteredSelection = selection.Where(s=>s != oldNode && s.IsSelected(this)).ToList();
            //if (filteredSelection.Count > 0 && filteredSelection.Last() is RGENodeBase node)
            //{
            //    nodeInspectorElement.SetEditor(node.NodeInspector);
            //}
            //else
                nodeInspectorElement.SetEditor(null);
        }

        private void CreateDriverContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete/This deletes all instances of this driver.\n Are you sure?/REALLY Sure?", RemoveDriver, DropdownMenuAction.AlwaysEnabled, evt.currentTarget);
        }

        void ApplyRenamedDriver(Blackboard b, VisualElement fieldElement, string newName)
        {
            BlackboardField field = fieldElement as BlackboardField;
            if (drivers.Contains(field.text))
            {
                drivers.Remove(field.text);
            }
            newName = IncrementName(newName, drivers);
            string oldName = field.text;
            field.text = newName;
            drivers.Add(newName);

            if (driverInstances.ContainsKey(oldName))
            {
                var instanceList = driverInstances[oldName];
                foreach (var item in instanceList)
                {
                    item.title = newName;
                }
                driverInstances.Remove(oldName);
                driverInstances.Add(newName, instanceList);
            }

            editorWindow.Target.RenameDriverInstances(oldName, newName);
            editorWindow.Target.SetDriverBlackboard(drivers.ToArray());
        }

        void RemoveDriver(DropdownMenuAction evt)
        {
            BlackboardField field = evt.userData as BlackboardField;
            if (driverInstances.ContainsKey(field.text))
            {
                foreach (var item in driverInstances[field.text])
                {
                    DeleteElements(item.output.connections);
                    RemoveElement(item);
                }
                driverInstances.Remove(field.text);
            }
            drivers.Remove(field.text);
            driverParent.Remove(field);
            editorWindow.Target.RemoveDriversWithName(field.text);
            editorWindow.Target.SetDriverBlackboard(drivers.ToArray());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new();

            ports.ForEach(currentPort =>
            {
                if (startPort.node == currentPort.node || startPort.direction == currentPort.direction)
                    return;

                Port originPort = startPort.direction == Direction.Output ? startPort : currentPort;

                if(startPort.portType==currentPort.portType)
                    compatiblePorts.Add(currentPort);
            });

            return compatiblePorts;
        }

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(CreateNodeContextMenu<RGESwitchNode>("Create Node/Switch Node"));
            this.AddManipulator(CreateNodeContextMenu<RGEAnimationNode<SimpleAnimationNode, SimpleCel>>("Create Node/Simple Animation Node"));
        }

        private IManipulator CreateNodeContextMenu<T>(string actionName) where T : RGENodeBase, new()
        {
            ContextualMenuManipulator contextualMenuManipulator = new(
                menuEvent =>
                {
                    if (!editorWindow.Target)
                        return;
                    if (menuEvent.target == this)
                        menuEvent.menu.InsertAction(0, actionName, actionEvent => AddElement(CreateNode<T>(WindowToGraphPosition(actionEvent.eventInfo.localMousePosition))));
                }
                );

            return contextualMenuManipulator;
        }

        private RGENodeBase CreateNode<T>(Vector2 pos, ReanimatorNode existingNode = null) where T : RGENodeBase , new()
        {
            T node = new();
            node.Initialize(pos, this, existingNode);
            if (registeredNodes.ContainsKey(node.NodeName))
            {
                Debug.LogWarning("name duplicate found, attempting to assign new name");
                node.NodeName = IncrementName(node.NodeName, registeredNodes.Keys.ToList());
                node.ApplyNameToAsset();
            }
            registeredNodes.Add(node.NodeName, node);
            node.Draw();
            node.RefreshExpandedState();

            ClearSelection();
            AddToSelection(node);

            return node;
        }

        public string GetNewNodeName(string startName) => IncrementName(startName, registeredNodes.Keys.ToList());

        string IncrementName(string startName, List<string> existing)
        {
            if (string.IsNullOrWhiteSpace(startName))
                startName = "Unnamed";

            if (!existing.Contains(startName))
                return startName;

            while (existing.Contains(startName))
            {
                string[] splitName = startName.Split(' ');
                if (int.TryParse(splitName[^1], out int endValue))
                {
                    while (existing.Contains(startName))
                    {
                        endValue++;
                        splitName[^1] = endValue.ToString();
                        startName = string.Join(" ", splitName);
                    }
                }
                else
                {
                    startName += " 1";
                }
            }

            return startName;
        }

        public void ApplyRenamedNode(string oldName)
        {
            if (!registeredNodes.ContainsKey(oldName))
            {
                Debug.LogWarning("node is not registered");
                return;
            }
            RGENodeBase node = registeredNodes[oldName];

            if (registeredNodes.ContainsKey(node.NodeName) || string.IsNullOrWhiteSpace(node.NodeName))
            {
                string newName = IncrementName(node.NodeName, registeredNodes.Keys.ToList());
                node.NodeName = oldName;
                node.UpdateName(newName);
            }
            else
            {
                registeredNodes.Remove(oldName);
                registeredNodes.Add(node.NodeName, node);
                if (node.ControlDriverPort.connected)
                    editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(node.ControlDriverPort.connections.First().output.node as TokenNode));
                node.ApplyNameToAsset();
            }
        }

        void ElementsDeletedCallback()
        {
            deleteSelection = (operation, askUser) =>
            {
                DeleteElementsRGE(selection);
            };
        }

        void DeleteElementsRGE(List<ISelectable> toDelete)
        {
            List<RGENodeBase> nodesToDelete = new();
            List<Edge> edgesToDelete = new();
            List<TokenNode> driverTokensToDelete = new();

            foreach (GraphElement item in toDelete)
            {
                if (item is RGENodeBase node)
                {
                    nodesToDelete.Add(node);
                }
                else if (item is Edge edge)
                {
                    edgesToDelete.Add(edge);
                }
                else if (item is TokenNode token)
                {
                    driverTokensToDelete.Add(token);
                }
            }

            foreach (var item in nodesToDelete)
            {

                if (item.ReferencePort.connected)
                {
                    if (item.ReferencePort.connections.First().output.node == rootNode)
                    {
                        editorWindow.Target.SetRootNode(null);
                    }
                    else
                    {
                        Port switchOutput = item.ReferencePort.connections.First().output;
                        (switchOutput.node as RGESwitchNode)?.RemoveReferenceFromPortIndex(switchOutput);
                    }
                }

                if (item.ControlDriverPort.connected)
                    editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(item.ControlDriverPort.connections.First().output.node as TokenNode));

                item.DisconnectAllPorts();
                registeredNodes.Remove(item.NodeName);
                RemoveElement(item);
                editorWindow.Target.RemoveNode(item.LinkedReanimNode);

            }
            foreach (var item in driverTokensToDelete)
            {
                DeleteElements(item.output.connections);
                RemoveElement(item);
                driverInstances[item.title].Remove(item);
                if (driverInstances[item.title].Count == 0)
                    driverInstances.Remove(item.title);
                editorWindow.Target.RemoveDriver(item.name);
            }
            foreach (var item in edgesToDelete)
            {
                if (item?.input?.portType == typeof(bool))
                {
                    if (item.output.node == rootNode)
                    {
                        editorWindow.Target.SetRootNode(null);
                    }
                    else
                    {
                        RGESwitchNode switchNode = item.output.node as RGESwitchNode;
                        switchNode?.RemoveReferenceFromPortIndex(item.output);
                    }
                }
                if (item?.input?.portType == typeof(int))
                {
                    RGENodeBase rgeNode = item.input.node as RGENodeBase;
                    rgeNode.SetControlDriverName("");
                    item.output.Disconnect(item);
                    editorWindow.Target.CreateOrUpdateDriver(CreateTokenData(item.output.node as TokenNode));
                }
            }
            DeleteElements(edgesToDelete);
        }

        private void AddGridBackground()
        {
            GridBackground gridBackground = new();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
        }

        public Vector2 WindowToGraphPosition(Vector2 mousePos, bool isSearchWindow=false)
        {
            //TODO: search window compensation is inaccurate, replace
            if (isSearchWindow)
                mousePos -= editorWindow.position.position;
            return contentViewContainer.WorldToLocal(mousePos);
        }
    }
}
