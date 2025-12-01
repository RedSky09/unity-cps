using UnityEngine;
using System.IO.Ports;
using System.Globalization;

public class ArduinoUpdate : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "COM5";
    public int baudRate = 115200;

    [Header("Jump & Ground Settings")]
    public float groundY = -999f;        // kalau -999, akan diisi otomatis dari posisi awal bola
    public float jumpVelocity = 6f;      // kecepatan awal lompat
    public float groundRestitution = 0.8f; // seberapa mantul ketika kena ground
    public float groundEpsilon = 0.05f;  // toleransi cek ground

    private SerialPort sp;
    private Rigidbody rb;

    private bool lastJumpPressed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // opsional biar nggak muter2 aneh

        // jika groundY belum di-set di Inspector, pakai posisi awal bola
        if (Mathf.Approximately(groundY, -999f))
        {
            groundY = rb.position.y;
        }

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
    }

    void OnDestroy()
    {
        if (sp != null)
        {
            try
            {
                if (sp.IsOpen)
                    sp.Close();
            }
            catch { }
        }
    }

    void FixedUpdate()
    {
        if (sp == null || !sp.IsOpen) return;

        string latestLine = null;

        try
        {
            while (sp.BytesToRead > 0)
            {
                latestLine = sp.ReadLine();
            }
        }
        catch
        {
            // abaikan error kecil (timeout dsb)
        }

        if (string.IsNullOrEmpty(latestLine))
            return;

        // Format dari Arduino:
        // "x:12.3456 y:-3.2100 j:0" / "x:.. y:.. j:1"
        string[] parts = latestLine.Split(' ');
        if (parts.Length < 2)
            return;

        float xVal, yVal;
        bool jumpPressed = false;

        try
        {
            xVal = float.Parse(parts[0].Substring(2), CultureInfo.InvariantCulture);
            yVal = float.Parse(parts[1].Substring(2), CultureInfo.InvariantCulture);

            if (parts.Length >= 3 && parts[2].StartsWith("j:"))
            {
                int j = int.Parse(parts[2].Substring(2), CultureInfo.InvariantCulture);
                jumpPressed = (j == 1);
            }
        }
        catch
        {
            // gagal parse → lewati frame
            return;
        }

        // --- Update posisi horizontal dari Arduino (XZ) ---
        Vector3 target = new Vector3(xVal, rb.position.y, yVal);
        rb.MovePosition(target);

        // --- Cek apakah bola sedang di ground ---
        bool isGrounded =
            Mathf.Abs(rb.position.y - groundY) <= groundEpsilon &&
            rb.linearVelocity.y <= 0.05f;

        // --- Trigger jump pada edge (0 -> 1) dan saat grounded ---
        if (jumpPressed && !lastJumpPressed && isGrounded)
        {
            Vector3 v = rb.linearVelocity;
            v.y = jumpVelocity;      // lompat ke atas
            rb.linearVelocity = v;
        }

        lastJumpPressed = jumpPressed;

        // --- Pantulan manual saat menyentuh ground setelah lompat ---
        // kondisi: mendekati ground dari atas dan sedang turun (vy < 0)
        if (rb.position.y <= groundY + groundEpsilon && rb.linearVelocity.y < -0.01f)
        {
            // clamp posisi persis di ground
            Vector3 p = rb.position;
            p.y = groundY;
            rb.position = p;

            // pantulkan velocity Y
            Vector3 v = rb.linearVelocity;
            v.y = -v.y * groundRestitution;
            rb.linearVelocity = v;
        }
    }
}
