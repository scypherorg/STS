using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeSync : MonoBehaviour
{
    public SensorBundle[] sensorHistory;
    public Action<SensorBundle> OnCompletedBundle;
    bool SBP_Analysis = false;
    float SBP_Threshold = 0;
    int[] SBP_peakOffset;
    public uint[] sensorClockOffset;
    uint[] lastSensorTime;
    SensorData[] lastSensorData;
    SensorData[] currentBatchData;
    Dictionary<uint, SensorData?[]> incompleteBundles;
    DateTime zero;
    public void Setup(int sensorCount)
    {
        zero = GetTimeDiscreteMS(DateTime.UtcNow, 1000);
        sensorClockOffset = new uint[sensorCount];
        lastSensorTime = new uint[sensorCount];
        lastSensorData = new SensorData[sensorCount];
        currentBatchData = new SensorData[sensorCount];
        for (int i = 0; i < lastSensorData.Length; i++)
        {
            lastSensorData[i] = new(){ID=i, Acceleration = Vector3.zero, Gyro = Vector3.zero};
            currentBatchData[i] = new(){ID=i, Acceleration = Vector3.zero, Gyro = Vector3.zero};
        }
        incompleteBundles = new();
        sensorHistory = new SensorBundle[10];
    }
    public void AddData(uint ts, SensorData data)
    {
        if(sensorClockOffset[data.ID] == 0)
            ZeroSensors(ts);
        if(SBP_Analysis)
            SBP_Analyze(ts, data);
        uint msSinceStart = GetMSfromSensorTS(ts)-sensorClockOffset[data.ID];
        uint batchID = msSinceStart/100;
        uint lastBatchID = lastSensorTime[data.ID]/100;
        if(batchID == lastBatchID)
        {
            float delta = (msSinceStart-lastSensorTime[data.ID])/1000f;
            currentBatchData[data.ID].Acceleration += lastSensorData[data.ID].Acceleration * delta;
            currentBatchData[data.ID].Gyro += lastSensorData[data.ID].Gyro * delta;
            lastSensorData[data.ID] = data;
            lastSensorTime[data.ID] = msSinceStart;
            return;
        }
        if(lastBatchID + 1 != batchID)
        {
            lastSensorTime[data.ID] = msSinceStart;
            currentBatchData[data.ID] = new()
            {
                ID = data.ID,
                Acceleration = new(),
                Gyro = new()
            };
            Debug.LogError("Somethings fcked");
            return;
        }
        //Finish Batch
        float completionDelta = 100 - (lastSensorTime[data.ID]%100);
        completionDelta /= 1000f;
        float initialDelta = msSinceStart%100;
        initialDelta /= 1000f;
        currentBatchData[data.ID].Acceleration += lastSensorData[data.ID].Acceleration * completionDelta;
        currentBatchData[data.ID].Gyro += lastSensorData[data.ID].Gyro * completionDelta;
        AddCompleteBatch(lastBatchID, currentBatchData[data.ID]);
        currentBatchData[data.ID] = new()
        {
            ID = data.ID,
            Acceleration = lastSensorData[data.ID].Acceleration * initialDelta,
            Gyro = lastSensorData[data.ID].Gyro * initialDelta
        };
        lastSensorData[data.ID] = data;
        lastSensorTime[data.ID] = msSinceStart;
    }
    public void AddTimeOffset(int sensorID, int offsetMS)
    {
        sensorClockOffset[sensorID] = (uint)(sensorClockOffset[sensorID] + offsetMS);
        lastSensorTime[sensorID] = (uint)(lastSensorTime[sensorID] - offsetMS);
    }
    public void SyncByPeak(float threshold)
    {
        SBP_Analysis = true;
        SBP_Threshold = threshold;
        SBP_peakOffset = new int[sensorClockOffset.Length];
    }
    void SBP_Analyze(uint ts, SensorData data)
    {
        if(Mathf.Abs(data.Gyro.z) < SBP_Threshold)
            return;
        SBP_peakOffset[data.ID] = (int)GetMSfromSensorTS(ts);
        bool complete = true;
        for (int i = 0; i < SBP_peakOffset.Length; i++)
            complete &= SBP_peakOffset[i] > 0;
        if(!complete)
            return;
        Debug.Log("Internal Timers: " + SBP_peakOffset[0] + "//" + SBP_peakOffset[1]);
        int offset = SBP_peakOffset[1] - SBP_peakOffset[0];
        sensorClockOffset[1] = (uint)(sensorClockOffset[0] + offset);
        Debug.Log("Adjusted Sensor-Offset by " + offset + "ms");
        SBP_Analysis = false;
    }
    void AddCompleteBatch(uint batchID, SensorData batch)
    {
        bool newBatch = !incompleteBundles.TryGetValue(batchID, out SensorData?[] bundle);
        if(newBatch)
            bundle = new SensorData?[currentBatchData.Length];
        bundle[batch.ID] = batch;
        bool complete = true;
        for (int i = 0; i < bundle.Length; i++)
            complete &= bundle[i] != null;
        if(complete)
        {
            incompleteBundles.Remove(batchID);
            SensorData[] data = new SensorData[bundle.Length];
            for (int i = 0; i < bundle.Length; i++)
                data[i] = bundle[i]??throw new Exception("Impossible");
            AddCompleteBundle(new()
            {
                BatchID = batchID,
                timestamp = zero.AddMilliseconds(batchID*100),
                sensorData = data
            });
            return;
        }
        if(newBatch)
            incompleteBundles.Add(batchID, bundle);
        else
            incompleteBundles[batchID] = bundle;
    }
    void AddCompleteBundle(SensorBundle bundle)
    {
        for (int i = 0; i < sensorHistory.Length - 1; i++)
            sensorHistory[i] = sensorHistory[i+1];
        sensorHistory[^1] = bundle;
        OnCompletedBundle?.Invoke(bundle);
    }
    void ZeroSensors(uint ts)
    {
        uint timeSinceStart = (uint)GetTimeDiscreteMS(DateTime.UtcNow, 1).Subtract(zero).TotalMilliseconds;
        for (int i = 0; i < sensorClockOffset.Length; i++)
        {
            sensorClockOffset[i] = GetMSfromSensorTS(ts) - timeSinceStart;
            lastSensorTime[i] = timeSinceStart;
        }
    }
    uint GetMSfromSensorTS(uint ts)
    {
        return ts/1000;
    }
    DateTime GetTimeDiscreteMS(DateTime time, int order)
    {
        return new(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond - time.Millisecond % order);
    }
}