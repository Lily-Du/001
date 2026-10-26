using UnityEngine;

namespace DragonCourt
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CourtPlayer : MonoBehaviour
    {
        public Camera view;
        public Transform body;
        public bool inputEnabled = true;
        public bool crouched { get; private set; }
        CharacterController motor;
        float yaw, pitch = 16f, verticalSpeed;
        Vector3 previousMouse;
        bool mouseReady;

        void Awake()
        {
            motor = GetComponent<CharacterController>();
            previousMouse = Input.mousePosition;
        }

        void Update()
        {
            if (!inputEnabled) return;
            // Hold RMB to orbit. No Input Manager axes or external packages required.
            Vector3 mouse = Input.mousePosition;
            if (Input.GetMouseButton(1) && mouseReady)
            {
                Vector3 delta = mouse - previousMouse;
                yaw += delta.x * .22f;
                pitch = Mathf.Clamp(pitch - delta.y * .17f, -20f, 65f);
            }
            previousMouse = mouse;
            mouseReady = true;
            if (Input.GetKey(KeyCode.LeftArrow)) yaw -= 85f * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow)) yaw += 85f * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow)) pitch -= 45f * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow)) pitch += 45f * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -20f, 65f);

            bool wantCrouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
            if (wantCrouch) crouched = true;
            else if (crouched)
            {
                // Do not stand up through a low ceiling.
                crouched = Physics.SphereCast(transform.position + Vector3.up * .65f,
                    .28f, Vector3.up, out _, 1.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            }
            motor.height = crouched ? 1.05f : 1.8f;
            motor.center = Vector3.up * (motor.height * .5f);
            if (body != null)
            {
                body.localPosition = Vector3.up * motor.height * .5f;
                body.localScale = new Vector3(.55f, motor.height * .5f, .55f);
            }
            float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Vector3 motion = Quaternion.Euler(0, yaw, 0) * Vector3.ClampMagnitude(new Vector3(x, 0, z), 1f);
            float speed = crouched ? 2.1f : Input.GetKey(KeyCode.LeftShift) ? 6.2f : 3.6f;
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            verticalSpeed -= 22f * Time.deltaTime;
            motor.Move((motion * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (motion.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(motion), Time.deltaTime * 12f);
        }

        void LateUpdate()
        {
            if (!inputEnabled || view == null) return;
            Vector3 focus = transform.position + Vector3.up * (crouched ? .9f : 1.5f);
            Vector3 offset = Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -5.5f);
            float distance = offset.magnitude;
            // The player is on Ignore Raycast; scenery still blocks the camera.
            if (Physics.SphereCast(focus, .18f, offset.normalized, out RaycastHit hit,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(.25f, hit.distance - .12f);
            view.transform.position = focus + offset.normalized * distance;
            view.transform.LookAt(focus);
        }

        public void Warp(Vector3 position)
        {
            motor.enabled = false;
            transform.position = position;
            verticalSpeed = 0;
            motor.enabled = true;
        }
    }
}
