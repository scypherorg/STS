using UnityEngine;
using UnityEngine.InputSystem;

public class Vibrator : MonoBehaviour
{
    public TimeSync timeSync;
    public float RatioThreshold;
    public float ValueThreshold;
    public int TimeBuffer100MS;
    public  float ratio;
    void Start()
    {
        timeSync.OnCompletedBundle += ReceiveData;
    }
    void ReceiveData(SensorBundle bundle)
    {
        float s0Peak = 0;
        float s1Peak = 0;
        int peakIndex = -1;
        for (int i = 0; i < timeSync.sensorHistory.Length; i++)
        {
            if(Mathf.Abs(timeSync.sensorHistory[i].sensorData[0].Gyro.y) > s0Peak)
            {
                s0Peak = Mathf.Abs(timeSync.sensorHistory[i].sensorData[0].Gyro.y);
                peakIndex = i;
            }
            if(Mathf.Abs(timeSync.sensorHistory[i].sensorData[1].Gyro.y) > s1Peak)
            {
                s1Peak = Mathf.Abs(timeSync.sensorHistory[i].sensorData[1].Gyro.y);
                peakIndex = i;
            }
        }
        if(peakIndex + TimeBuffer100MS > timeSync.sensorHistory.Length || s0Peak < ValueThreshold || s1Peak < ValueThreshold)
            return;
        ratio = s1Peak/s0Peak;
        if(ratio < RatioThreshold)
            return;
        Debug.Log($"Triggering Virbation ({ratio}/{RatioThreshold})");
        Gamepad.current?.SetMotorSpeeds(1, 1);
    }
}
