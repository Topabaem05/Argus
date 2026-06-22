using System;
using ArgusUnity.Motion;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace ArgusUnity.Editor
{
    public static class ProceduralWalkBuilder
    {
        [MenuItem("Argus/Build Procedural Walk Rig")]
        public static void Build()
        {
            var rig = FindRig();
            if (rig == null)
            {
                EditorUtility.DisplayDialog("Procedural Walk Rig",
                    "No GameObject with Animator found in scene. Open a scene with the MiniBot rig first.", "OK");
                return;
            }

            var animator = rig.GetComponent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("Procedural Walk Rig",
                    "Selected rig has no Animator component.", "OK");
                return;
            }

            var legController = EnsureComponent<ProceduralLegController>(rig);
            var headFix = EnsureComponent<HeadFixConstraint>(rig);

            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            AssignPrivateField(legController, "leftFoot", leftFoot);
            AssignPrivateField(legController, "rightFoot", rightFoot);
            AssignPrivateField(legController, "footRaycastOrigin", hips);
            AssignPrivateField(headFix, "headBone", head);

            EditorUtility.SetDirty(rig);
            EditorSceneManager_SaveScene();

            var status = $"LegController: leftFoot={leftFoot != null}, rightFoot={rightFoot != null}, origin={hips != null}\n" +
                         $"HeadFix: headBone={head != null}";
            Debug.Log($"ProceduralWalkBuilder: {status}");
            EditorUtility.DisplayDialog("Procedural Walk Rig", status, "OK");
        }

        private static GameObject FindRig()
        {
            var animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                if (anim.isHuman)
                {
                    return anim.gameObject;
                }
            }

            return animators.Length > 0 ? animators[0].gameObject : null;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }

            return comp;
        }

        private static void AssignPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        private static void EditorSceneManager_SaveScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
