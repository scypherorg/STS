using UnityEngine;

public class XDOTManager : MonoBehaviour
{
    public LineRenderer line0;
    public LineRenderer line1;
    public Vector3[] history0;
    public Vector3[] history1;
    public TimeSync timeSync;
    public bool Sync;
    public float maxDiff = 0;
    public float maxRatio = 0;
    void Start()
    {
        history0 = new Vector3[100];
        history1 = new Vector3[100];
        timeSync.OnCompletedBundle += OnSensorBundle;
    }
    void Update()
    {
        if(Sync)
        {
            Sync = false;
            Debug.Log("Running Peak Sync...");
            timeSync.SyncByPeak(100);
        }
    }
    public void OnSensorBundle(SensorBundle bundle)
    {
        maxDiff = 0;
        float max0 = 0;
        float max1 = 0;
        for (int i = 0; i < history0.Length - 1; i++)
        {
            history0[i] = history0[i+1];
            history0[i].x = i;
            history1[i] = history1[i+1];
            history1[i].x = i;
            if(Mathf.Abs(history0[i].y-history1[i].y) > maxDiff)
                maxDiff = Mathf.Abs(history0[i].y-history1[i].y);
            if(Mathf.Abs(history0[i].y) > max0)
                max0 = history0[i].y;
            if(Mathf.Abs(history1[i].y) > max1)
                max1 = history1[i].y;
        }
        history0[^1] = new Vector3(history0.Length, bundle.sensorData[0].Gyro.y);
        history1[^1] = new Vector3(history1.Length, bundle.sensorData[1].Gyro.y);
        if(Mathf.Abs(history0[^1].y) > max0)
            max0 = history0[^1].y;
        if(Mathf.Abs(history1[^1].y) > max1)
            max1 = history1[^1].y;
        if(max0 > 50 && max1 > 50)
            maxRatio = max1/max0;
        else
            maxRatio = 0;
        line0.SetPositions(history0);
        line1.SetPositions(history1);
    }
}
