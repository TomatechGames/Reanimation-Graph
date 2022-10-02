using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tomatech.ReanimationGraph.RGEditor
{
    public class RGEEditorWindow : EditorWindow
    {
        [MenuItem("Window/Tomatech/Reanimation Node Editor")]
        public static void ShowExample()
        {
            GetWindow<RGEEditorWindow>();
        }

        [OnOpenAsset]
        //Handles opening the editor window when double-clicking graph files
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if(EditorUtility.InstanceIDToObject(instanceID) is RGEGraphContainer)
            {
                bool windowIsOpen = HasOpenInstances<RGEEditorWindow>();
                if (!windowIsOpen)
                {
                    CreateWindow<RGEEditorWindow>();
                }
                else
                {
                    FocusWindowIfItsOpen<RGEEditorWindow>();
                }
            }
            return false;
        }

        RGEGraphView graphView;
        RGEGraphContainer target;
        public RGEGraphContainer Target => target;

        public void CreateGUI()
        {
            position = new Rect(position.position, new Vector2(1200, 700));
            AddGraphView();
            AddStyles();
            TrySetTarget(Selection.activeObject);
        }

        void AddGraphView()
        {
            graphView = new(this);
            rootVisualElement.Add(graphView);
            graphView.StretchToParentSize();
        }

        private void AddStyles()
        {
            StyleSheet stylesheet = EditorGUIUtility.Load("ReanimatorNodeEditor/RNVariables.uss") as StyleSheet;

            rootVisualElement.styleSheets.Add(stylesheet);
        }

        private void OnSelectionChange()
        {
            TrySetTarget(Selection.activeObject);
        }

        void TrySetTarget(Object possibleTarget)
        {
            bool hadTarget = target;
            if (possibleTarget is RGEGraphContainer newTarget)
                target = newTarget;
            else
                target = null;

            if (target)
                graphView.BuildGraph();
            else if (hadTarget)
                graphView.ClearGraph();
        }

        public void DeselectTarget()
        {
            graphView.ClearGraph();
            target = null;
        }
    }

    public class RGEGraphContainerDeleteDetector : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(RGEGraphContainer))
            {
                var activeWindow = EditorWindow.GetWindow<RGEEditorWindow>("Reanimation Node Editor", false);
                if (activeWindow.Target != null && path == AssetDatabase.GetAssetPath(activeWindow.Target.GetInstanceID()))
                    activeWindow.DeselectTarget();
            }
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
