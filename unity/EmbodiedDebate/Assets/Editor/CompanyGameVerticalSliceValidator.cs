using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    /// <summary>
    /// Repeatable editor validation for the AI company vertical slice.
    /// Use Build + Validate after importing or changing assets.
    /// </summary>
    public static class CompanyGameVerticalSliceValidator
    {
        private const string ScenePath = "Assets/Project/Scenes/AICompanyGame.unity";
        private const string RobotPrefabPath = "Assets/Project/Robots/Prefabs/FallbackRobot.prefab";
        private const string OfficeFloorPath = "Assets/Project/Models/GameAssets/office/Floor_01.glb";
        private const string CityTerrainPath = "Assets/Project/Models/GameAssets/city/Terrain01_Art.glb";
        private const string CratePath = "Assets/Project/Models/KayKit/Box_A.obj";

        [MenuItem("Argus/Build + Validate AI Company Vertical Slice")]
        public static void BuildAndValidate()
        {
            BuildGameScene.Build();
            Validate();
        }

        [MenuItem("Argus/Validate AI Company Vertical Slice")]
        public static void Validate()
        {
            var errors = new List<string>();
            ValidateAssets(errors);
            ValidateScene(errors);

            if (errors.Count == 0)
            {
                Debug.Log(
                    "AI Company vertical slice validation passed: 4 CEOs, 12 employees, " +
                    "bridge, HUD, camera, office/city/crate assets.");
                return;
            }

            foreach (var error in errors)
            {
                Debug.LogError($"AI Company validation: {error}");
            }
            Debug.LogError($"AI Company vertical slice validation failed with {errors.Count} issue(s).");
        }

        private static void ValidateAssets(ICollection<string> errors)
        {
            RequireAsset<GameObject>(RobotPrefabPath, "minibot prefab", errors);
            RequireAsset<GameObject>(OfficeFloorPath, "office floor asset", errors);
            RequireAsset<GameObject>(CityTerrainPath, "city terrain asset", errors);
            RequireAsset<GameObject>(CratePath, "task crate asset", errors);
        }

        private static void ValidateScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                errors.Add($"scene missing at {ScenePath}; run Argus/Build Game Scene");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var actors = CollectComponents<CompanyMiniBotActor>(scene);
                var playerCount = 0;
                var employeeCount = 0;
                var actorIds = new HashSet<string>();
                foreach (var actor in actors)
                {
                    if (!actorIds.Add(actor.ActorId))
                    {
                        errors.Add($"duplicate actor id: {actor.ActorId}");
                    }

                    if (actor.Role == CompanyActorRole.Player)
                    {
                        playerCount++;
                    }
                    else
                    {
                        employeeCount++;
                    }
                }

                if (playerCount != 4)
                {
                    errors.Add($"expected 4 CEO minibots, found {playerCount}");
                }
                if (employeeCount != 12)
                {
                    errors.Add($"expected 12 employee minibots, found {employeeCount}");
                }
                if (CollectComponents<CompanyPlayerMiniBotController>(scene).Count != 1)
                {
                    errors.Add("expected exactly one local player controller (p1 prototype client)");
                }
                if (CollectComponents<CompanyEmployeeAgentController>(scene).Count != 12)
                {
                    errors.Add("every employee minibot must have CompanyEmployeeAgentController");
                }
                if (CollectComponents<CompanyMiniBotMotionDriver>(scene).Count != 16)
                {
                    errors.Add("all CEO and employee minibots must have motion drivers");
                }
                if (CollectComponents<GameBridgeReceiver>(scene).Count != 1)
                {
                    errors.Add("expected exactly one GameBridgeReceiver");
                }
                if (CollectComponents<CompanyGameCoordinator>(scene).Count != 1)
                {
                    errors.Add("expected exactly one CompanyGameCoordinator");
                }
                if (CollectComponents<CompanyGameHud>(scene).Count != 1)
                {
                    errors.Add("expected exactly one CompanyGameHud");
                }
                if (CollectComponents<CompanyCameraController>(scene).Count != 1)
                {
                    errors.Add("expected exactly one CompanyCameraController");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static List<T> CollectComponents<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }
            return result;
        }

        private static void RequireAsset<T>(string path, string label, ICollection<string> errors)
            where T : Object
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            {
                errors.Add($"{label} missing at {path}");
            }
        }
    }
}
