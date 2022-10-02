using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace Tomatech.ReanimationGraph.RGEditor
{
    public class RGENodeInspector : GraphElement
    {
        private VisualElement m_MainContainer;

        private VisualElement m_Root;

        private Label m_SubTitleLabel;

        private VisualElement m_ContentContainer;

        private IMGUIContainer m_IMGUIContainer;

        private Dragger m_Dragger;

        private GraphView m_GraphView;

        //
        // Summary:
        //     The GraphView that the Blackboard is attached to.
        public GraphView graphView
        {
            get
            {
                if (m_GraphView == null)
                {
                    m_GraphView = GetFirstAncestorOfType<GraphView>();
                }

                return m_GraphView;
            }
        }

        //
        // Summary:
        //     The subtitle of this Blackboard.
        public string subTitle
        {
            get
            {
                return m_SubTitleLabel.text;
            }
            set
            {
                m_SubTitleLabel.text = value;
            }
        }

        public void SetEditor(Editor customEditor)
        {
            if (customEditor)
            {
                m_IMGUIContainer.onGUIHandler = customEditor.OnInspectorGUI;
                subTitle = customEditor.target.GetType().Name;
            }
            else
            {
                m_IMGUIContainer.onGUIHandler = null;
                subTitle = "";
            }
        }

        public RGENodeInspector(GraphView associatedGraphView = null)
        {
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("RNNodeInspector"); 
            m_MainContainer = visualTreeAsset.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");
            m_Root = m_MainContainer.Q("content");
            //m_HeaderItem = m_MainContainer.Q("header");
            //m_HeaderItem.AddToClassList("inspectorHeader");
            m_SubTitleLabel = m_MainContainer.Q<Label>("subTitleLabel");
            m_ContentContainer = m_MainContainer.Q("contentContainer");
            RemoveFromClassList("graphElement");
            hierarchy.Add(m_MainContainer);

            subTitle = "";

            m_IMGUIContainer = new();
            m_ContentContainer.Add(m_IMGUIContainer);

            //capabilities = (Capabilities.Movable | Capabilities.Resizable);
            style.overflow = Overflow.Hidden;
            //ClearClassList();
            //AddToClassList("blackboard");
            m_Dragger = new Dragger
            {
                clampToParentEdges = true,
            };
            //this.AddManipulator(m_Dragger);
            //hierarchy.Add(new Resizer());
            RegisterCallback(delegate (DragUpdatedEvent e)
            {
                e.StopPropagation();
            });
            RegisterCallback(delegate (WheelEvent e)
            {
                e.StopPropagation();
            });
            RegisterCallback(delegate (MouseDownEvent e)
            {
                if (e.button == 0)
                {
                    ClearSelection();
                }

                e.StopPropagation();
            });
            m_GraphView = associatedGraphView;
            focusable = true;

            style.width = new Length(370);
            style.height = new Length(100, LengthUnit.Percent);
            style.minWidth = new Length(200);
            style.minHeight = new Length(200);
            style.maxWidth = new Length(100, LengthUnit.Percent);
            style.maxHeight = new Length(100, LengthUnit.Percent);

            m_MainContainer.style.width = new Length(100, LengthUnit.Percent);
            m_MainContainer.style.height = new Length(100, LengthUnit.Percent);

            style.position = Position.Absolute;
            style.right = new Length(0);
            style.top = new Length(0);
            style.bottom = new StyleLength(StyleKeyword.Auto);
            style.left = new StyleLength(StyleKeyword.Auto);
        }

        //
        // Summary:
        //     Clears the selection in the GraphView that the Blackboard is attached to.
        public virtual void ClearSelection()
        {
            //graphView?.ClearSelection();
        }

    }
}
