using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ValidacionVisualHeavy
{
    private static void Exigir(bool condition, string mensaje)
    {
        if (!condition) throw new Exception(mensaje);
    }
    [MenuItem("Tools/Heavy/Validate Visual Assets")]
    public static void Ejecutar()
    {
        var escena = EditorSceneManager.NewPreviewScene();
        GrupoTrazadorasInstantaneas pool = null;
        try
        {
            var prefabricado = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TF2/Heavy/HeavyMinigun.prefab");
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabricado, escena);
            var armaActual = instancia.GetComponent<Arma>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Arma).GetMethod("BuscarTamborMinigun", flags).Invoke(armaActual, null);
            Exigir(armaActual.tamborMinigun != null, "Barrel bone not found");
            Vector3 eje = (Vector3)typeof(Arma).GetField("ejeTambor", flags).GetValue(armaActual);
            Exigir(Vector3.Dot(eje, Vector3.right) > 0.99f, "Unexpected barrel axis");
            typeof(Arma).GetField("inicializado", flags).SetValue(armaActual, true);
            var animator = armaActual.animadorArma;
            animator.Rebind();
            animator.SetBool("Spin", true);
            animator.Play("Base Layer.@spool_idle", 0, 0);
            animator.Update(0);
            Vector3 barrelDirection = armaActual.tamborMinigun.right;
            Quaternion parentRotation = armaActual.tamborMinigun.parent.localRotation;
            MethodInfo animate = typeof(Arma).GetMethod("AnimarTamborMinigun", flags);
            float max = armaActual.gradosTamborPorSegundo;
            for (int i = 0; i < 10; i++) animate.Invoke(armaActual, new object[] { 0.1f });
            float velocidad = (float)typeof(Arma).GetField("velocidadTambor", flags).GetValue(armaActual);
            Exigir(Mathf.Abs(velocidad - max) < 0.1f, "Barrel did not accelerate to full speed");
            Exigir(Quaternion.Angle(parentRotation, armaActual.tamborMinigun.parent.localRotation) < 0.01f, "Spin changed parent pose");
            Exigir(Vector3.Dot(barrelDirection, armaActual.tamborMinigun.right) > 0.999f, "Barrel wobbles instead of spinning along its length");
            Quaternion rotation = armaActual.tamborMinigun.localRotation;
            animate.Invoke(armaActual, new object[] { 0f });
            Exigir(Quaternion.Angle(rotation, armaActual.tamborMinigun.localRotation) < 0.01f, "Spin moves while paused");
            animator.Play("Base Layer.@spool_down", 0, 0);
            animator.Update(0);
            for (int i = 0; i < 7; i++) animate.Invoke(armaActual, new object[] { 0.1f });
            Exigir((float)typeof(Arma).GetField("velocidadTambor", flags).GetValue(armaActual) < 0.01f, "Barrel did not stop");
            var material = Resources.Load<Material>("WeaponTracerMaterial");
            Exigir(material != null && material.shader != null, "Missing tracer material or shader");
            Exigir(!ShaderUtil.ShaderHasError(material.shader), "Tracer shader has compilation errors");
            pool = new GrupoTrazadorasInstantaneas();
            for (int i = 0; i < 70; i++) pool.Emitir(Vector3.zero, Vector3.forward * 10, 0.015f, 0.075f);
            var lineas = (LineRenderer[])typeof(GrupoTrazadorasInstantaneas).GetField("lineas", flags).GetValue(pool);
            Exigir(lineas.Length == 48 && lineas[47] != null, "Tracer pool capacity/reuse failed");
            Exigir(lineas[0].GetPosition(1).z > 0 && lineas[0].GetPosition(1).z < 1, "Tracer appears as a full instant line");
            Exigir(lineas[0].gameObject.layer != LayerMask.NameToLayer("Viewmodel"), "Tracer on overlay layer");
            var marcos = (int[])typeof(GrupoTrazadorasInstantaneas).GetField("marcosGenerados", flags).GetValue(pool);
            for (int i = 0; i < marcos.Length; i++) marcos[i] = Time.frameCount - 1;
            Vector3 initialHead = lineas[0].GetPosition(1);
            pool.ActualizarPaso(0f);
            Exigir(lineas[0].GetPosition(1) == initialHead, "Tracer moves while paused");
            pool.ActualizarPaso(0.02f);
            Exigir(lineas[0].GetPosition(1).z > initialHead.z && lineas[0].GetPosition(1).z < 10f, "Tracer does not travel");
            Exigir(Vector3.Distance(lineas[0].GetPosition(0), lineas[0].GetPosition(1)) <= 1.401f, "Tail exceeds configured length");
            pool.ActualizarPaso(1f);
            Exigir(lineas[0].GetPosition(1) == Vector3.forward * 10f, "Tracer passes impact point");
            Exigir(lineas[0].gameObject.activeSelf, "Impact not visible at low FPS");
            pool.ActualizarPaso(0.1f);
            foreach (var line in lineas) Exigir(!line.gameObject.activeSelf, "Expired tracer still active");
            // Simula una boca que cambia de pose entre el disparo y LateUpdate.
            var cameraObject = new GameObject("Tracer validation camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, escena);
            typeof(Arma).GetField("camaraApuntado", flags).SetValue(armaActual, cameraObject.AddComponent<Camera>());
            typeof(Arma).GetField("camaraArma", flags).SetValue(armaActual, null);
            typeof(Arma).GetField("trazadoras", flags).SetValue(armaActual, pool);
            var pendientes = (System.Collections.Generic.List<Vector3>)typeof(Arma).GetField("finalesTrazadorasPendientes", flags).GetValue(armaActual);
            armaActual.puntoGeneracionProyectil = armaActual.tamborMinigun;
            pendientes.Add(Vector3.forward * 10f);
            armaActual.puntoGeneracionProyectil.position = new Vector3(1f, 2f, 3f);
            int posicionArreglo = (int)typeof(GrupoTrazadorasInstantaneas).GetField("siguiente", flags).GetValue(pool);
            typeof(Arma).GetMethod("EmitirTrazadorasPendientes", flags).Invoke(armaActual, null);
            Exigir(lineas[posicionArreglo].GetPosition(0) == armaActual.puntoGeneracionProyectil.position, "Tracer uses stale reload pose");
            Exigir(pendientes.Count == 0, "Pending tracers were not consumed");
            typeof(Arma).GetField("trazadoras", flags).SetValue(armaActual, null);
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
                var lineas = (LineRenderer[])typeof(GrupoTrazadorasInstantaneas).GetField("lineas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);
                foreach (var line in lineas) if (line != null) UnityEngine.Object.DestroyImmediate(line.gameObject);
            }
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }
}
