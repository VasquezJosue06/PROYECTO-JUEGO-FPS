using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Integration checks run against real imported Animators in Play Mode.
[InitializeOnLoad]
public static class ValidacionHeavy
{
    private static int fase;
    private static float desde;
    private static DisparoJugador jugador;
    private static Arma armaActual;
    private static int municion;
    private static Enemigo objetivo;
    private static string informe;
    static ValidacionHeavy() { EditorApplication.update += ActualizarPaso; }

    public static void EjecutarLote()
    {
        try { ConfiguracionHeavy.Configurar(); Ejecutar(); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Heavy/Validate in Play Mode")]
    public static void Ejecutar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Heavy Demo.unity");
        fase = 0;
        desde = 0;
        informe = "";
        SessionState.SetString("HeavyValidationDeadline", DateTime.UtcNow.AddSeconds(90).ToString("O"));
        SessionState.SetBool("HeavyValidationRunning", true);
        EditorApplication.isPlaying = true;
    }
    private static void Comprobar(bool condition, string mensaje)
    {
        if (!condition) throw new Exception(mensaje);
        informe += "PASS: " + mensaje + "\n";
    }
    private static void Avanzar() { fase++; desde = Time.time; }
    private static void ActualizarPaso()
    {
        if (!SessionState.GetBool("HeavyValidationRunning", false)) return;
        if (!DateTime.TryParse(SessionState.GetString("HeavyValidationDeadline", ""), null,
            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime deadline) || DateTime.UtcNow > deadline)
        {
            SessionState.SetBool("HeavyValidationRunning", false);
            File.WriteAllText("Temp/HeavyValidation.result", informe + "INCOMPLETE: Editor did not finish the Play Mode checks within 90 seconds.");
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        try
        {
            Application.runInBackground = true;
            EditorApplication.QueuePlayerLoopUpdate();
            float transcurrido = Time.time - desde;
            if (fase == 0)
            {
                jugador = UnityEngine.Object.FindFirstObjectByType<DisparoJugador>();
                if (jugador == null || jugador.armaActual == null || Time.time < 0.5f) return;
                foreach (Enemigo enemy in UnityEngine.Object.FindObjectsByType<Enemigo>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                armaActual = jugador.armaActual;
                Comprobar(jugador.GetComponent<ClaseJugador>().ClaseActual.nombreVisible == "Heavy", "Heavy selected");
                Comprobar(jugador.GetComponent<VidaJugador>().vida == 300, "300 health");
                Comprobar(Mathf.Approximately(jugador.GetComponent<MovimientoJugador>().velocidadMovimiento, 5.75f), "Heavy speed");
                Comprobar(armaActual.minigunAnimada && armaActual.MunicionActual == 200, "Minigun equipped with 200 rounds");
                Avanzar();
            }
            else if (fase == 1 && transcurrido > 1.5f)
            {
                // Disable input polling so this test supplies deterministic input.
                jugador.enabled = false;
                armaActual.ActualizarEntradaMinigun(false, true);
                Comprobar(armaActual.EstaGirando && !armaActual.PuedeCambiarArma, "Spin restricts movement and switching");
                Comprobar(Mathf.Abs(armaActual.MultiplicadorMovimiento - 110f / 230f) < 0.001f, "Spun speed ratio");
                Avanzar();
            }
            else if (fase == 2)
            {
                armaActual.ActualizarEntradaMinigun(false, true);
                if (transcurrido < 1.3f) return;
                Comprobar(armaActual.MunicionActual == 200, "Right click spins without consuming ammunition");
                Comprobar(armaActual.animadorArma.GetCurrentAnimatorStateInfo(0).IsName("@spool_idle"), "Spin reaches spool_idle");
                var cuerpoRigido = jugador.GetComponent<Rigidbody>();
                float before = cuerpoRigido.linearVelocity.y;
                typeof(MovimientoJugador).GetMethod("IntentarSaltar", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(jugador.GetComponent<MovimientoJugador>(), null);
                Comprobar(cuerpoRigido.linearVelocity.y == before, "Jump blocked while spinning");
                GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dummy.name = "HeavyValidationTarget";
                Camera camera = jugador.GetComponentInChildren<Camera>();
                dummy.transform.position = camera.transform.position + camera.transform.forward * 3f;
                dummy.transform.localScale = Vector3.one * 2f;
                objetivo = dummy.AddComponent<Enemigo>();
                objetivo.enabled = false;
                objetivo.vida = 10000;
                Physics.SyncTransforms();
                Avanzar();
            }
            else if (fase == 3)
            {
                armaActual.ActualizarEntradaMinigun(true, true);
                if (transcurrido < 1.2f) return;
                Comprobar(armaActual.MunicionActual < 200 && armaActual.MunicionActual >= 187, "Automatic fire consumes ammunition at bounded cadence");
                Comprobar(objetivo.vida < 10000, "Hitscan damages targets");
                UnityEngine.Object.Destroy(objetivo.gameObject);
                municion = armaActual.MunicionActual;
                Avanzar();
            }
            else if (fase == 4)
            {
                armaActual.ActualizarEntradaMinigun(false, true);
                if (transcurrido < 0.3f) return;
                Comprobar(armaActual.MunicionActual == municion, "Release stops firing while right click keeps spinning");
                Avanzar();
            }
            else if (fase == 5)
            {
                armaActual.ActualizarEntradaMinigun(false, false);
                if (transcurrido < 2f) return;
                Comprobar(!armaActual.EstaGirando && armaActual.PuedeCambiarArma, "Spin-down restores movement and switching");
                armaActual.IntentarRecargar();
                Comprobar(armaActual.MunicionActual == municion && !armaActual.EstaRecargando, "Minigun cannot reload for free");
                jugador.enabled = true;
                jugador.SeleccionarArma(1);
                Avanzar();
            }
            else if (fase == 6 && transcurrido > 1.5f)
            {
                armaActual = jugador.armaActual;
                Comprobar(armaActual.escopetaAnimada && armaActual.MunicionActual == 6 && armaActual.MunicionReserva == 32, "Shotgun equipped with 6 plus 32");
                armaActual.Disparar();
                Comprobar(armaActual.MunicionActual == 5, "Shotgun fires one cartridge");
                Avanzar();
            }
            else if (fase == 7 && transcurrido > 1.2f)
            {
                armaActual.IntentarRecargar();
                Comprobar(armaActual.EstaRecargando, "Shotgun reload starts");
                Avanzar();
            }
            else if (fase == 8 && transcurrido > 3f)
            {
                Comprobar(armaActual.MunicionActual == 6 && armaActual.MunicionReserva == 31 && !armaActual.EstaRecargando, "Reload transfers exactly one reserve shell");
                jugador.SeleccionarArma(0);
                Comprobar(jugador.armaActual.MunicionActual == municion, "Switching preserves minigun ammunition");
                Avanzar();
            }
            else if (fase == 9 && transcurrido > 1.5f)
            {
                // Empty the belt without waiting 20 seconds; exercise the real firing path.
                typeof(Arma).GetField("municionActual", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(jugador.armaActual, 0);
                jugador.enabled = false;
                armaActual = jugador.armaActual;
                Avanzar();
            }
            else if (fase == 10)
            {
                armaActual.ActualizarEntradaMinigun(true, true);
                if (transcurrido < 1.5f) return;
                Comprobar(armaActual.MunicionActual == 0 && !armaActual.EstaRecargando, "Empty minigun does not fire or create ammunition");
                Avanzar();
            }
            else if (fase == 11)
            {
                armaActual.ActualizarEntradaMinigun(false, false);
                if (transcurrido < 2f) return;
                jugador.enabled = true;
                jugador.SeleccionarArma(1);
                Avanzar();
            }
            else if (fase == 12 && transcurrido > 1.5f)
            {
                armaActual = jugador.armaActual;
                armaActual.Disparar();
                Avanzar();
            }
            else if (fase == 13 && transcurrido > 1f)
            {
                armaActual.IntentarRecargar();
                Avanzar();
            }
            else if (fase == 14 && transcurrido > 0.1f)
            {
                armaActual.Disparar();
                Comprobar(!armaActual.EstaRecargando && armaActual.MunicionActual == 4 && armaActual.MunicionReserva == 31, "Firing interrupts reload without spending an unfinished shell");
                Avanzar();
            }
            else if (fase == 15 && transcurrido > 1f)
            {
                typeof(Arma).GetField("municionReserva", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(armaActual, 0);
                armaActual.IntentarRecargar();
                Comprobar(!armaActual.EstaRecargando && armaActual.MunicionActual == 4, "Empty reserve cannot refill shotgun");
                var claseScout = AssetDatabase.LoadAssetAtPath<ClasesJugador>("Assets/Classes/Scout.asset");
                Comprobar(jugador.GetComponent<ClaseJugador>().SeleccionarClase(claseScout), "Can switch back to Scout");
                Comprobar(jugador.GetComponent<VidaJugador>().VidaMaxima == 125 && jugador.GetComponent<MovimientoJugador>().saltosAereos == 1, "Scout stats and double jump preserved");
                SessionState.SetBool("HeavyValidationRunning", false);
                File.WriteAllText("Temp/HeavyValidation.result", informe);
                Debug.Log(informe);
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            SessionState.SetBool("HeavyValidationRunning", false);
            File.WriteAllText("Temp/HeavyValidation.result", informe + "FAIL: " + e);
            Debug.LogException(e);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
