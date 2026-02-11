using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using Unity.Netcode;
using TMPro;

public class LANBroadcastHost : MonoBehaviour
{
    #region Inspector
    [Header("Broadcast Settings")]
    [SerializeField] private int broadcastPort = 47777;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI broadcastStatusText;

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseStrength = 2f;
    #endregion

    #region State
    private UdpClient udpClient;
    private NetworkManager networkManager;

    [SerializeField] private float timer;
    [SerializeField] private float elapsedTime;
    [SerializeField] private float currentInterval = 3f;

    private float baseFontSize;
    private bool hasStopped;
    #endregion

    #region Unity lifecycle
    private void Awake()
    {
        networkManager = NetworkManager.Singleton;

        if (broadcastStatusText != null)
            baseFontSize = broadcastStatusText.fontSize;
    }

    private void Start()
    {
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        Debug.Log("[LANBroadcastHost] Broadcast started.");
    }

    private void Update()
    {
        if (hasStopped)
            return;

        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                StopBroadcasting();
                return;
            }
        }

        if (!networkManager.IsHost)
        {
            StopBroadcasting();
            return;
        }

        if (networkManager.ConnectedClientsList != null && networkManager.ConnectedClientsList.Count >= 2)
        {
            StopBroadcasting();
            return;
        }

        elapsedTime += Time.deltaTime;
        timer += Time.deltaTime;

        UpdateBroadcastInterval();
        UpdateBroadcastStatusText();
        UpdatePulseEffect();

        if (timer >= currentInterval)
        {
            timer = 0f;
            BroadcastHostInfo();
        }
    }

    private void OnDestroy()
    {
        StopBroadcasting();
    }
    #endregion

    #region Broadcast
    private void BroadcastHostInfo()
    {
        if (udpClient == null)
            return;

        string hostIP = GetLocalIPAddress();
        string message = $"FIGHTHOST:{hostIP}";
        byte[] data = Encoding.UTF8.GetBytes(message);

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                IPAddress broadcastAddress = GetBroadcastAddress(ip.Address, ip.IPv4Mask);
                if (broadcastAddress == null)
                    continue;

                try
                {
                    IPEndPoint endPoint = new IPEndPoint(broadcastAddress, broadcastPort);
                    udpClient.Send(data, data.Length, endPoint);
                }
                catch (SocketException ex)
                {
                    Debug.LogWarning($"[LANBroadcastHost] Failed broadcast to {broadcastAddress}: {ex.Message}");
                }
            }
        }
    }

    private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
    {
        byte[] ipBytes = address.GetAddressBytes();
        byte[] maskBytes = subnetMask.GetAddressBytes();

        if (ipBytes.Length != maskBytes.Length)
            return null;

        byte[] broadcastBytes = new byte[ipBytes.Length];
        for (int i = 0; i < ipBytes.Length; i++)
            broadcastBytes[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));

        return new IPAddress(broadcastBytes);
    }

    private void StopBroadcasting()
    {
        if (hasStopped)
            return;

        hasStopped = true;

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        enabled = false;
    }
    #endregion

    #region UI
    private void UpdateBroadcastInterval()
    {
        if (elapsedTime < 30f) currentInterval = 3f;
        else if (elapsedTime < 120f) currentInterval = 10f;
        else currentInterval = 30f;
    }

    private void UpdateBroadcastStatusText()
    {
        if (broadcastStatusText == null)
            return;

        int elapsedSeconds = Mathf.FloorToInt(elapsedTime);
        int intervalSeconds = Mathf.FloorToInt(currentInterval);

        string colorTag =
            elapsedSeconds < 30 ? "<color=green>" :
            elapsedSeconds < 120 ? "<color=yellow>" :
            "<color=red>";

        broadcastStatusText.text = $"{colorTag}Elapsed: {elapsedSeconds}s\nBroadcast every: {intervalSeconds}s</color>";
    }

    private void UpdatePulseEffect()
    {
        if (broadcastStatusText == null)
            return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseStrength;
        broadcastStatusText.fontSize = baseFontSize + pulse;
    }
    #endregion

    #region IP
    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork)
                continue;

            if (IPAddress.IsLoopback(ip))
                continue;

            string s = ip.ToString();
            if (s.StartsWith("169.") || s.StartsWith("127.") || s.StartsWith("192.168.56."))
                continue;

            return s;
        }

        return "127.0.0.1";
    }
    #endregion
}