using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BossFight.Editor
{
    // Lookups, reference wiring, validation and material helpers shared by every builder step.
    internal static class BuilderUtil
    {
        public static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        // The player is whichever object has PlayerMovement.
        public static PlayerMovement FindPlayer(Scene scene)
        {
            return FindInScene<PlayerMovement>(scene);
        }

        public static Transform FindByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        public static bool SetReference(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                throw new System.InvalidOperationException($"{so.targetObject.GetType().Name} has no serialized field '{field}'");
            }
            if (prop.objectReferenceValue == value)
            {
                return false;
            }
            prop.objectReferenceValue = value;
            return true;
        }

        // Validator check: each named object-reference field must be assigned.
        public static void RequireFields(Component component, string label, string[] fields, List<string> errors)
        {
            var so = new SerializedObject(component);
            foreach (var field in fields)
            {
                var property = so.FindProperty(field);
                if (property == null || property.objectReferenceValue == null)
                {
                    errors.Add($"{label}: {field} reference is missing");
                }
            }
        }

        public static void RequireFields(Component component, string[] fields, List<string> errors)
        {
            RequireFields(component, $"Boss Scene {component.GetType().Name}", fields, errors);
        }

        // Created once; an existing asset at path is returned untouched.
        public static Material EnsureLitMaterial(string path, Color color, System.Action<Material> configure = null)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(RequireShader("Universal Render Pipeline/Lit", path));
            material.SetColor("_BaseColor", color);
            configure?.Invoke(material);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        public static Material EnsureTransparentMaterial(string path, string shaderName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            var material = new Material(RequireShader(shaderName, path));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static Shader RequireShader(string shaderName, string path)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.InvalidOperationException($"Shader '{shaderName}' not found for {path}");
            }
            return shader;
        }
    }
}
