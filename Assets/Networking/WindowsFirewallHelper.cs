// Made by HCS, its entirely free to use aslong as you credit me. If you need: "© 2025 HCS"
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public static class WindowsFirewallHelper
{
    public static void OpenHostPorts()
    {
        TryAddRuleIfNotExists("PixelDuelers-LAN-UDP", 47777, "UDP");
        TryAddRuleIfNotExists("PixelDuelers-Game-UDP", 7777, "UDP");
        TryAddRuleIfNotExists("PixelDuelers-Game-TCP", 7777, "TCP");
    }

    public static void OpenClientPorts()
    {
        TryAddRuleIfNotExists("PixelDuelers-LAN-UDP", 47777, "UDP");
    }

    private static void TryAddRuleIfNotExists(string name, int port, string protocol)
    {
        string checkCmd = $"advfirewall firewall show rule name=\"{name}\"";
        var checkPsi = new ProcessStartInfo("netsh", checkCmd)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var checkProcess = Process.Start(checkPsi);
            if (checkProcess != null)
            {
                string output = checkProcess.StandardOutput.ReadToEnd();
                checkProcess.WaitForExit();

                if (output.Contains("Rule Name"))
                {
                    UnityEngine.Debug.Log($"[FW] Rule {name} already exists. Skipping add.");
                    return;
                }
            }
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"[FW] Failed to check rule {name}. Continuing anyway.");
        }

        // If rule doesn't exist, add it
        string args = $"advfirewall firewall add rule name=\"{name}\" dir=in action=allow profile=any protocol={protocol} localport={port}";

        var addPsi = new ProcessStartInfo("netsh", args)
        {
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        try
        {
            using var addProcess = Process.Start(addPsi);
            addProcess?.WaitForExit();

            if (addProcess == null)
                UnityEngine.Debug.LogWarning($"[FW] Failed to start netsh for rule {name}");
            else if (addProcess.ExitCode == 0)
                UnityEngine.Debug.Log($"[FW] Rule {name} created.");
            else
                UnityEngine.Debug.LogWarning($"[FW] netsh exit {addProcess.ExitCode} while adding {name} (may already exist).");
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"[FW] User cancelled UAC or netsh start failed for {name}.");
        }
    }
}
#endif
