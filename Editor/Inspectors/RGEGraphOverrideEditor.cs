using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Aarthificial.Reanimation.Nodes;

namespace Tomatech.ReanimationGraph.Editor.Inspectors
{
    [CustomEditor(typeof(RGEGraphOverride))]
    public class RGEGraphOverrideEditor : UnityEditor.Editor
    {
        private SerializedProperty _next;
        private SerializedProperty _override;

        private List<TerminationNode> subAssetNodes;
        private List<TerminationNode> looseSubAssetNodes;

        private void OnEnable()
        {
            _next = serializedObject.FindProperty("graphNode");
            _override = serializedObject.FindProperty("overrides");

            looseSubAssetNodes = new List<TerminationNode>();
            subAssetNodes = new List<TerminationNode>();

            RegeneratePairs();
            RecountSubAssets();
        }

        void RegeneratePairs()
        {
            if (_next.objectReferenceValue is RGEGraphContainer graph)
            {
                bool changed = false;
                List<TerminationNode> graphTerminationNodes = graph.GetTerminationNodes();
                List<OverridePair> currentPairs = new();
                for (int i = 0; i < _override.arraySize; i++)
                {
                    TerminationNode fromNode = _override.GetArrayElementAtIndex(i).FindPropertyRelative("fromNode").objectReferenceValue as TerminationNode;
                    if (graphTerminationNodes.Contains(fromNode))
                    {
                        graphTerminationNodes.RemoveAll(x => x == fromNode);
                    }
                    else
                    {
                        _override.DeleteArrayElementAtIndex(i);
                        changed = true;
                        i--;
                    }
                }
                foreach (TerminationNode node in graphTerminationNodes)
                {
                    int index = _override.arraySize;
                    _override.InsertArrayElementAtIndex(index);
                    _override.GetArrayElementAtIndex(index).FindPropertyRelative("fromNode").objectReferenceValue = node;
                    changed = true;
                }
                if (changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        void RecountSubAssets()
        {
            looseSubAssetNodes.Clear();
            subAssetNodes.Clear();
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(serializedObject.targetObject));
            foreach (Object subAsset in subAssets)
            {
                if (subAsset == serializedObject.targetObject)
                    continue;
                if (subAsset is TerminationNode subNode)
                    looseSubAssetNodes.Add(subNode);
            }

            for (int i = 0; i < _override.arraySize; i++)
            {
                TerminationNode toNode = _override.GetArrayElementAtIndex(i).FindPropertyRelative("toNode").objectReferenceValue as TerminationNode;
                if (looseSubAssetNodes.Contains(toNode))
                {
                    subAssetNodes.Add(toNode);
                    looseSubAssetNodes.Remove(toNode);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_next);
            if (EditorGUI.EndChangeCheck())
            {
                //serializedObject.ApplyModifiedProperties();
                RegeneratePairs();
            }

            if (_next.objectReferenceValue && _override.arraySize>0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Overrides");
                for (int i = 0; i < _override.arraySize; i++)
                {
                    DrawNodePairField(_override.GetArrayElementAtIndex(i));
                }
            }

            if (looseSubAssetNodes.Count>0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Loose Sub-assets");

                for (int i = 0; i < looseSubAssetNodes.Count; i++)
                {
                    Rect space = EditorGUILayout.GetControlRect();
                    Rect objectRect = new(space.x, space.y, space.width - space.height, space.height);
                    Rect removeButton = new(space.x + space.width - space.height, space.y, space.height, space.height);
                    GUI.enabled = false;
                    EditorGUI.ObjectField(objectRect, looseSubAssetNodes[i], typeof(TerminationNode), false);
                    GUI.enabled = true;
                    if (GUI.Button(removeButton, "-"))
                    {
                        AssetDatabase.RemoveObjectFromAsset(looseSubAssetNodes[i]);
                        DestroyImmediate(looseSubAssetNodes[i]);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(serializedObject.targetObject));

                        looseSubAssetNodes.RemoveAt(i);
                        i--;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawNodePairField(SerializedProperty nodePair)
        {
            Rect space = EditorGUILayout.GetControlRect();

            Rect leftButton = new(space.x, space.y, space.height, space.height);
            Rect left = new(space.x + space.height, space.y, (space.width * 0.5f) - (space.height * 1.5f), space.height);
            Rect midButton = new(space.x + (space.width * 0.5f) - (space.height * 0.5f), space.y, space.height, space.height);
            Rect right = new(space.x + (space.width * 0.5f) + (space.height * 0.5f), space.y, (space.width * 0.5f) - (space.height * 1.5f), space.height);
            Rect rightButton = new(space.x + space.width - space.height, space.y, space.height, space.height);

            SerializedProperty fromProp = nodePair.FindPropertyRelative("fromNode");
            SerializedProperty toProp = nodePair.FindPropertyRelative("toNode");

            EditorGUI.BeginChangeCheck();
            if (GUI.Button(leftButton, new GUIContent("=", "Match references, effectively disabling the override")))
            {
                toProp.objectReferenceValue = fromProp.objectReferenceValue;
            }

            GUI.enabled = false;
            EditorGUI.ObjectField(left, fromProp.objectReferenceValue, typeof(TerminationNode), false);
            GUI.enabled = true;

            GUI.Label(midButton, "->");


            toProp.objectReferenceValue = EditorGUI.ObjectField(right, toProp.objectReferenceValue, typeof(TerminationNode), false);
            if (EditorGUI.EndChangeCheck())
            {
                RecountSubAssets();
            }

            if (subAssetNodes.Contains(toProp.objectReferenceValue as TerminationNode))
            {
                GUI.enabled = false;
                GUI.Button(rightButton, new GUIContent("*", "Clone node as editable sub-object"));
                GUI.enabled = true;
            }
            else if(GUI.Button(rightButton, new GUIContent("*", "Clone node as editable sub-object")))
            {
                if (looseSubAssetNodes.Exists(x=>x.name== "Ovr_" + fromProp.objectReferenceValue.name))
                {
                    toProp.objectReferenceValue = looseSubAssetNodes.Find(x => x.name == "Ovr_" + fromProp.objectReferenceValue.name);
                    RecountSubAssets();
                }
                else
                {
                    TerminationNode newNode = CreateInstance(fromProp.objectReferenceValue.GetType()) as TerminationNode;
                    EditorUtility.CopySerialized(fromProp.objectReferenceValue, newNode);
                    newNode.name = "Ovr_" + fromProp.objectReferenceValue.name;
                    AssetDatabase.AddObjectToAsset(newNode, serializedObject.targetObject);
                    toProp.objectReferenceValue = newNode;
                    subAssetNodes.Add(newNode);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(serializedObject.targetObject));
                }
            }
        }
    }
}
