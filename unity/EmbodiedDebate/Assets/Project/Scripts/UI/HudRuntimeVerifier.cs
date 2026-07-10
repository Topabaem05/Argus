using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArgusUnity.Bridge;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Runtime smoke test for the HUD. Attach to HudBootstrap; auto-runs in play mode.
    /// </summary>
    public sealed class HudRuntimeVerifier : MonoBehaviour
    {
        private const string OutputDir = "/Users/guribbong/code/Argus/.omo/evidence";
        private float timer;
        private bool fired;
        private bool captured;

        private void Awake()
        {
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(Path.Combine(OutputDir, "hud-verify.log"), "Runtime HUD verify started.\n");
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (!fired && timer > 1f)
            {
                fired = true;
                FireTestEvents();
            }

            if (!captured && timer > 3f)
            {
                captured = true;
                CaptureState();
                Application.Quit();
            }
        }

        private void FireTestEvents()
        {
            var receiver = FindObjectOfType<GameBridgeReceiver>();
            if (receiver == null)
            {
                WriteLine("ERROR: GameBridgeReceiver not found.");
                return;
            }

            var state = new JObject
            {
                ["round_number"] = 2,
                ["phase"] = "work",
                ["companies"] = new JArray
                {
                    new JObject
                    {
                        ["player_id"] = "player_1",
                        ["company_name"] = "AlphaCorp",
                        ["funds"] = 15200,
                        ["customer_satisfaction"] = 72,
                        ["completed_tasks"] = 5,
                        ["failed_tasks"] = 1,
                        ["employees"] = new JArray
                        {
                            new JObject
                            {
                                ["employee_id"] = "employee_0",
                                ["display_name"] = "Bot A",
                                ["mood"] = 85,
                                ["is_active"] = true,
                                ["employed_by"] = "player_1",
                                ["personality_tags"] = new JArray { "diligent", "shy" },
                                ["loyalty_map"] = new JObject { ["player_1"] = 30 },
                                ["memory"] = new JArray { " praised today" }
                            }
                        }
                    },
                    new JObject
                    {
                        ["player_id"] = "player_2",
                        ["company_name"] = "BetaTech",
                        ["funds"] = 12100,
                        ["customer_satisfaction"] = 60,
                        ["completed_tasks"] = 4,
                        ["failed_tasks"] = 2,
                        ["employees"] = new JArray()
                    }
                }
            };

            var envelope = new BridgeEnvelope
            {
                Type = "game.state_sync",
                SessionId = "ai-company-game",
                Payload = state,
                MessageId = "verify-state-sync",
                Sequence = 1,
                SentAtMs = 1
            };

            var method = receiver.GetType().GetMethod("Dispatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(receiver, new object[] { envelope });

            WriteLine("Fired game.state_sync with 2 companies and 1 employee.");
        }

        private void CaptureState()
        {
            var path = Path.Combine(OutputDir, "hud-verify-screenshot.png");
            ScreenCapture.CaptureScreenshot(path);
            WriteLine($"Screenshot: {path}");

            var manager = HudManager.Instance;
            WriteLine($"HudManager instance: {manager != null}");
            if (manager != null)
            {
                WriteLine($"HudCanvas: {manager.HudCanvas != null}");
                WriteLine($"TopLeftPanel: {manager.TopLeftPanel != null}");
                WriteLine($"TopCenterPanel: {manager.TopCenterPanel != null}");
                WriteLine($"TopRightPanel: {manager.TopRightPanel != null}");
                WriteLine($"BottomCenterPanel: {manager.BottomCenterPanel != null}");
                WriteLine($"RightEdgePanel: {manager.RightEdgePanel != null}");
                WriteLine($"CenterOverlayPanel: {manager.CenterOverlayPanel != null}");
            }

            var registry = FindObjectOfType<EmployeeRegistry>();
            WriteLine($"EmployeeRegistry annotated: {registry?.Dots.Count ?? 0}");

            var selector = FindObjectOfType<EmployeeSelector>();
            WriteLine($"EmployeeSelector present: {selector != null}");

            var commandBar = FindObjectOfType<CommandActionBar>();
            WriteLine($"CommandActionBar present: {commandBar != null}");

            var pauseMenu = FindObjectOfType<PauseMenu>();
            WriteLine($"PauseMenu present: {pauseMenu != null}");
        }

        private static void WriteLine(string line)
        {
            Directory.CreateDirectory(OutputDir);
            File.AppendAllText(Path.Combine(OutputDir, "hud-verify.log"), line + "\n");
        }
    }
}
