using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Integration checks run against real imported Animators in Play Mode.
[InitializeOnLoad]
public static class HeavyValidation
{
    private static int phase;
    private static float since;
    private static PlayerShoothing player;
    private static Gun gun;
    private static int ammo;
    private static Enemy target;
    private static string report;
    static HeavyValidation() { EditorApplication.update += Tick; }

    public static void RunBatch()
    {
        try { HeavySetup.Configure(); Run(); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Heavy/Validate in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Heavy Demo.unity");
        phase = 0;
        since = 0;
        report = "";
        SessionState.SetString("HeavyValidationDeadline", DateTime.UtcNow.AddSeconds(90).ToString("O"));
        SessionState.SetBool("HeavyValidationRunning", true);
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        report += "PASS: " + message + "\n";
    }
    private static void Next() { phase++; since = Time.time; }
    private static void Tick()
    {
        if (!SessionState.GetBool("HeavyValidationRunning", false)) return;
        if (!DateTime.TryParse(SessionState.GetString("HeavyValidationDeadline", ""), null,
            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime deadline) || DateTime.UtcNow > deadline)
        {
            SessionState.SetBool("HeavyValidationRunning", false);
            File.WriteAllText("Temp/HeavyValidation.result", report + "INCOMPLETE: Editor did not finish the Play Mode checks within 90 seconds.");
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        try
        {
            Application.runInBackground = true;
            EditorApplication.QueuePlayerLoopUpdate();
            float elapsed = Time.time - since;
            if (phase == 0)
            {
                player = UnityEngine.Object.FindFirstObjectByType<PlayerShoothing>();
                if (player == null || player.gun == null || Time.time < 0.5f) return;
                foreach (Enemy enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                gun = player.gun;
                Check(player.GetComponent<PlayerClass>().CurrentClass.displayName == "Heavy", "Heavy selected");
                Check(player.GetComponent<PlayerHealth>().health == 300, "300 health");
                Check(Mathf.Approximately(player.GetComponent<PlayerMove>().movespeed, 5.75f), "Heavy speed");
                Check(gun.animatedMinigun && gun.CurrentAmmo == 200, "Minigun equipped with 200 rounds");
                Next();
            }
            else if (phase == 1 && elapsed > 1.5f)
            {
                // Disable input polling so this test supplies deterministic input.
                player.enabled = false;
                gun.UpdateMinigunInput(false, true);
                Check(gun.IsSpinning && !gun.CanSwitch, "Spin restricts movement and switching");
                Check(Mathf.Abs(gun.MovementMultiplier - 110f / 230f) < 0.001f, "Spun speed ratio");
                Next();
            }
            else if (phase == 2)
            {
                gun.UpdateMinigunInput(false, true);
                if (elapsed < 1.3f) return;
                Check(gun.CurrentAmmo == 200, "Right click spins without consuming ammunition");
                Check(gun.weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("@spool_idle"), "Spin reaches spool_idle");
                var body = player.GetComponent<Rigidbody>();
                float before = body.linearVelocity.y;
                typeof(PlayerMove).GetMethod("TryJump", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player.GetComponent<PlayerMove>(), null);
                Check(body.linearVelocity.y == before, "Jump blocked while spinning");
                GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dummy.name = "HeavyValidationTarget";
                Camera camera = player.GetComponentInChildren<Camera>();
                dummy.transform.position = camera.transform.position + camera.transform.forward * 3f;
                dummy.transform.localScale = Vector3.one * 2f;
                target = dummy.AddComponent<Enemy>();
                target.enabled = false;
                target.health = 10000;
                Physics.SyncTransforms();
                Next();
            }
            else if (phase == 3)
            {
                gun.UpdateMinigunInput(true, true);
                if (elapsed < 1.2f) return;
                Check(gun.CurrentAmmo < 200 && gun.CurrentAmmo >= 187, "Automatic fire consumes ammunition at bounded cadence");
                Check(target.health < 10000, "Hitscan damages targets");
                UnityEngine.Object.Destroy(target.gameObject);
                ammo = gun.CurrentAmmo;
                Next();
            }
            else if (phase == 4)
            {
                gun.UpdateMinigunInput(false, true);
                if (elapsed < 0.3f) return;
                Check(gun.CurrentAmmo == ammo, "Release stops firing while right click keeps spinning");
                Next();
            }
            else if (phase == 5)
            {
                gun.UpdateMinigunInput(false, false);
                if (elapsed < 2f) return;
                Check(!gun.IsSpinning && gun.CanSwitch, "Spin-down restores movement and switching");
                gun.TryReload();
                Check(gun.CurrentAmmo == ammo && !gun.IsReloading, "Minigun cannot reload for free");
                player.enabled = true;
                player.SelectWeapon(1);
                Next();
            }
            else if (phase == 6 && elapsed > 1.5f)
            {
                gun = player.gun;
                Check(gun.animatedShotgun && gun.CurrentAmmo == 6 && gun.ReserveAmmo == 32, "Shotgun equipped with 6 plus 32");
                gun.Shoot();
                Check(gun.CurrentAmmo == 5, "Shotgun fires one cartridge");
                Next();
            }
            else if (phase == 7 && elapsed > 1.2f)
            {
                gun.TryReload();
                Check(gun.IsReloading, "Shotgun reload starts");
                Next();
            }
            else if (phase == 8 && elapsed > 3f)
            {
                Check(gun.CurrentAmmo == 6 && gun.ReserveAmmo == 31 && !gun.IsReloading, "Reload transfers exactly one reserve shell");
                player.SelectWeapon(0);
                Check(player.gun.CurrentAmmo == ammo, "Switching preserves minigun ammunition");
                Next();
            }
            else if (phase == 9 && elapsed > 1.5f)
            {
                // Empty the belt without waiting 20 seconds; exercise the real firing path.
                typeof(Gun).GetField("currentAmmo", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player.gun, 0);
                player.enabled = false;
                gun = player.gun;
                Next();
            }
            else if (phase == 10)
            {
                gun.UpdateMinigunInput(true, true);
                if (elapsed < 1.5f) return;
                Check(gun.CurrentAmmo == 0 && !gun.IsReloading, "Empty minigun does not fire or create ammunition");
                Next();
            }
            else if (phase == 11)
            {
                gun.UpdateMinigunInput(false, false);
                if (elapsed < 2f) return;
                player.enabled = true;
                player.SelectWeapon(1);
                Next();
            }
            else if (phase == 12 && elapsed > 1.5f)
            {
                gun = player.gun;
                gun.Shoot();
                Next();
            }
            else if (phase == 13 && elapsed > 1f)
            {
                gun.TryReload();
                Next();
            }
            else if (phase == 14 && elapsed > 0.1f)
            {
                gun.Shoot();
                Check(!gun.IsReloading && gun.CurrentAmmo == 4 && gun.ReserveAmmo == 31, "Firing interrupts reload without spending an unfinished shell");
                Next();
            }
            else if (phase == 15 && elapsed > 1f)
            {
                typeof(Gun).GetField("reserveAmmo", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gun, 0);
                gun.TryReload();
                Check(!gun.IsReloading && gun.CurrentAmmo == 4, "Empty reserve cannot refill shotgun");
                var scout = AssetDatabase.LoadAssetAtPath<Classes>("Assets/Classes/Scout.asset");
                Check(player.GetComponent<PlayerClass>().SetClass(scout), "Can switch back to Scout");
                Check(player.GetComponent<PlayerHealth>().MaxHealth == 125 && player.GetComponent<PlayerMove>().airJumps == 1, "Scout stats and double jump preserved");
                SessionState.SetBool("HeavyValidationRunning", false);
                File.WriteAllText("Temp/HeavyValidation.result", report);
                Debug.Log(report);
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            SessionState.SetBool("HeavyValidationRunning", false);
            File.WriteAllText("Temp/HeavyValidation.result", report + "FAIL: " + e);
            Debug.LogException(e);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
