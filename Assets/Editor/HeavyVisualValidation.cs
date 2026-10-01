using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HeavyVisualValidation
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    [MenuItem("Tools/Heavy/Validate Visual Assets")]
    public static void Run()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        HitscanTracerPool pool = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TF2/Heavy/HeavyMinigun.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var gun = instance.GetComponent<Gun>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Gun).GetMethod("FindMinigunBarrel", flags).Invoke(gun, null);
            Require(gun.minigunBarrel != null, "Barrel bone not found");
            Vector3 axis = (Vector3)typeof(Gun).GetField("barrelAxis", flags).GetValue(gun);
            Require(Vector3.Dot(axis, Vector3.right) > 0.99f, "Unexpected barrel axis");
            typeof(Gun).GetField("initialized", flags).SetValue(gun, true);
            var animator = gun.weaponAnimator;
            animator.Rebind();
            animator.SetBool("Spin", true);
            animator.Play("Base Layer.@spool_idle", 0, 0);
            animator.Update(0);
            Vector3 barrelDirection = gun.minigunBarrel.right;
            Quaternion parentRotation = gun.minigunBarrel.parent.localRotation;
            MethodInfo animate = typeof(Gun).GetMethod("AnimateMinigunBarrel", flags);
            float max = gun.barrelDegreesPerSecond;
            for (int i = 0; i < 10; i++) animate.Invoke(gun, new object[] { 0.1f });
            float speed = (float)typeof(Gun).GetField("barrelSpeed", flags).GetValue(gun);
            Require(Mathf.Abs(speed - max) < 0.1f, "Barrel did not accelerate to full speed");
            Require(Quaternion.Angle(parentRotation, gun.minigunBarrel.parent.localRotation) < 0.01f, "Spin changed parent pose");
            Require(Vector3.Dot(barrelDirection, gun.minigunBarrel.right) > 0.999f, "Barrel wobbles instead of spinning along its length");
            Quaternion rotation = gun.minigunBarrel.localRotation;
            animate.Invoke(gun, new object[] { 0f });
            Require(Quaternion.Angle(rotation, gun.minigunBarrel.localRotation) < 0.01f, "Spin moves while paused");
            animator.Play("Base Layer.@spool_down", 0, 0);
            animator.Update(0);
            for (int i = 0; i < 7; i++) animate.Invoke(gun, new object[] { 0.1f });
            Require((float)typeof(Gun).GetField("barrelSpeed", flags).GetValue(gun) < 0.01f, "Barrel did not stop");
            var material = Resources.Load<Material>("WeaponTracerMaterial");
            Require(material != null && material.shader != null, "Missing tracer material or shader");
            Require(!ShaderUtil.ShaderHasError(material.shader), "Tracer shader has compilation errors");
            pool = new HitscanTracerPool();
            for (int i = 0; i < 70; i++) pool.Emit(Vector3.zero, Vector3.forward * 10, 0.015f, 0.075f);
            var lines = (LineRenderer[])typeof(HitscanTracerPool).GetField("lines", flags).GetValue(pool);
            Require(lines.Length == 48 && lines[47] != null, "Tracer pool capacity/reuse failed");
            Require(lines[0].GetPosition(1).z > 0 && lines[0].GetPosition(1).z < 1, "Tracer appears as a full instant line");
            Require(lines[0].gameObject.layer != LayerMask.NameToLayer("Viewmodel"), "Tracer on overlay layer");
            var frames = (int[])typeof(HitscanTracerPool).GetField("spawnedFrames", flags).GetValue(pool);
            for (int i = 0; i < frames.Length; i++) frames[i] = Time.frameCount - 1;
            Vector3 initialHead = lines[0].GetPosition(1);
            pool.Tick(0f);
            Require(lines[0].GetPosition(1) == initialHead, "Tracer moves while paused");
            pool.Tick(0.02f);
            Require(lines[0].GetPosition(1).z > initialHead.z && lines[0].GetPosition(1).z < 10f, "Tracer does not travel");
            Require(Vector3.Distance(lines[0].GetPosition(0), lines[0].GetPosition(1)) <= 1.401f, "Tail exceeds configured length");
            pool.Tick(1f);
            Require(lines[0].GetPosition(1) == Vector3.forward * 10f, "Tracer passes impact point");
            Require(lines[0].gameObject.activeSelf, "Impact not visible at low FPS");
            pool.Tick(0.1f);
            foreach (var line in lines) Require(!line.gameObject.activeSelf, "Expired tracer still active");
            // Simula una boca que cambia de pose entre el disparo y LateUpdate.
            var cameraObject = new GameObject("Tracer validation camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            typeof(Gun).GetField("aimCamera", flags).SetValue(gun, cameraObject.AddComponent<Camera>());
            typeof(Gun).GetField("viewmodelCamera", flags).SetValue(gun, null);
            typeof(Gun).GetField("tracers", flags).SetValue(gun, pool);
            var pending = (System.Collections.Generic.List<Vector3>)typeof(Gun).GetField("pendingTracerEnds", flags).GetValue(gun);
            gun.bulletSpawnPoint = gun.minigunBarrel;
            pending.Add(Vector3.forward * 10f);
            gun.bulletSpawnPoint.position = new Vector3(1f, 2f, 3f);
            int slot = (int)typeof(HitscanTracerPool).GetField("next", flags).GetValue(pool);
            typeof(Gun).GetMethod("EmitPendingTracers", flags).Invoke(gun, null);
            Require(lines[slot].GetPosition(0) == gun.bulletSpawnPoint.position, "Tracer uses stale reload pose");
            Require(pending.Count == 0, "Pending tracers were not consumed");
            typeof(Gun).GetField("tracers", flags).SetValue(gun, null);
            File.WriteAllText("Temp/HeavyVisualValidation.result", "PASS: exported barrel bone/axis; acceleration; pause; parent pose; deceleration; material/shader; bounded tracer reuse; endpoints; world layer; expiration.");
            Debug.Log("Heavy visual asset validation passed.");
        }
        catch (Exception e)
        {
            File.WriteAllText("Temp/HeavyVisualValidation.result", "FAIL: " + e);
            Debug.LogException(e);
        }
        finally
        {
            if (pool != null)
            {
                var lines = (LineRenderer[])typeof(HitscanTracerPool).GetField("lines", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);
                foreach (var line in lines) if (line != null) UnityEngine.Object.DestroyImmediate(line.gameObject);
            }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
