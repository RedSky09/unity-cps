#include <math.h>

struct Body {
  float x, y;    // posisi
  float vx, vy;  // kecepatan
  float fx, fy;  // gaya
  float mass;    // massa
};

Body obj;

const float F_MAX   = 5.0f;          // gaya maksimum (N) dari analog
const float C_DRAG  = 2.0f;          // koefisien drag (N per (m/s))
const unsigned long DT_MS = 10;      // 10 ms = 0.01 s

unsigned long lastUpdate = 0;
float timeSec = 0.0f;

// PIN ANALOG JOYSTICK
const int PIN_AX = A0;   // sumbu X (kiri-kanan)
const int PIN_AY = A1;   // sumbu Y (atas-bawah)

// Deadzone
const float DEADZONE = 0.15f;       // 15%

void setup() {
  Serial.begin(115200);

  obj.x = 0.0f;
  obj.y = 0.0f;
  obj.vx = 0.0f;
  obj.vy = 0.0f;
  obj.fx = 0.0f;
  obj.fy = 0.0f;
  obj.mass = 0.5f;   // 0.5 kg

  lastUpdate = millis();

  Serial.println("# 2D Euler Simulation - Analog + Drag (lebih responsif)");
  Serial.println("# Joystick / pot:");
  Serial.println("#   A0: gaya X (kiri-kanan)");
  Serial.println("#   A1: gaya Y (bawah-atas)");
  Serial.println("# r: reset posisi & kecepatan & gaya");
  Serial.println("# Output: x:<val> y:<val>");
  Serial.println();
}

void handleSerialInput() {
  while (Serial.available() > 0) {
    char c = Serial.read();

    switch (c) {
      case 'r':
      case 'R':
        obj.fx = 0.0f;
        obj.fy = 0.0f;
        obj.vx = 0.0f;
        obj.vy = 0.0f;
        obj.x = 0.0f;
        obj.y = 0.0f;
        timeSec = 0.0f;
        break;
      default:
        break;
    }
  }
}

// Baca joystick, set fx, fy, dan kembalikan apakah input aktif
bool update_force_from_analog() {
  int rawX = analogRead(PIN_AX);
  int rawY = analogRead(PIN_AY);

  float normX = (rawX - 512) / 512.0f;
  float normY = (rawY - 512) / 512.0f;

  bool active = false;

  if (fabs(normX) < DEADZONE) normX = 0.0f;
  else active = true;

  if (fabs(normY) < DEADZONE) normY = 0.0f;
  else active = true;

  if (!active) {
    obj.fx = 0.0f;
    obj.fy = 0.0f;
    return false;
  }

  obj.fx = normX * F_MAX;
  obj.fy = -normY * F_MAX;   // balik Y supaya atas positif

  return true;
}

void update_physics(Body *b, float dt) {
  // Gaya drag melawan kecepatan
  float F_drag_x = -C_DRAG * b->vx;
  float F_drag_y = -C_DRAG * b->vy;

  // Total gaya
  float Fx_total = b->fx + F_drag_x;
  float Fy_total = b->fy + F_drag_y;

  // Percepatan
  float ax = Fx_total / b->mass;
  float ay = Fy_total / b->mass;

  // Update kecepatan
  b->vx += ax * dt;
  b->vy += ay * dt;

  // Update posisi
  b->x  += b->vx * dt;
  b->y  += b->vy * dt;
}

void loop() {
  handleSerialInput();

  bool active = update_force_from_analog();

  unsigned long now = millis();
  if (now - lastUpdate >= DT_MS) {
    float dt = (now - lastUpdate) / 1000.0f;
    lastUpdate = now; 

    if (active) {
      update_physics(&obj, dt);
    } else {
      // Kalaun analog di tengah, tetap ada drag: kecepatan akan cepat turun ke 0
      update_physics(&obj, dt);
      // Kalau mau berhenti super-cepat, boleh tambahkan:
      if (fabs(obj.vx) < 0.01f) obj.vx = 0.0f;
      if (fabs(obj.vy) < 0.01f) obj.vy = 0.0f;
    }

    timeSec += dt;

    Serial.print("x:");
    Serial.print(obj.x, 4);
    Serial.print(" y:");
    Serial.println(obj.y, 4);
  }
}
