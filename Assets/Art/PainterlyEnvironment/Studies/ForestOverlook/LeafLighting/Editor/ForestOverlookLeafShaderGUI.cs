using UnityEditor;
using UnityEngine;

namespace RaggedBanner.Editor
{
    public sealed class ForestOverlookLeafShaderGUI : ShaderGUI
    {
        static readonly GUIContent _baseColorLabel = new GUIContent("잎 바탕색");

        bool _showAdvanced;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (materialEditor.targets.Length != 1 || !(materialEditor.target is Material material)
                || !AssetDatabase.IsMainAsset(material))
            {
                base.OnGUI(materialEditor, properties);
                return;
            }

            bool palette = material.IsKeywordEnabled("_SCENE_PALETTE");
            bool flat = material.IsKeywordEnabled("_FLAT_FOLIAGE_LIGHTING");
            bool oliveFog = material.IsKeywordEnabled("_OLIVE_VISTA_FOG");
            bool farBankFog = material.IsKeywordEnabled("_FAR_BANK_FOG");
            bool foreground = palette && !flat && !oliveFog && !farBankFog;
            bool vista = !palette && flat && oliveFog;
            if (!foreground && !vista)
            {
                base.OnGUI(materialEditor, properties);
                return;
            }

            DrawPigments(materialEditor, material, properties, foreground);
            EditorGUILayout.LabelField("명암", flat ? "단일 톤" : "3단 명암");
            if (oliveFog)
            {
                string fog = farBankFog
                    ? "거리에 따라 푸른 안개로 전환"
                    : "강변 올리브색 안개";
                EditorGUILayout.LabelField("안개", fog);
            }

            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 설정", true);
            if (_showAdvanced)
            {
                base.OnGUI(materialEditor, properties);
            }
        }

        static void DrawPigments(MaterialEditor editor, Material material, MaterialProperty[] properties, bool foreground)
        {
            bool noise = material.GetFloat("_Use_Color_Noise") != 0f;
            if (!noise && material.GetFloat("_Leaf_Flat_Color") == 0f)
            {
                EditorGUILayout.LabelField("잎 바탕색", "텍스처 색상 사용");
                return;
            }

            var baseColor = FindProperty("_Leaf_Base_Color", properties);
            if (!noise)
            {
                editor.ShaderProperty(baseColor, _baseColorLabel.text);
                return;
            }

            var smallColor = FindProperty("_Leaf_Noise_Color", properties);
            var largeColor = FindProperty("_Leaf_Noise_Large_Color", properties);
            if (foreground && !material.isVariant
                && !material.IsPropertyLocked(baseColor.name) && !material.IsPropertyLocked(smallColor.name)
                && !material.IsPropertyLocked(largeColor.name) && SameRgb(baseColor.colorValue, smallColor.colorValue)
                && SameRgb(baseColor.colorValue, largeColor.colorValue))
            {
                Color current = baseColor.colorValue;
                current.a = 1f;
                EditorGUI.BeginChangeCheck();
                Color next = EditorGUI.ColorField(EditorGUILayout.GetControlRect(), _baseColorLabel, current, true, false, false);
                if (EditorGUI.EndChangeCheck())
                {
                    editor.RegisterPropertyChangeUndo(_baseColorLabel.text);
                    ApplyPigmentRgb(baseColor, smallColor, largeColor, next);
                }
                return;
            }

            editor.ShaderProperty(baseColor, _baseColorLabel.text);
            editor.ShaderProperty(smallColor, "작은 색면");
            editor.ShaderProperty(largeColor, "큰 색면");
        }

        static bool SameRgb(Color first, Color second)
        {
            return first.r == second.r && first.g == second.g && first.b == second.b;
        }

        static void ApplyPigmentRgb(MaterialProperty baseColor, MaterialProperty smallColor, MaterialProperty largeColor, Color rgb)
        {
            rgb.a = baseColor.colorValue.a;
            baseColor.colorValue = rgb;
            rgb.a = smallColor.colorValue.a;
            smallColor.colorValue = rgb;
            rgb.a = largeColor.colorValue.a;
            largeColor.colorValue = rgb;
        }
    }
}
