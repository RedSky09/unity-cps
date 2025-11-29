using UnityEngine;
using System.IO.Ports;

public class ArduinoController : MonoBehaviour
{
    SerialPort sp = new SerialPort("COM5", 115200);
    public float speed = 5f;
    public float jumpForce = 7f;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sp.Open();
        sp.ReadTimeout = 10;
    }

    void Update()
    {
        if (sp.IsOpen)
        {
            try
            {
                string data = sp.ReadLine();
                string[] values = data.Split(',');

                if (values.Length == 3)
                {
                    int x = int.Parse(values[0]);
                    int y = int.Parse(values[1]);
                    int sw = int.Parse(values[2]);

                    // Convert analog 0–1023 ? -1 to 1
                    float axisX = (x - 512) / 512f;
                    float axisY = (y - 512) / 512f;

                    // Gerakkan bola
                    Vector3 movement = new Vector3(axisX, 0, axisY);
                    rb.AddForce(movement * speed);

                    // Loncat jika tombol joystick ditekan (sw == 0)
                    if (sw == 0)
                    {
                        if (IsGrounded())
                        {
                            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                        }
                    }
                }
            }
            catch { }
        }
    }

    // Cek agar melompat hanya saat menyentuh tanah
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}
