using UnityEngine;

namespace DragonCourt
{
    public sealed class CourtFlow : MonoBehaviour
    {
        public CourtPlayer player;
        public CourtDragon dragon;
        public Transform sword, bell, gate;
        public bool inSanctuary { get; private set; }
        Vector3 checkpoint = new Vector3(0, .1f, 4);
        int checkpointIndex, failures;
        bool gateOpened, loreRead, riding, finished, debug;
        float messageUntil, bellReady, elapsed, rideTime;
        string message = "";
        readonly Vector3[] flight = {
            new Vector3(0, 12, 166), new Vector3(0, 30, 147),
            new Vector3(-18, 45, 118), new Vector3(-28, 53, 78),
            new Vector3(0, 58, 34), new Vector3(25, 65, -4), new Vector3(0, 85, -45)
        };

        void Start() { Notify("Reach the sword in the northern palace. Roofs block the dragon's sight.", 8f); }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2)) debug = !debug;
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
            if (riding) { UpdateRide(); return; }
            elapsed += Time.deltaTime;
            Vector3 p = player.transform.position;
            inSanctuary = p.z >= 157f;
            if (p.y < -14f) { Respawn("You fell. Returning to the last safe checkpoint."); return; }
            if (Input.GetKeyDown(KeyCode.R)) { Respawn("Restarted at the last checkpoint."); return; }
            if (p.z > 19f && checkpointIndex == 0)
                SetCheckpoint(1, new Vector3(0, .1f, 20));
            if (p.z > 101f && checkpointIndex < 2)
                SetCheckpoint(2, new Vector3(p.x < 0 ? -30 : 30, .1f, 103));
            if (p.z > 136f && checkpointIndex < 3)
                SetCheckpoint(3, new Vector3(0, .1f, 138));

            if (!Input.GetKeyDown(KeyCode.E)) return;
            if (Near(sword.position, 3.2f)) BeginRide();
            else if (Near(bell.position, 3f) && Time.time >= bellReady)
            {
                if (dragon.mode != CourtDragon.Mode.Patrol)
                { Notify("The dragon is searching. Wait until it resumes patrol.", 3f); return; }
                bellReady = Time.time + 35f;
                dragon.Lure(bell.position + Vector3.right * 14f);
                Notify("Bell rung. The dragon investigates the east side for 11 seconds.", 5f);
            }
            else if (!gateOpened && Near(new Vector3(-30, 0, 95), 4f))
            {
                gateOpened = true;
                gate.gameObject.SetActive(false);
                Notify("Western gate opened. This passage remains open after a retry.", 5f);
            }
            else if (!loreRead && Near(new Vector3(-35, 0, 76), 4f))
            {
                loreRead = true;
                Notify("The mural shows a knight beside a dragon: a pact between equals.", 8f);
            }
        }

        bool Near(Vector3 target, float radius) { return Vector3.Distance(player.transform.position, target) < radius; }
        void SetCheckpoint(int index, Vector3 position)
        {
            checkpointIndex = index;
            checkpoint = position;
            Notify("Checkpoint reached. R retries here.", 3f);
        }
        public void Notify(string text, float seconds) { message = text; messageUntil = Time.time + seconds; }
        public void Respawn(string reason)
        {
            if (riding) return;
            failures++;
            player.Warp(checkpoint);
            dragon.ResetDetection();
            Notify(reason, 5f);
        }

        void BeginRide()
        {
            riding = true;
            dragon.ResetDetection();
            dragon.riding = true;
            sword.gameObject.SetActive(false);
            player.inputEnabled = false;
            player.GetComponent<CharacterController>().enabled = false;
            // First approach the dais; boarding happens after the approach.
            rideStart = dragon.transform.position;
            rideRotation = dragon.transform.rotation;
            Notify("The false bond is broken. The dragon accepts a new pact.", 7f);
        }
        Vector3 rideStart;
        Quaternion rideRotation;

        void UpdateRide()
        {
            rideTime += Time.deltaTime;
            if (rideTime < 6f)
            {
                float t = Mathf.SmoothStep(0, 1, rideTime / 6f);
                dragon.transform.position = Vector3.Lerp(rideStart, flight[0], t);
                dragon.transform.rotation = Quaternion.Slerp(rideRotation, Quaternion.Euler(0, 180, 0), t);
                Vector3 eye = new Vector3(10, 6, 160);
                player.view.transform.position = eye;
                player.view.transform.LookAt(dragon.transform.position);
                return;
            }
            float normalized = Mathf.Clamp01((rideTime - 6f) / 48f) * (flight.Length - 1);
            int segment = Mathf.Min(Mathf.FloorToInt(normalized), flight.Length - 2);
            float u = normalized - segment;
            Vector3 point = Vector3.Lerp(flight[segment], flight[segment + 1], u);
            Vector3 forward = (flight[segment + 1] - flight[segment]).normalized;
            dragon.transform.position = point;
            dragon.transform.rotation = Quaternion.Slerp(dragon.transform.rotation,
                Quaternion.LookRotation(forward), Time.deltaTime * 2f);
            player.transform.position = dragon.transform.TransformPoint(new Vector3(0, 2.3f, 2));
            player.transform.rotation = dragon.transform.rotation;
            // Q/E allow looking to either side on the authored flight path.
            float side = Input.GetKey(KeyCode.Q) ? -60 : Input.GetKey(KeyCode.E) ? 60 : 0;
            Vector3 offset = Quaternion.AngleAxis(side, Vector3.up) * new Vector3(0, 5, -15);
            Vector3 cameraTarget = dragon.transform.TransformPoint(offset);
            player.view.transform.position = Vector3.Lerp(player.view.transform.position, cameraTarget, Time.deltaTime * 3f);
            player.view.transform.LookAt(point + Vector3.up * 2);
            finished = rideTime >= 54f;
        }

        void OnGUI()
        {
            if (player == null || dragon == null) return;
            float scale = Mathf.Max(.7f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            GUI.Box(new Rect(18, 18, 620, 115), "DRAGON COURT | playable level blockout");
            GUI.Label(new Rect(32, 45, 590, 24), "WASD move | RMB drag / arrows look | Shift sprint | Ctrl crouch");
            GUI.Label(new Rect(32, 70, 590, 24), "E interact | R checkpoint | F1 perception gizmos | F2 debug map");
            GUI.Label(new Rect(32, 96, 590, 24), riding ? "Pact restored. Q/E: look sideways during flight." : "Goal: reach the northern sanctuary and claim the sword.");
            if (!riding)
            {
                GUI.Box(new Rect(18, 145, 290, 75), "Dragon: " + dragon.mode);
                GUI.Box(new Rect(33, 176, 255, 22), "");
                GUI.color = Color.Lerp(Color.yellow, Color.red, dragon.suspicion);
                GUI.DrawTexture(new Rect(35, 178, 251 * dragon.suspicion, 18), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(320, 170, 420, 26), inSanctuary ? "SANCTUARY: safe" : dragon.visible ? "EXPOSED: find solid cover" : "Not currently seen");
                string prompt = "";
                if (Near(sword.position, 3.2f)) prompt = "E - Draw the sword and restore the pact";
                else if (Near(bell.position, 3f)) prompt = Time.time >= bellReady ? "E - Ring bell (patrol only)" : "Bell cooling down";
                else if (!gateOpened && Near(new Vector3(-30, 0, 95), 4f)) prompt = "E - Open west gate";
                else if (!loreRead && Near(new Vector3(-35, 0, 76), 4f)) prompt = "E - Examine the old pact mural";
                GUI.Box(new Rect(width / 2 - 240, Screen.height / scale - 105, 480, 38), prompt);
            }
            if (Time.time < messageUntil)
                GUI.Box(new Rect(width / 2 - 360, Screen.height / scale - 60, 720, 42), message);
            if (finished)
                GUI.Box(new Rect(width / 2 - 280, 260, 560, 100), "PACT RESTORED\nThe courtyard is now beneath your wings.\nExplore time: " + Mathf.FloorToInt(elapsed) + "s | Retries: " + failures);
            if (debug && !riding) DrawMap(width);
        }

        void DrawMap(float width)
        {
            Rect panel = new Rect(width - 235, 18, 215, 370);
            GUI.Box(panel, "DEBUG TOPOLOGY (F2)");
            GUI.Label(new Rect(panel.x + 12, panel.y + 25, 195, 22), "N: sword / sanctuary");
            DrawMapRect(panel, -20, 156, 40, 22, Color.yellow);
            DrawMapRect(panel, -6, 136, 12, 20, Color.gray);
            DrawMapRect(panel, -38, 100, 76, 36, Color.gray);
            DrawMapRect(panel, -37, 52, 14, 48, Color.gray);
            DrawMapRect(panel, 23, 52, 14, 48, Color.cyan);
            DrawMapRect(panel, -38, 22, 76, 30, Color.gray);
            DrawMapRect(panel, -6, 0, 12, 22, Color.gray);
            Vector3 p = player.transform.position;
            DrawMapRect(panel, p.x - 1.5f, p.z - 1.5f, 3, 3, Color.green);
            Vector3 d = dragon.transform.position;
            DrawMapRect(panel, d.x - 2, d.z - 2, 4, 4, Color.red);
        }
        void DrawMapRect(Rect panel, float x, float z, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(panel.x + 107 + x * 1.8f, panel.y + 345 - (z + h) * 1.65f,
                w * 1.8f, h * 1.65f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
