#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Search;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System;
using System.ComponentModel;

public class InspectorSearchWindow : EditorWindow
{
    public enum PropertyTreeNodeType { Component, Foldout, Array, Property }
    public struct PropertyTreeNode
    {
        public PropertyTreeNodeType type;
        // only valid if type is { Component, Foldout, Array }
        public string name;
        // only valid if parent is type Array
        public int elementI;
        public SerializedObject component;
        public bool foldout;
        public List<PropertyTreeNode> children;
        public SerializedProperty property;
    }

    [MenuItem("Tools/Inspector Search Window")]
    public static void ShowWindow()
    {
        Type ByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
            {
                var tt = assembly.GetType(name);
                if (tt != null)
                {
                    return tt;
                }
            }

            return null;
        }

        GetWindow<InspectorSearchWindow>(ByName("UnityEditor.InspectorWindow"));
    }

    public string searchProperty = string.Empty;
    public bool searchHidden = false;
    public GameObject[] lastSelectedGameObjects = null;
    public List<PropertyTreeNode> matchingProperties;

    public Vector2 scrollPosition;

    public void OnGUI()
    {
        using var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition);
        scrollPosition = scrollViewScope.scrollPosition;

        bool search = HasSelectionChanged();

        EditorGUI.BeginChangeCheck();
        searchProperty = EditorGUILayout.TextField("Property Name", searchProperty);
        searchHidden = EditorGUILayout.Toggle("Search Hidden Properties", searchHidden);
        if (EditorGUI.EndChangeCheck())
        {
            search = true;
        }
        if (GUILayout.Button("Search", GUILayout.MaxWidth(300)) ||
            search)
        {
            SearchProperties();
        }
        if (GUILayout.Button("Clear", GUILayout.MaxWidth(300)))
        {
            matchingProperties.Clear();
            searchProperty = string.Empty;
        }

        if (matchingProperties != null &&
            matchingProperties.Any())
        {
            void VisitChildren(List<PropertyTreeNode> children)
            {
                foreach (ref PropertyTreeNode node in children.AsSpan())
                {
                    switch (node.type)
                    {
                        case PropertyTreeNodeType.Foldout:
                        case PropertyTreeNodeType.Array:
                            node.foldout = EditorGUILayout.Foldout(node.foldout, node.name, true);
                            if (node.foldout)
                            {
                                EditorGUI.indentLevel++;
                                VisitChildren(node.children);
                                EditorGUI.indentLevel--;
                            }
                            break;
                        case PropertyTreeNodeType.Property:
                            EditorGUILayout.PropertyField(node.property, true);
                            break;
                    }
                }
            }

            for (int matchI = 0; matchI < matchingProperties.Count; matchI++)
            {
                ref var matchingProperty = ref matchingProperties.AsSpan()[matchI];

                EditorGUILayout.Separator();
                matchingProperty.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(matchingProperty.foldout, matchingProperty.name);
                EditorGUILayout.EndFoldoutHeaderGroup();

                if (matchingProperty.foldout)
                {
                    matchingProperty.component.Update();
                    VisitChildren(matchingProperty.children);
                    matchingProperty.component.ApplyModifiedProperties();
                }
            }
        }
        else
        {
            GUILayout.Label("No results.");
        }
    }

    public bool HasSelectionChanged()
    {
        GameObject[] gameObjects = Selection.gameObjects;
        if (lastSelectedGameObjects == null ||
            !lastSelectedGameObjects.SequenceEqual(gameObjects))
        {
            lastSelectedGameObjects = (GameObject[])gameObjects.Clone();
            return true;
        }
        return false;

    }

    public void SearchProperties()
    {
        if (searchProperty == null)
            searchProperty = string.Empty;

        matchingProperties ??= new List<PropertyTreeNode>();
        matchingProperties.Clear();

        // start with fuzzy. if we find exact substring, then narrow down all results to substring
        bool checkFuzzy = true;
        List<(SerializedObject, List<SerializedProperty>)> candidates = new();

        // search properties in gameObjects
        foreach (GameObject gameObject in lastSelectedGameObjects)
        {
            for (int componentI = 0; componentI < gameObject.GetComponentCount(); componentI++)
            {
                var component = new SerializedObject(gameObject.GetComponentAtIndex(componentI));

                SerializedProperty property = component.GetIterator();
                property.Next(true);

                List<SerializedProperty> candidateProperties = new();
                bool enterChildren = true;
                while (true)
                {
                    if (!searchHidden || property.isArray)
                    {
                        if (!property.NextVisible(enterChildren))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (!property.Next(enterChildren))
                        {
                            break;
                        }
                    }

                    if (property.displayName.Contains(searchProperty, StringComparison.CurrentCultureIgnoreCase))
                    {
                        checkFuzzy = false;
                        enterChildren = false;
                        candidateProperties.Add(property.Copy());
                    }
                    else if (checkFuzzy && FuzzySearch.FuzzyMatch(searchProperty, property.displayName))
                    {
                        // don't need both parent and child in matches. DrawProperty can display children
                        enterChildren = false;
                        candidateProperties.Add(property.Copy());
                    }
                    else
                        enterChildren = true;
                }

                candidates.Add((component, candidateProperties));
            }
        }

        // categorise into hierachy
        foreach (var (component, candidateProperties) in candidates)
        {
            var root = new PropertyTreeNode()
            {
                type = PropertyTreeNodeType.Component,
                name = ObjectNames.NicifyVariableName(component.targetObject.GetType().Name),
                component = component,
                children = new List<PropertyTreeNode>(),
                foldout = true,
            };

            foreach (SerializedProperty property in candidateProperties)
            {
                if (!checkFuzzy && !property.displayName.Contains(searchProperty, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                string[] pathParts = property.PropertyPathParts();

                ref PropertyTreeNode findSuitableHome = ref root;
                for (int pathPartsI = 0; pathPartsI < pathParts.Length; pathPartsI++)
                {
                    string pathPart = pathParts[pathPartsI];
                    if (pathPart.EndsWith(']'))
                    {
                        int indexStart = pathPart.IndexOf('[');
                        string arrayName = pathPart.Substring(0, indexStart);
                        string elementString = pathPart.Substring(indexStart + 1, pathPart.Length - indexStart - 2);
                        int elementI = Int32.Parse(elementString);

                        int findArrayIndex = findSuitableHome.children.FindIndex(
                            x => x.type == PropertyTreeNodeType.Array && x.name == arrayName);

                        PropertyTreeNode NewArrayElement()
                        {
                            return new PropertyTreeNode
                            {
                                type = PropertyTreeNodeType.Foldout,
                                name = $"Element {elementString}",
                                elementI = elementI,
                                children = new(),
                            };
                        };

                        if (findArrayIndex == -1)
                        {
                            findSuitableHome.children.Add(new PropertyTreeNode
                            {
                                type = PropertyTreeNodeType.Array,
                                name = arrayName,
                                children = new List<PropertyTreeNode> { NewArrayElement() },
                            });
                            findSuitableHome = ref findSuitableHome.children[^1].children.AsSpan()[0];
                        }
                        else
                        {
                            int findElementIndex = findSuitableHome.children[findArrayIndex].children.FindIndex(x => x.elementI == elementI);
                            if (findElementIndex == -1)
                            {
                                findSuitableHome.children[findArrayIndex].children.Add(NewArrayElement());
                                findSuitableHome = ref findSuitableHome.children[findArrayIndex].children.AsSpan()[^1];
                            }
                            else
                            {
                                findSuitableHome = ref findSuitableHome.children[findArrayIndex].children.AsSpan()[findElementIndex];
                            }
                        }
                    }
                    else if (pathPartsI < pathParts.Length - 1)
                    {
                        int findFoldout = findSuitableHome.children.FindIndex(
                            x => x.type == PropertyTreeNodeType.Foldout && x.name == pathPart);
                        if (findFoldout == -1)
                        {
                            findSuitableHome.children.Add(new PropertyTreeNode
                            {
                                type = PropertyTreeNodeType.Foldout,
                                name = pathPart,
                                foldout = true,
                                children = new List<PropertyTreeNode>()
                            });
                            findSuitableHome = ref findSuitableHome.children.AsSpan()[^1];
                        }
                        else
                        {
                            findSuitableHome = ref findSuitableHome.children.AsSpan()[findFoldout];
                        }
                    }
                    else
                    {
                        findSuitableHome.children.Add(new PropertyTreeNode());
                        findSuitableHome = ref findSuitableHome.children.AsSpan()[^1];
                    }
                }

                findSuitableHome.type = PropertyTreeNodeType.Property;
                findSuitableHome.property = property;
            }


            if (0 < root.children.Count)
                matchingProperties.Add(root);
        }

        // nicify string names
        void NicifyChildren(List<PropertyTreeNode> children)
        {
            foreach (ref PropertyTreeNode node in children.AsSpan())
            {
                switch (node.type)
                {
                    case PropertyTreeNodeType.Component:
                    case PropertyTreeNodeType.Foldout:
                    case PropertyTreeNodeType.Array:
                        node.name = ObjectNames.NicifyVariableName(node.name);
                        NicifyChildren(node.children);
                        break;
                    case PropertyTreeNodeType.Property:
                        break;
                }
            }
        }
        NicifyChildren(matchingProperties);
    }
}
#endif