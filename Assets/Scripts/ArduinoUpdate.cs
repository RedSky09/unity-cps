using UnityEngine;
using System.IO.Ports;
using System.Globalization;

public class ArduinoUpdate : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "COM5";
    public int baudRate = 115200;

    [Header("Mapping Settings")]
    public float scale = 0.5f;

    private SerialPort sp;
    private Rigidbody rb;
    private Vector3 lastVelocity;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        sp = new SerialPort(portName, baudRate);
        sp.ReadTimeout = 10;
        sp.NewLine = "\n";
        sp.DtrEnable = true;
        sp.RtsEnable = true;

        try
        {
            sp.Open();
            sp.DiscardInBuffer();
            Debug.Log("Serial port opened: " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to open serial port " + portName + " : " + e.Message);
        }

        lastPosition = rb.position;
    }

    void OnDestroy()
    {
        if (sp != null)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Close();
                    Debug.Log("Serial port closed: " + portName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Error closing serial port: " + e.Message);
            }
        }
    }

    void FixedUpdate()
    {
        if (sp == null || !sp.IsOpen) return;

        string latestLine = null;

        try
        {
            // baca semua data dan simpan yang terakhir
            while (sp.BytesToRead > 0)
                latestLine = sp.ReadLine();
        }
        catch (System.TimeoutException)
        {
            // tidak ada baris lengkap
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Serial read error: " + e.Message);
        }

        // tidak ada data baru
        if (string.IsNullOrEmpty(latestLine))
            return;

        // Format: "x:0.1234 y:0.5678"
        string[] parts = latestLine.Split(' ');
        if (parts.Length != 2)
            return;

        try
        {
            float xVal = float.Parse(parts[0].Substring(2), CultureInfo.InvariantCulture);
            float yVal = float.Parse(parts[1].Substring(2), CultureInfo.InvariantCulture);

            // target posisi berdasarkan joystick
            Vector3 target = new Vector3(xVal * scale, rb.position.y, yVal * scale);

            // ANTI-Teleport smoothing:
            float maxStep = 0.1f;   // gerakan maksimal per frame
            Vector3 direction = target - rb.position;

            // jika target terlalu jauh, buat langkah kecil
            if (direction.magnitude > maxStep)
                direction = direction.normalized * maxStep;

            rb.MovePosition(rb.position + direction); // inilah inti solusi 4
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Parse error for line '{latestLine}' : {e.Message}");
        }

        // hitung velocity manual
        lastVelocity = (rb.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = rb.position;
    }
}
