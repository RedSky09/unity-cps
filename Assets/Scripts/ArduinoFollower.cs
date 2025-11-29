using UnityEngine;
using System.IO.Ports;

public class ArduinoControlledBall : MonoBehaviour
{
    public string portName = "COM5";
    public int baudRate = 115200;
    public float scale = 0.5f;

    private SerialPort sp;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        sp = new SerialPort(portName, baudRate);
        sp.ReadTimeout = 50;
        sp.Open();
    }

    void FixedUpdate()
    {
        if (!sp.IsOpen) return;

        try
        {
            string data = sp.ReadLine();
            string[] parts = data.Split(' ');

            if (parts.Length != 2) return;

            float xVal = float.Parse(parts[0].Substring(2));
            float yVal = float.Parse(parts[1].Substring(2));

            Vector3 target = new Vector3(xVal * scale, rb.position.y, yVal * scale);
            rb.MovePosition(target);
        }
        catch { }
    }
}