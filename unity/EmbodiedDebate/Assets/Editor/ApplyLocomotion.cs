using ArgusUnity.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    public static class ApplyLocomotion
    {
        private const string ControllerPath = "Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion2D.controller";

        [MenuItem("Argus/Apply 2D Locomotion to Mini-bot")]
        public static void Apply()
        {
            var robot = FindRobot();
            if (robot == null)
            {
                EditorUtility.DisplayDialog("Apply Locomotion", "No robot found in scene.", "OK");
                return;
            }

            var animator = robot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = robot.AddComponent<Animator>();
            }

            var controllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controllerAsset == null)
            {
                EditorUtility.DisplayDialog("Apply Locomotion", "MiniBotLocomotion2D.controller not found at " + ControllerPath, "OK");
                return;
            }

            animator.runtimeAnimatorController = controllerAsset;
            animator.applyRootMotion = false;

            var charController = robot.GetComponent<CharacterController>();
            if (charController == null)
            {
                charController = robot.AddComponent<CharacterController>();
            }
            charController.height = 1.2f;
            charController.radius = 0.25f;
            charController.center = new Vector3(0f, 0.6f, 0f);

            var handler = robot.GetComponent<AnimationHandler>();
            if (handler == null)
            {
                handler = robot.AddComponent<AnimationHandler>();
            }

            var so = new SerializedObject(handler);
            SetObject(so, "animator", animator);
            SetObject(so, "controller", charController);
            SetFloat(so, "walkSpeed", 2f);
            SetFloat(so, "runSpeed", 3f);
            SetBool(so, "faceMoveDirection", true);
            SetFloat(so, "bodyRotationSpeed", 12f);
            SetBool(so, "enableMouseLook", false);
            so.ApplyModifiedProperties();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("ApplyLocomotion: wired Animator + CharacterController + AnimationHandler on " + robot.name);
            EditorUtility.DisplayDialog("Apply Locomotion", "Done! Animator + CharacterController + AnimationHandler on " + robot.name, "OK");
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.boolValue = value;
        }

        private static void SetObject(SerializedObject so, string name, Object value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static GameObject FindRobot()
        {
            if (Selection.activeGameObject != null)
            {
                var selected = Selection.activeGameObject;
                if (selected.GetComponentInChildren<Animator>() != null || selected.GetComponentInParent<Animator>() != null)
                {
                    return selected.transform.root.gameObject;
                }
            }

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.IndexOf("mini-bot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return root;
                }
            }

            var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                if (anim.isHuman)
                {
                    return anim.gameObject;
                }
            }

            return animators.Length > 0 ? animators[0].gameObject : null;
        }
    }
}
