// Receives Polaris tool matrices over UDP (sent by main.py).
// Matrices are already left-handed, in millimetres, one per ROM file (same order).
// Read them from `Matrices` / `Visible` in your own scripts.
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PolarisReceiver : MonoBehaviour
{
    [Serializable] class ToolData { public bool visible; public float[] matrix; }
    [Serializable] class Packet { public ToolData[] tools; }

    public int port = 5005;

    public Matrix4x4[] Matrices { get; private set; } = new Matrix4x4[0];
    public bool[] Visible { get; private set; } = new bool[0];

    UdpClient client;
    Thread thread;
    string latestJson;
    readonly object jsonLock = new object();


    public enum Tool { Origin = 0, Probe = 1}

    public class ToolInfo
    {
        public Tool tool;
        public Matrix4x4 matrix;
        public bool visible;
    }
    

    void OnEnable()
    {
        client = new UdpClient(port);
        thread = new Thread(ReceiveLoop) { IsBackground = true };
        thread.Start();
    }

    void ReceiveLoop()
    {
        var remote = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                string json = Encoding.UTF8.GetString(client.Receive(ref remote));
                lock (jsonLock) latestJson = json;
            }
        }
        catch (Exception) { /* socket closed */ }
    }

    void Update()
    {
        string json;
        lock (jsonLock) { json = latestJson; latestJson = null; }
        if (json == null) return;

        Packet packet = JsonUtility.FromJson<Packet>(json);
        int n = packet.tools.Length;
        var matrices = new Matrix4x4[n];
        var visible = new bool[n];

        for (int i = 0; i < n; i++)
        {
            float[] v = packet.tools[i].matrix;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    matrices[i][r, c] = v[r * 4 + c]; // row-major from Python
            visible[i] = packet.tools[i].visible;
        }

        Matrices = matrices;
        Visible = visible;
    }

    public ToolInfo GetToolInfo(Tool tool)
    {
        int i = (int)tool;
        ToolInfo info = new ToolInfo
        {
            tool = tool,
            matrix = Matrices[i],
            visible = Visible[i]
        };
    
        return info;
    }

    void OnDisable()
    {
        client?.Close();
        thread?.Join(100);
    }
}
