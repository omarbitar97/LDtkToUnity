﻿using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LDtkUnity.Editor
{
    internal sealed class LDtkSectionDependencies : LDtkSectionDrawer
    {
        private readonly SerializedObject _serializedObject;
        private string[] _dependencies;
        private Object[] _dependencyAssets;
        private GUIContent[] _dependencyContent;
        
        protected override string GuiText => "Dependencies";
        protected override string GuiTooltip => "Dependencies that were defined since the last import.\n" +
                                                "If any of these dependencies save changes, then this asset will automatically reimport.";
        protected override Texture GuiImage => LDtkIconUtility.GetUnityIcon("UnityEditor.FindDependencies", "");
        protected override string ReferenceLink => LDtkHelpURL.SECTION_DEPENDENCIES;
        protected override bool SupportsMultipleSelection => false;
        
        public LDtkSectionDependencies(LDtkImporterEditor editor, SerializedObject serializedObject) : base(editor, serializedObject)
        {
            _serializedObject = serializedObject;
            UpdateDependencies();
        }

        public void UpdateDependencies() //originally this was updated less regularly for performance, but was difficult to find the right event for.
        {
            if (Editor == null || _serializedObject == null)
            {
                return;
            }

            _serializedObject.Update();
            if (_serializedObject.targetObject == null)
            {
                return;
            }
            
            string importerPath = AssetDatabase.GetAssetPath(_serializedObject.targetObject);
            
            _dependencies = LDtkDependencyCache.Load(importerPath);
            _dependencyAssets = new Object[_dependencies.Length];
            _dependencyContent = new GUIContent[_dependencies.Length];

            for (int i = 0; i < _dependencies.Length; i++)
            {
                _dependencyAssets[i] = AssetDatabase.LoadAssetAtPath<Object>(_dependencies[i]);
                _dependencyContent[i] = new GUIContent
                {
                    text = _dependencyAssets[i].name,
                    tooltip = _dependencies[i],
                    image = GetIconForDependency(_dependencyAssets[i].GetType(), _dependencies[i])
                };
            }
            
            Texture2D GetIconForDependency(Type type, string assetPath)
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                
                if (importer != null && importer is LDtkTilesetImporter)
                {
                    return LDtkIconUtility.LoadTilesetFileIcon();
                }

                return AssetPreview.GetMiniTypeThumbnail(type);
            }
        }

        public override void Draw()
        {
            //don't draw this section at all if there are no dependencies
            if (_dependencyAssets.All(p => p == null))
            {
                return;
            }
            
            LDtkEditorGUIUtility.DrawDivider();
            base.Draw();
        }
        
        protected override void DrawDropdownContent()
        {
            EditorGUIUtility.SetIconSize(Vector2.one * 16f);
            for (int i = 0; i < _dependencies.Length; i++)
            {
                Object dependencyAsset = _dependencyAssets[i];

                if (dependencyAsset == null)
                {
                    continue;
                }
                
                using (new LDtkGUIEnabledScope(false))
                {
                    EditorGUILayout.ObjectField(_dependencyContent[i], dependencyAsset, typeof(Object), false);
                }
            }
        }
    }
}