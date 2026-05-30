using System;
using UnityEngine;

public struct SensorBundle
{
    public uint BatchID;
    public DateTime timestamp;
    public SensorData[] sensorData;
}
public struct SensorData
{
    public int ID;
    public Vector3 Acceleration;
    public Vector3 Gyro;
}
[Serializable]
public class DotData {
    public string adr;
    public uint ts;
    public Vector3 acc;
    public Vector3 gyro;
}