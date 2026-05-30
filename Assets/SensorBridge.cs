using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SensorBridge : MonoBehaviour
{
    public string[] SensorAdresses;
    public bool[] isConnected;
    public TimeSync timeSync;
    bool isRunning = true;
    readonly ConcurrentQueue<(uint, SensorData)> incomingQueue = new();
    void Start()
    {
        isConnected = new bool[SensorAdresses.Length];
        timeSync.Setup(SensorAdresses.Length);
        new Thread(ReceiveData).Start();
        new Thread(InitializeSensors).Start();
    }

    void InitializeSensors()
    {
        Thread.Sleep(1000);
        string scriptPath = Path.Combine(Application.dataPath, "bridge.py");
        for (int i = 0; i < SensorAdresses.Length; i++)
        {
            if(isConnected[i])
                continue;
            System.Diagnostics.ProcessStartInfo start = new()
            {
                FileName = "python",
                Arguments = $"{scriptPath} {SensorAdresses[i]}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            Debug.Log("Starting Python Bridge for " + SensorAdresses[i]);
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(start);
            process.ErrorDataReceived += (sendingProcess, errLine) => {if(errLine.Data.StartsWith("ERROR")){Debug.LogError(errLine.Data);}else{Debug.Log(errLine.Data);}};
            process.BeginErrorReadLine();
            process.Exited += (proc, code) => {Debug.LogError(proc.ToString() + " exited with code " + code.ToString());};
            int counter = 300;
            while(!isConnected[i] && counter > 0)
            {
                Thread.Sleep(100);
                counter--;
            }
            if(isConnected[i])
                Debug.Log("Started Stream for " + SensorAdresses[i]);
            if(counter <= 0)
                Debug.LogError("Connection for " + SensorAdresses[i] + " timed out!");
        }
    }
    void ReceiveData()
    {
        UdpClient udp = new(5001);
        IPEndPoint ep = new(IPAddress.Any, 0);
        while(isRunning)
        {
            try
            {
                DotData data = JsonUtility.FromJson<DotData>(Encoding.UTF8.GetString(udp.Receive(ref ep)));
                int id = GetSensorID(data.adr);
                if(id == -1)
                    throw new InvalidDataException($"Unlisted Device! MAC: {data.adr}");
                isConnected[id] = true;
                incomingQueue.Enqueue((data.ts, new SensorData(){
                   ID = id,
                   Acceleration = data.acc,
                   Gyro = data.gyro 
                }));
            }
            catch 
            {
            }
        }
        udp.Close();
        udp.Dispose();
    }
    void Update()
    {
        while(incomingQueue.TryDequeue(out (uint, SensorData) data))
            timeSync.AddData(data.Item1, data.Item2);
    }
    void OnApplicationQuit()
    {
        isRunning = false;
    }
    int GetSensorID(string sensorAdress)
    {
        for (int i = 0; i < SensorAdresses.Length; i++)
            if(sensorAdress == SensorAdresses[i])
                return i;
        return -1;
    }
}