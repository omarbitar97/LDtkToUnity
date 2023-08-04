using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LDtkUnity.Editor
{
    /// <summary>
    /// This importer is for generating everything that's related to a tileset definition.
    /// This is generated by the project importer.
    /// This has no dependency back to the project importer, only the texture it references.
    /// </summary>
    [HelpURL(LDtkHelpURL.IMPORTER_LDTK_TILESET)]
    [ScriptedImporter(LDtkImporterConsts.TILESET_VERSION, LDtkImporterConsts.TILESET_EXT, LDtkImporterConsts.TILESET_ORDER)]
    internal sealed partial class LDtkTilesetImporter : LDtkJsonImporter<LDtkTilesetFile>
    {
        //public FilterMode _filterMode = FilterMode.Point;
        
        /// <summary>
        /// Holds onto all the standard grid-sized tiles. This serializes the sprite's changed settings between reimports, like pivot or physics shape.
        /// </summary>
        public List<LDtkSpriteRect> _sprites = new List<LDtkSpriteRect>();
        /// <summary>
        /// Any sprites that were defined from entity/level fields.
        /// It's separate because we don't want to draw them in the sprite editor window, or otherwise make them configurable.
        /// Also because they won't have tilemap assets generated for them anyways, as their size wouldn't fit in the tilemap.
        /// </summary>
        [SerializeField] internal List<LDtkSpriteRect> _additionalTiles = new List<LDtkSpriteRect>();
        [SerializeField] internal SecondarySpriteTexture[] _secondaryTextures;
    
        private Texture2D _cachedTex;
        private LDtkArtifactAssetsTileset _cachedArtifacts;

        /// <summary>
        /// filled by deserializing
        /// </summary>
        private LDtkTilesetDefinition _definition;
        private int _pixelsPerUnit = 16;
        private TilesetDefinition _json;
        
        private TextureImporter _srcTextureImporter;
        private LDtkTilesetFile _tilesetFile;
        private string _texturePath;
        
        
        public static string[] _previousDependencies;
        protected override string[] GetGatheredDependencies() => _previousDependencies;
        private static string[] GatherDependenciesFromSourceFile(string path)
        {
            Debug.Log("tileset GatherDependenciesFromSourceFile");

            //this depends on the texture
            LDtkProfiler.BeginSample($"GatherDependenciesFromSourceFile/{Path.GetFileName(path)}");
            string texPath = PathToTexture(path);
            _previousDependencies = string.IsNullOrEmpty(texPath) ? Array.Empty<string>() : new []{texPath};
            LDtkProfiler.EndSample();
            
            return _previousDependencies;
        }

        protected override void Import()
        {
            Profiler.BeginSample("DeserializeAndAssign");
            if (!DeserializeAndAssign())
            {
                Profiler.EndSample();
                return;
            }
            Profiler.EndSample();

            Profiler.BeginSample("GetTextureImporterPlatformSettings");
            TextureImporterPlatformSettings platformSettings = GetTextureImporterPlatformSettings();
            Profiler.EndSample();
            
            Profiler.BeginSample("CorrectTheTexture");
            if (CorrectTheTexture(_srcTextureImporter, platformSettings))
            {
                //return because of texture importer corrections. we're going to import a 2nd time
                Profiler.EndSample();
                return;
            }
            Profiler.EndSample();

            Profiler.BeginSample("GetStandardSpriteRectsForDefinition");
            var rects = ReadSourceRectsFromJsonDefinition(_definition.Def);
            Profiler.EndSample();
            
            Profiler.BeginSample("ReformatRectMetaData");
            ReformatRectMetaData(rects);
            Profiler.EndSample();

            Profiler.BeginSample("ReformatAdditionalTiles");
            ReformatAdditionalTiles();
            Profiler.EndSample();
            
            Profiler.BeginSample("PrepareGenerate");
            TextureGenerationOutput output = PrepareGenerate(platformSettings);
            Profiler.EndSample();

            Texture outputTexture = output.output;
            if (output.sprites.IsNullOrEmpty() && outputTexture == null)
            {
                ImportContext.LogImportWarning("No Sprites or Texture are generated. Possibly because all assets in file are hidden or failed to generate texture.", this);
                return;
            }
            if (!string.IsNullOrEmpty(output.importInspectorWarnings))
            {
                ImportContext.LogImportWarning(output.importInspectorWarnings);
            }
            if (output.importWarnings != null)
            {
                foreach (var warning in output.importWarnings)
                {
                    ImportContext.LogImportWarning(warning);
                }
            }
            if (output.thumbNail == null)
            {
                ImportContext.LogImportWarning("Thumbnail generation fail");
            }
            
            outputTexture.name = AssetName;
            
            Profiler.BeginSample("MakeAndCacheArtifacts");
            LDtkArtifactAssetsTileset artifacts = MakeAndCacheArtifacts(output);
            Profiler.EndSample();

            ImportContext.AddObjectToAsset("artifactCache", artifacts, (Texture2D)LDtkIconUtility.GetUnityIcon("Tilemap"));
            ImportContext.AddObjectToAsset("texture", outputTexture, LDtkIconUtility.LoadTilesetFileIcon());
            ImportContext.AddObjectToAsset("tilesetFile", _tilesetFile, LDtkIconUtility.LoadTilesetIcon());
            
            ImportContext.SetMainObject(outputTexture);
        }

        private LDtkArtifactAssetsTileset MakeAndCacheArtifacts(TextureGenerationOutput output)
        {
            LDtkArtifactAssetsTileset artifacts = ScriptableObject.CreateInstance<LDtkArtifactAssetsTileset>();
            artifacts.name = $"{_definition.Def.Identifier}_Artifacts";
            
            artifacts._sprites = new List<Sprite>(_sprites.Count);
            artifacts._tiles = new List<LDtkTilesetTile>(_sprites.Count);
            artifacts._additionalSprites = new List<Sprite>(_additionalTiles.Count);

            var customData = _definition.Def.CustomDataToDictionary();
            var enumTags = _definition.Def.EnumTagsToDictionary();

            for (int i = 0; i < output.sprites.Length; i++)
            {
                Sprite spr = output.sprites[i];
                ImportContext.AddObjectToAsset(spr.name, spr);

                //any indexes past the sprite count is additional sprites. dont make tile, just sprite.
                if (i >= _sprites.Count)
                {
                    artifacts._additionalSprites.Add(spr);
                    continue;
                }

                //Debug.Log(spr.name);
                AddOffsetToPhysicsShape(spr);

                LDtkTilesetTile newTilesetTile = ScriptableObject.CreateInstance<LDtkTilesetTile>();
                newTilesetTile.name = spr.name;
                newTilesetTile._sprite = spr;
                newTilesetTile._type = GetColliderTypeForSprite(spr);
                newTilesetTile.hideFlags = HideFlags.None;

                if (customData.TryGetValue(i, out string cd))
                {
                    newTilesetTile._customData = cd;
                }

                if (enumTags.TryGetValue(i, out List<string> et))
                {
                    newTilesetTile._enumTagValues = et;
                }
                
                ImportContext.AddObjectToAsset(newTilesetTile.name, newTilesetTile);
                artifacts._sprites.Add(spr);
                artifacts._tiles.Add(newTilesetTile);
            }

            return artifacts;
        }
        
        Tile.ColliderType GetColliderTypeForSprite(Sprite spr)
        {
            int shapeCount = spr.GetPhysicsShapeCount();
            if (shapeCount == 0)
            {
                return Tile.ColliderType.None;
            }
            if (shapeCount == 1)
            {
                List<Vector2> list = new List<Vector2>();
                spr.GetPhysicsShape(0, list);
                if (IsShapeSetForGrid(list))
                {
                    return Tile.ColliderType.Grid;
                }
            }
            return Tile.ColliderType.Sprite;
        }
        private static Vector2 GridCheck1 = new Vector2(-0.5f, -0.5f);
        private static Vector2 GridCheck2 = new Vector2(-0.5f, 0.5f);
        private static Vector2 GridCheck3 = new Vector2(0.5f, 0.5f);
        private static Vector2 GridCheck4 = new Vector2(0.5f, -0.5f);
        public static bool IsShapeSetForGrid(List<Vector2> shape)
        {
            return shape.Count == 4 &&
                   shape.Any(p => p == GridCheck1) &&
                   shape.Any(p => p == GridCheck2) &&
                   shape.Any(p => p == GridCheck3) &&
                   shape.Any(p => p == GridCheck4);
        }

        private void ReformatAdditionalTiles()
        {
            var srcRects = _definition.Rects;
            
            //if no tiles were populated (can be null)
            if (srcRects.IsNullOrEmpty())
            {
                _additionalTiles.Clear();
                EditorUtility.SetDirty(this);
                return;
            }

            if (_additionalTiles.Count > srcRects.Count)
            {
                _additionalTiles.RemoveRange(srcRects.Count, _additionalTiles.Count - srcRects.Count);
                EditorUtility.SetDirty(this);
            }
            
            if (_additionalTiles.Count < srcRects.Count)
            {
                for (int i = _additionalTiles.Count; i < srcRects.Count; i++)
                {
                    var rect = _definition.Rects[i].ToRect();
                    rect = LDtkCoordConverter.ImageSlice(rect, _definition.Def.PxHei);
                    LDtkSpriteRect newRect = new LDtkSpriteRect
                    {
                        border = Vector4.zero,
                        pivot = new Vector2(0.5f, 0.5f),
                        alignment = SpriteAlignment.Center,
                        rect = rect,
                        spriteID = GUID.Generate(),
                        name = MakeAssetName()
                    };
                    _additionalTiles.Add(newRect);
                    
                    string MakeAssetName()
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(_definition.Def.Identifier);
                        sb.Append('_');
                        sb.Append(rect.x);
                        sb.Append('_');
                        sb.Append(rect.y);
                        sb.Append('_');
                        sb.Append(rect.width);
                        sb.Append('_');
                        sb.Append(rect.height);
                        return sb.ToString();
                    }
                }
                EditorUtility.SetDirty(this);
            }
            
            Debug.Assert(_additionalTiles.Count == srcRects.Count);
        }

        private static void RefreshSceneTilemapColliders()
        {
            //refresh tilemap colliders in the current scene.
            //tiles would normally not update in the scene view until entering play mode, or reloading the scene, or resetting the component. this will immediately update it. 
            //todo this doesn't feel right and is not performant at all, but it works! Change later with a better solution
            //todo at the least, cache if we're doing this delay call so it's not being run an extra time for every reimported tileset definition
            //disabling, it's super super slow. I'd rather it just doesn't update
            EditorApplication.delayCall += () =>
            {
                TilemapCollider2D[] colliders = Object.FindObjectsOfType<TilemapCollider2D>();
                foreach (TilemapCollider2D collider in colliders)
                {
                    Unsupported.SmartReset(collider);
                    PrefabUtility.RevertObjectOverride(collider, InteractionMode.AutomatedAction);
                }
            };
        }
        

        private TextureGenerationOutput PrepareGenerate(TextureImporterPlatformSettings platformSettings)
        {
            Debug.Assert(_pixelsPerUnit > 0, $"_pixelsPerUnit was {_pixelsPerUnit}");
            
            TextureImporterSettings importerSettings = new TextureImporterSettings();
            _srcTextureImporter.ReadTextureSettings(importerSettings);
            importerSettings.spritePixelsPerUnit = _pixelsPerUnit;
            importerSettings.filterMode = FilterMode.Point;

            NativeArray<Color32> rawData = LoadTex().GetRawTextureData<Color32>();

            return TextureGeneration.Generate(
                ImportContext, rawData, _json.PxWid, _json.PxHei, _sprites.Concat(_additionalTiles).ToArray(),
                platformSettings, importerSettings, string.Empty, _secondaryTextures);
        }

        private TextureImporterPlatformSettings GetTextureImporterPlatformSettings()
        {
            string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
            TextureImporterPlatformSettings platformSettings = _srcTextureImporter.GetPlatformTextureSettings(platform);
            return platformSettings.overridden ? platformSettings : _srcTextureImporter.GetDefaultPlatformTextureSettings();
        }

        private bool DeserializeAndAssign()
        {
            //deserialize first. required for the path to the texture importer 
            try
            {
                _definition = FromJson<LDtkTilesetDefinition>();
                _json = _definition.Def;
                _pixelsPerUnit = _definition.Ppu;
            }
            catch (Exception e)
            {
                ImportContext.LogImportError(e.ToString());
                return false;
            }
            
            Profiler.BeginSample("GetTextureImporter");
            _srcTextureImporter = (TextureImporter)GetAtPath(PathToTexture(assetPath));
            Profiler.EndSample();
            
            if (_srcTextureImporter == null)
            {
                ImportContext.LogImportError($"Tried to build tileset {AssetName}, but the texture importer was not found. Is this tileset asset in a folder relative to the LDtk project file? Ensure that it's relativity is maintained if the project was moved also.");
                return false;
            }

            Profiler.BeginSample("AddTilesetSubAsset");
            _tilesetFile = ReadAssetText();
            Profiler.EndSample();
            
            if (_tilesetFile == null)
            {
                ImportContext.LogImportError("Tried to build tileset, but the tileset json ScriptableObject was null");
                return false;
            }
            
            return true;
        }
        
        /*private void AddGeneratedAssets(AssetImportContext ctx, TextureGenerationOutput output)
        {
            

            var assetName = assetNameGenerator.GetUniqueName(System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath),  true, this);
            UnityEngine.Object mainAsset = null;

            RegisterTextureAsset(ctx, output, assetName, ref mainAsset);
            /*RegisterGameObjects(ctx, output, ref mainAsset);
            RegisterAnimationClip(ctx, assetName, output);
            RegisterAnimatorController(ctx, assetName);#1#

            ctx.AddObjectToAsset("AsepriteImportData", _tex);
            ctx.SetMainObject(mainAsset);
        }*/

        /*private void RegisterTextureAsset(AssetImportContext ctx, TextureGenerationOutput output, string assetName, ref UnityEngine.Object mainAsset)
        {
            var registerTextureNameId = string.IsNullOrEmpty(_tex.name) ? "Texture" : _tex.name;

            output.texture.name = assetName;
            ctx.AddObjectToAsset(registerTextureNameId, output.texture, output.thumbNail);
            mainAsset = output.texture;
        }*/

        /// <summary>
        /// Only use when needed, it performs a deserialize. look at optimizing if it's expensive
        /// </summary>
        private static string PathToTexture(string assetPath)
        {
            TilesetDefinition def = FromJson<LDtkTilesetDefinition>(assetPath).Def;
            if (def.IsEmbedAtlas)
            {
                string iconsPath = LDtkProjectSettings.InternalIconsTexturePath;
                return iconsPath.IsNullOrEmpty() ? string.Empty : iconsPath;
            }

            LDtkRelativeGetterTilesetTexture getter = new LDtkRelativeGetterTilesetTexture();
            string pathFrom = Path.Combine(assetPath, "..");
            pathFrom = LDtkPathUtility.CleanPath(pathFrom);
            string path = getter.GetPath(def, pathFrom);
            //Debug.Log($"relative from {pathFrom}. path of texture importer was {path}");
            return !File.Exists(path) ? string.Empty : path;
        }

        private void AddOffsetToPhysicsShape(Sprite spr)
        {
            List<Vector2[]> srcShapes = GetSpriteData(spr.name).GetOutlines();
            List<Vector2[]> newShapes = new List<Vector2[]>();
            foreach (Vector2[] srcOutline in srcShapes)
            {
                Vector2[] newOutline = new Vector2[srcOutline.Length];
                for (int ii = 0; ii < srcOutline.Length; ii++)
                {
                    Vector2 point = srcOutline[ii];
                    point += spr.rect.size * 0.5f;
                    newOutline[ii] = point;
                }
                newShapes.Add(newOutline);
            }
            spr.OverridePhysicsShape(newShapes);
        }

        private void ForceUpdateSpriteDataName(SpriteRect spr)
        {
            spr.name = $"{AssetName}_{spr.rect.x}_{spr.rect.y}_{spr.rect.width}_{spr.rect.height}";
        }

        private bool CorrectTheTexture(TextureImporter textureImporter, TextureImporterPlatformSettings platformSettings)
        {
            bool issue = false;

            if (platformSettings.maxTextureSize < _json.PxWid || platformSettings.maxTextureSize < _json.PxHei)
            {
                issue = true;
                platformSettings.maxTextureSize = 8192;
                Debug.Log($"The texture {textureImporter.assetPath} maxTextureSize was greater than it's resolution. This was automatically fixed.");
            }

            if (platformSettings.format != TextureImporterFormat.RGBA32)
            {
                issue = true;
                platformSettings.format = TextureImporterFormat.RGBA32;
                Debug.Log($"The texture {textureImporter.assetPath} format was not {TextureImporterFormat.RGBA32}. This was automatically fixed.");
            }

            if (!textureImporter.isReadable)
            {
                issue = true;
                textureImporter.isReadable = true;
                Debug.Log($"The texture {textureImporter.assetPath} was not readable. This was automatically fixed.");
            }

            if (!issue)
            {
                return false;
            }
        
            _cachedTex = null;
            textureImporter.SetPlatformTextureSettings(platformSettings);
            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private Texture2D LoadTex(bool forceLoad = false)
        {
            //in case the importer was destroyed via file delete
            if (this == null)
            {
                return null;
            }
            
            if (_cachedTex == null || forceLoad)
            {
                _cachedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PathToTexture(assetPath));
            }
            return _cachedTex;
        }
        
        private LDtkSpriteRect GetSpriteData(GUID guid)
        {
            LDtkSpriteRect data = _sprites.FirstOrDefault(x => x.spriteID == guid);
            Debug.Assert(data != null, $"Sprite data not found for GUID: {guid.ToString()}");
            return data;
        }

        private LDtkSpriteRect GetSpriteData(string spriteName)
        {
            LDtkSpriteRect data = _sprites.FirstOrDefault(x => x.name == spriteName);
            Debug.Assert(data != null, $"Sprite data not found for name: {spriteName}");
            return data;
        }
        
        public LDtkArtifactAssetsTileset LoadArtifacts()
        {
            if (!_cachedArtifacts)
            {
                _cachedArtifacts = AssetDatabase.LoadAssetAtPath<LDtkArtifactAssetsTileset>(assetPath);
            }
            //It's possible that the artifact assets don't exist, either because the texture importer failed to import, or the artifact assets weren't produced due to being an aseprite file or otherwise
            Debug.Assert(_cachedArtifacts, $"Cached artifacts didnt load! For \"{assetPath}\"");
            return _cachedArtifacts;
        }
        
    }
}