using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

namespace TEngine
{
    [InitializeOnLoad]
    public class SceneSwitchLeftButton
    {
        private static GUIContent _switchSceneContent;
        private static List<string> _sceneAssetList = new();
        
        static SceneSwitchLeftButton()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
            EditorSceneManager.sceneOpened += OnSceneOpened;

            var curOpenSceneName = SceneManager.GetActiveScene().name;
            _switchSceneContent =
                new GUIContent(String.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, EditorGUIUtility.FindTexture("UnityLogo"), "Switch Scene");
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            _switchSceneContent.text = scene.name;
        }

        static readonly string ButtonStyleName = "Tab middle";
        static GUIStyle _buttonGuiStyle;
        static GUIStyle _dropdownGUIStyle;

        static void OnToolbarGUI()
        {
            _buttonGuiStyle ??= new GUIStyle(ButtonStyleName)
            {
                padding = new RectOffset(2, 8, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            _dropdownGUIStyle ??= new GUIStyle(EditorStyles.toolbarPopup)
            {
                padding = new RectOffset(2, 8, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
 
            GUILayout.FlexibleSpace();
            
            if (EditorGUILayout.DropdownButton(_switchSceneContent, FocusType.Passive, _dropdownGUIStyle, GUILayout.MaxWidth(150)))
            {
                DrawSwitchSceneDropdownMenus();
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button(
                    new GUIContent("Launcher", EditorGUIUtility.FindTexture("PlayButton"), $"Start Scene Launcher"),
                    _buttonGuiStyle))
            {
                SceneHelper.StartScene(Constant.LauncherScene);
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        
        static void DrawSwitchSceneDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu
            {
                allowDuplicateNames = true
            };
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { Constant.ScenePath });
            _sceneAssetList.Clear();
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                _sceneAssetList.Add(scenePath);
                string fileDir = System.IO.Path.GetDirectoryName(scenePath);
                bool isInRootDir = Utility.Path.GetRegularPath(Constant.ScenePath).TrimEnd('/') == Utility.Path.GetRegularPath(fileDir).TrimEnd('/');
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string displayName = sceneName;
                if (!isInRootDir)
                {
                    var sceneDir = System.IO.Path.GetRelativePath(Constant.ScenePath, fileDir);
                    displayName = $"{sceneDir}/{sceneName}";
                }

                popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SceneHelper.SwitchScene((int)menuIdx, _sceneAssetList); }, i);
            }
            popMenu.ShowAsContext();
        }
    }

    static class SceneHelper
    {
        static string _sceneToOpen;

        public static void StartScene(string sceneName)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            _sceneToOpen = sceneName;
            EditorApplication.update += OnUpdate;
        }


        public static void SwitchScene(int index, List<string> sceneAssetList)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("This cannot be used during play mode");
            }
            
            if (index >= 0 && index < sceneAssetList.Count)
            {
                var scenePath = sceneAssetList[index];
                var curScene = SceneManager.GetActiveScene();
                if (curScene.isDirty)
                {
                    if (EditorUtility.DisplayDialog("Warning", $"The current scene {curScene.name} has not been saved. Would you like to save it?", "Save", "Cancel"))
                    {
                        EditorSceneManager.SaveOpenScenes();
                    }
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        static void OnUpdate()
        {
            if (_sceneToOpen == null ||
                EditorApplication.isPlaying || EditorApplication.isPaused ||
                EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string[] guids = AssetDatabase.FindAssets("t:scene " + _sceneToOpen, null);
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Couldn't find scene file");
                }
                else
                {
                    string scenePath = null;
                    // 优先打开完全匹配_sceneToOpen的场景
                    for (var i = 0; i < guids.Length; i++)
                    {
                        scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                        if (scenePath.EndsWith("/" + _sceneToOpen + ".unity"))
                        {
                            break;
                        }
                    }

                    // 如果没有完全匹配的场景，默认显示找到的第一个场景
                    if (string.IsNullOrEmpty(scenePath))
                    {
                        scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    }

                    EditorSceneManager.OpenScene(scenePath);
                    EditorApplication.isPlaying = true;
                }
            }

            _sceneToOpen = null;
        }
    }
}