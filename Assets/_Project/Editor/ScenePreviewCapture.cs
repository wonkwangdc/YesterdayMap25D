#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayMap.Editor
{
    public static class ScenePreviewCapture
    {
        public static void CaptureShelterFromCommandLine()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Shelter.unity");
            Camera camera = Camera.main;
            const int width = 1280;
            const int height = 720;
            RenderTexture target = new(width, height, 24);
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "ShelterPreview.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
            Debug.Log("SHELTER_PREVIEW_CREATED " + path);
            EditorApplication.Exit(0);
        }
    }
}
#endif
