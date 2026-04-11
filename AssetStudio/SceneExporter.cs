using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public static class SceneExporter
    {
        /// <summary>
        /// Exports a SerializedFile as a Unity .scene (.unity) YAML file.
        /// Each object in the file becomes a YAML document with the format:
        /// --- !u!{classID} &amp;{pathID}
        /// ClassName:
        ///   property: value
        ///   ...
        /// </summary>
        public static string ExportScene(SerializedFile assetsFile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("%YAML 1.1");
            sb.AppendLine("%TAG !u! tag:unity3d.com,2011:");

            var objects = assetsFile.Objects;
            int exported = 0;
            int skipped = 0;

            foreach (var obj in objects)
            {
                var classID = (int)obj.type;
                var pathID = obj.m_PathID;
                var className = GetClassName(obj.type);

                // Skip types that are not part of scene serialization
                if (ShouldSkipType(obj.type))
                {
                    skipped++;
                    continue;
                }

                OrderedDictionary properties = null;
                try
                {
                    properties = obj.ToType();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to read type tree for {className} (PathID: {pathID}): {ex.Message}");
                }

                if (properties == null || properties.Count == 0)
                {
                    skipped++;
                    continue;
                }

                sb.AppendLine($"--- !u!{classID} &{pathID}");
                sb.AppendLine($"{className}:");
                WriteOrderedDictionary(sb, properties, 1);
                exported++;
            }

            Logger.Info($"Scene export: {exported} objects exported, {skipped} skipped");
            return sb.ToString();
        }

        /// <summary>
        /// Exports all loaded SerializedFiles that contain scene-like data to individual .unity files.
        /// </summary>
        public static int ExportScenes(AssetsManager assetsManager, string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            int count = 0;

            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                if (!HasSceneObjects(assetsFile))
                    continue;

                var sceneName = GetSceneName(assetsFile);
                var outputPath = Path.Combine(outputDir, sceneName + ".unity");

                // Avoid overwriting
                if (File.Exists(outputPath))
                {
                    for (int i = 1; i < int.MaxValue; i++)
                    {
                        outputPath = Path.Combine(outputDir, $"{sceneName} ({i}).unity");
                        if (!File.Exists(outputPath))
                            break;
                    }
                }

                try
                {
                    var yaml = ExportScene(assetsFile);
                    File.WriteAllText(outputPath, yaml);
                    count++;
                    Logger.Info($"Exported scene: {outputPath}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to export scene {sceneName}: {ex.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if a SerializedFile contains scene-like objects (GameObjects, Transforms, etc.)
        /// </summary>
        public static bool HasSceneObjects(SerializedFile assetsFile)
        {
            return assetsFile.Objects.Any(obj =>
                obj.type == ClassIDType.GameObject ||
                obj.type == ClassIDType.Transform ||
                obj.type == ClassIDType.RectTransform ||
                obj.type == ClassIDType.OcclusionCullingSettings ||
                obj.type == ClassIDType.RenderSettings ||
                obj.type == ClassIDType.LightmapSettings);
        }

        private static string GetSceneName(SerializedFile assetsFile)
        {
            // Try to derive a meaningful name from the file path
            if (!string.IsNullOrEmpty(assetsFile.originalPath))
            {
                var name = Path.GetFileNameWithoutExtension(assetsFile.originalPath);
                if (!string.IsNullOrEmpty(name))
                    return SanitizeFileName(name);
            }

            if (!string.IsNullOrEmpty(assetsFile.fileName))
            {
                var name = Path.GetFileNameWithoutExtension(assetsFile.fileName);
                if (!string.IsNullOrEmpty(name))
                    return SanitizeFileName(name);
            }

            return "scene";
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static bool ShouldSkipType(ClassIDType type)
        {
            // Skip bundle/resource manager metadata types that aren't part of scene data
            switch (type)
            {
                case ClassIDType.AssetBundle:
                case ClassIDType.ResourceManager:
                case ClassIDType.PreloadData:
                    return true;
                default:
                    return false;
            }
        }

        private static string GetClassName(ClassIDType type)
        {
            // Map to Unity's class names used in YAML
            switch (type)
            {
                case ClassIDType.GameObject: return "GameObject";
                case ClassIDType.Transform: return "Transform";
                case ClassIDType.RectTransform: return "RectTransform";
                case ClassIDType.Camera: return "Camera";
                case ClassIDType.Light: return "Light";
                case ClassIDType.MeshFilter: return "MeshFilter";
                case ClassIDType.MeshRenderer: return "MeshRenderer";
                case ClassIDType.SkinnedMeshRenderer: return "SkinnedMeshRenderer";
                case ClassIDType.Material: return "Material";
                case ClassIDType.Texture2D: return "Texture2D";
                case ClassIDType.BoxCollider: return "BoxCollider";
                case ClassIDType.SphereCollider: return "SphereCollider";
                case ClassIDType.CapsuleCollider: return "CapsuleCollider";
                case ClassIDType.MeshCollider: return "MeshCollider";
                case ClassIDType.Rigidbody: return "Rigidbody";
                case ClassIDType.MonoBehaviour: return "MonoBehaviour";
                case ClassIDType.MonoScript: return "MonoScript";
                case ClassIDType.Animator: return "Animator";
                case ClassIDType.Animation: return "Animation";
                case ClassIDType.AnimationClip: return "AnimationClip";
                case ClassIDType.AudioSource: return "AudioSource";
                case ClassIDType.AudioClip: return "AudioClip";
                case ClassIDType.AudioListener: return "AudioListener";
                case ClassIDType.RenderSettings: return "RenderSettings";
                case ClassIDType.LightmapSettings: return "LightmapSettings";
                case ClassIDType.OcclusionCullingSettings: return "OcclusionCullingSettings";
                case ClassIDType.NavMeshSettings: return "NavMeshSettings";
                case ClassIDType.ParticleSystem: return "ParticleSystem";
                case ClassIDType.ParticleSystemRenderer: return "ParticleSystemRenderer";
                case ClassIDType.Canvas: return "Canvas";
                case ClassIDType.CanvasRenderer: return "CanvasRenderer";
                case ClassIDType.Sprite: return "Sprite";
                case ClassIDType.SpriteRenderer: return "SpriteRenderer";
                case ClassIDType.Terrain: return "Terrain";
                case ClassIDType.LODGroup: return "LODGroup";
                case ClassIDType.Mesh: return "Mesh";
                case ClassIDType.Shader: return "Shader";
                case ClassIDType.TextAsset: return "TextAsset";
                case ClassIDType.Font: return "Font";
                case ClassIDType.PhysicMaterial: return "PhysicMaterial";
                case ClassIDType.Cubemap: return "Cubemap";
                case ClassIDType.Avatar: return "Avatar";
                case ClassIDType.AnimatorController: return "AnimatorController";
                case ClassIDType.AnimatorOverrideController: return "AnimatorOverrideController";
                case ClassIDType.OcclusionArea: return "OcclusionArea";
                case ClassIDType.OcclusionPortal: return "OcclusionPortal";
                case ClassIDType.ReflectionProbe: return "ReflectionProbe";
                case ClassIDType.LightProbeGroup: return "LightProbeGroup";
                case ClassIDType.LightProbes: return "LightProbes";
                default: return type.ToString();
            }
        }

        #region YAML Writer

        private static void WriteOrderedDictionary(StringBuilder sb, OrderedDictionary dict, int indent)
        {
            var prefix = new string(' ', indent * 2);
            foreach (string key in dict.Keys)
            {
                var value = dict[key];
                WriteKeyValue(sb, key, value, indent);
            }
        }

        private static void WriteKeyValue(StringBuilder sb, string key, object value, int indent)
        {
            var prefix = new string(' ', indent * 2);

            if (value == null)
            {
                sb.AppendLine($"{prefix}{key}: ");
                return;
            }

            switch (value)
            {
                case OrderedDictionary dict:
                    if (IsPPtr(dict))
                    {
                        sb.AppendLine($"{prefix}{key}: {FormatPPtr(dict)}");
                    }
                    else if (IsInlineMapping(dict))
                    {
                        sb.AppendLine($"{prefix}{key}: {FormatInlineMapping(dict)}");
                    }
                    else
                    {
                        sb.AppendLine($"{prefix}{key}:");
                        WriteOrderedDictionary(sb, dict, indent + 1);
                    }
                    break;

                case List<object> list:
                    WriteList(sb, key, list, indent);
                    break;

                case List<KeyValuePair<object, object>> map:
                    WriteMap(sb, key, map, indent);
                    break;

                case byte[] bytes:
                    WriteTypelessData(sb, key, bytes, indent);
                    break;

                case bool b:
                    sb.AppendLine($"{prefix}{key}: {(b ? 1 : 0)}");
                    break;

                case float f:
                    sb.AppendLine($"{prefix}{key}: {FormatFloat(f)}");
                    break;

                case double d:
                    sb.AppendLine($"{prefix}{key}: {FormatDouble(d)}");
                    break;

                case string s:
                    sb.AppendLine($"{prefix}{key}: {FormatString(s)}");
                    break;

                default:
                    sb.AppendLine($"{prefix}{key}: {value}");
                    break;
            }
        }

        private static void WriteList(StringBuilder sb, string key, List<object> list, int indent)
        {
            var prefix = new string(' ', indent * 2);

            if (list.Count == 0)
            {
                sb.AppendLine($"{prefix}{key}: []");
                return;
            }

            // Check if all elements are simple (scalar) values
            if (list.All(item => IsScalar(item)))
            {
                sb.AppendLine($"{prefix}{key}:");
                foreach (var item in list)
                {
                    sb.AppendLine($"{prefix}- {FormatScalar(item)}");
                }
                return;
            }

            sb.AppendLine($"{prefix}{key}:");
            foreach (var item in list)
            {
                if (item is OrderedDictionary dict)
                {
                    if (IsPPtr(dict))
                    {
                        sb.AppendLine($"{prefix}- {FormatPPtr(dict)}");
                    }
                    else if (IsInlineMapping(dict))
                    {
                        sb.AppendLine($"{prefix}- {FormatInlineMapping(dict)}");
                    }
                    else
                    {
                        // For complex objects in lists, first key goes on the - line
                        bool first = true;
                        foreach (string k in dict.Keys)
                        {
                            if (first)
                            {
                                var val = dict[k];
                                if (val is OrderedDictionary innerDict && IsPPtr(innerDict))
                                {
                                    sb.AppendLine($"{prefix}- {k}: {FormatPPtr(innerDict)}");
                                }
                                else if (IsScalar(val))
                                {
                                    sb.AppendLine($"{prefix}- {k}: {FormatScalar(val)}");
                                }
                                else
                                {
                                    sb.AppendLine($"{prefix}- {k}:");
                                    WriteValue(sb, val, indent + 2);
                                }
                                first = false;
                            }
                            else
                            {
                                WriteKeyValue(sb, k, dict[k], indent + 1);
                            }
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"{prefix}- {FormatScalar(item)}");
                }
            }
        }

        private static void WriteMap(StringBuilder sb, string key, List<KeyValuePair<object, object>> map, int indent)
        {
            var prefix = new string(' ', indent * 2);

            if (map.Count == 0)
            {
                sb.AppendLine($"{prefix}{key}: {{}}");
                return;
            }

            sb.AppendLine($"{prefix}{key}:");
            foreach (var kvp in map)
            {
                var mapKey = FormatScalar(kvp.Key);
                var mapValue = kvp.Value;

                if (mapValue is OrderedDictionary dict)
                {
                    if (IsPPtr(dict))
                    {
                        sb.AppendLine($"{prefix}  {mapKey}: {FormatPPtr(dict)}");
                    }
                    else
                    {
                        sb.AppendLine($"{prefix}  {mapKey}:");
                        WriteOrderedDictionary(sb, dict, indent + 2);
                    }
                }
                else
                {
                    sb.AppendLine($"{prefix}  {mapKey}: {FormatScalar(mapValue)}");
                }
            }
        }

        private static void WriteTypelessData(StringBuilder sb, string key, byte[] data, int indent)
        {
            var prefix = new string(' ', indent * 2);
            if (data.Length == 0)
            {
                sb.AppendLine($"{prefix}{key}: ");
                return;
            }
            // Unity stores TypelessData as a hex string
            var hex = BitConverter.ToString(data).Replace("-", "");
            sb.AppendLine($"{prefix}{key}: {hex}");
        }

        private static void WriteValue(StringBuilder sb, object value, int indent)
        {
            if (value is OrderedDictionary dict)
            {
                WriteOrderedDictionary(sb, dict, indent);
            }
            else if (value is List<object> list)
            {
                var prefix = new string(' ', indent * 2);
                foreach (var item in list)
                {
                    if (item is OrderedDictionary itemDict)
                    {
                        if (IsPPtr(itemDict))
                        {
                            sb.AppendLine($"{prefix}- {FormatPPtr(itemDict)}");
                        }
                        else
                        {
                            sb.AppendLine($"{prefix}-");
                            WriteOrderedDictionary(sb, itemDict, indent + 1);
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{prefix}- {FormatScalar(item)}");
                    }
                }
            }
            else
            {
                var prefix = new string(' ', indent * 2);
                sb.AppendLine($"{prefix}{FormatScalar(value)}");
            }
        }

        #endregion

        #region Format Helpers

        private static bool IsPPtr(OrderedDictionary dict)
        {
            return dict.Count == 2 && dict.Contains("m_FileID") && dict.Contains("m_PathID");
        }

        private static string FormatPPtr(OrderedDictionary dict)
        {
            var fileID = dict["m_FileID"];
            var pathID = dict["m_PathID"];
            return $"{{fileID: {fileID}, guid: , type: 0}}";
        }

        private static bool IsInlineMapping(OrderedDictionary dict)
        {
            // Small mappings with only scalar values can be written inline
            if (dict.Count > 6) return false;
            foreach (var key in dict.Keys)
            {
                var val = dict[key];
                if (!IsScalar(val)) return false;
            }
            // Common inline types: vectors, colors, quaternions
            return dict.Contains("x") || dict.Contains("r");
        }

        private static string FormatInlineMapping(OrderedDictionary dict)
        {
            var parts = new List<string>();
            foreach (string key in dict.Keys)
            {
                parts.Add($"{key}: {FormatScalar(dict[key])}");
            }
            return "{" + string.Join(", ", parts) + "}";
        }

        private static bool IsScalar(object value)
        {
            return value is sbyte || value is byte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is bool || value is string ||
                   value is char || value == null;
        }

        private static string FormatScalar(object value)
        {
            if (value == null) return "";
            if (value is bool b) return b ? "1" : "0";
            if (value is float f) return FormatFloat(f);
            if (value is double d) return FormatDouble(d);
            if (value is string s) return FormatString(s);
            return value.ToString();
        }

        private static string FormatFloat(float f)
        {
            if (float.IsInfinity(f) || float.IsNaN(f))
                return "0";
            // Unity uses up to 7 significant digits for floats
            var str = f.ToString("G9", CultureInfo.InvariantCulture);
            // Ensure there's a decimal point for float values
            if (!str.Contains('.') && !str.Contains('E'))
                str += ".0";
            return str;
        }

        private static string FormatDouble(double d)
        {
            if (double.IsInfinity(d) || double.IsNaN(d))
                return "0";
            var str = d.ToString("G17", CultureInfo.InvariantCulture);
            if (!str.Contains('.') && !str.Contains('E'))
                str += ".0";
            return str;
        }

        private static string FormatString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            // Check if string needs quoting
            if (s.Contains('\n') || s.Contains('\r') || s.Contains(':') || s.Contains('#') ||
                s.Contains('{') || s.Contains('}') || s.Contains('[') || s.Contains(']') ||
                s.Contains(',') || s.Contains('&') || s.Contains('*') || s.Contains('?') ||
                s.Contains('|') || s.Contains('>') || s.Contains('\'') || s.Contains('"') ||
                s.Contains('%') || s.Contains('@') || s.Contains('`') ||
                s.StartsWith(" ") || s.EndsWith(" ") ||
                s == "true" || s == "false" || s == "null" || s == "yes" || s == "no")
            {
                // Escape and quote
                var escaped = s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                               .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                return $"\"{escaped}\"";
            }
            return s;
        }

        #endregion
    }
}
