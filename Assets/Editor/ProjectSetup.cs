using System.IO;
using TrainingDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class ProjectSetup
{
    private const string Lobby = "Assets/Scenes/Lobby.unity";
    private const string Training = "Assets/Scenes/Training.unity";

    static ProjectSetup()
    {
        EditorApplication.delayCall += EnsureProjectScenes;
    }

    [MenuItem("Training Demo/Rebuild demo scenes")]
    public static void EnsureProjectScenes()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Directory.CreateDirectory("Assets/Scenes");
        if (!File.Exists(Lobby)) CreateScene(Lobby, true);
        if (!File.Exists(Training)) CreateScene(Training, false);
        EnsureUrpAsset();
        var scenes = new[]
        {
            new EditorBuildSettingsScene(Lobby, true),
            new EditorBuildSettingsScene(Training, true)
        };
        EditorBuildSettings.scenes = scenes;
        if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            EditorSceneManager.OpenScene(Lobby, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureUrpAsset()
    {
        const string settingsFolder = "Assets/Settings";
        const string pipelinePath = settingsFolder + "/TrainingUniversalRP.asset";
        // Avoid asking URP for a renderer while replacing an incomplete first-run asset.
        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        if (!AssetDatabase.IsValidFolder(settingsFolder)) AssetDatabase.CreateFolder("Assets", "Settings");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create();
            pipeline.name = "Training Universal RP";
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
        }
        var serializedPipeline = new SerializedObject(pipeline);
        var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
        var hasRenderer = rendererList != null && rendererList.arraySize > 0 &&
                          rendererList.GetArrayElementAtIndex(0).objectReferenceValue != null;
        if (!hasRenderer)
            pipeline.LoadBuiltinRendererData();
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        AssetDatabase.SaveAssets();
    }

    private static void CreateScene(string path, bool lobby)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject(lobby ? "Lobby Scene" : "Training Scene");
        var builder = root.AddComponent<DemoSceneBuilder>(); builder.isLobby = lobby;
        EditorSceneManager.SaveScene(scene, path);
    }
}
