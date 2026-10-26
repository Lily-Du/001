using UnityEngine;

namespace DragonCourt
{
    public sealed class CourtDragon : MonoBehaviour
    {
        public CourtPlayer player;
        public CourtFlow flow;
        public Transform head, leftWing, rightWing;
        public enum Mode { Patrol, Alert, Search, Breath }
        public Mode mode { get; private set; }
        public float suspicion { get; private set; }
        public bool visible { get; private set; }
        public bool debugView;
        public bool riding;
        public float patrolSeconds = 56f;
        float phase = -.9f, tick, searchTime, breathTime, lureTime;
        Vector3 lastSeen, breathTarget, lureTarget;
        GameObject warning;

        void Start()
        {
            warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warning.name = "Dragon breath warning (7m radius)";
            Destroy(warning.GetComponent<Collider>());
            warning.transform.localScale = new Vector3(14f, .035f, 14f);
            warning.GetComponent<Renderer>().material.color = new Color(1f, .25f, .06f);
            warning.SetActive(false);
        }

        void Update()
        {
            AnimateWings();
            if (riding || player == null || flow == null) return;
            if (Input.GetKeyDown(KeyCode.F1)) debugView = !debugView;
            float dt = Time.deltaTime;
            if (lureTime > 0) lureTime -= dt;
            if (mode == Mode.Breath)
            {
                breathTime -= dt;
                TurnToward(breathTarget - transform.position, dt);
                if (breathTime <= 0)
                {
                    warning.SetActive(false);
                    Vector3 d = player.transform.position - breathTarget;
                    d.y = 0;
                    bool hit = d.sqrMagnitude < 49f && HasLineOfSight();
                    mode = Mode.Search;
                    searchTime = 12f;
                    suspicion = .35f;
                    if (hit) flow.Respawn("Dragon breath! Observe from cover before crossing.");
                }
                return;
            }

            Vector3 destination;
            if (mode == Mode.Search || mode == Mode.Alert)
            {
                searchTime -= dt;
                float orbit = Time.time * .32f;
                destination = lastSeen + new Vector3(Mathf.Cos(orbit) * 18f, 25f, Mathf.Sin(orbit) * 18f);
                if (searchTime <= 0 && suspicion < .1f) mode = Mode.Patrol;
            }
            else if (lureTime > 0)
                destination = lureTarget + new Vector3(0, 25, 12);
            else
            {
                phase += dt * Mathf.PI * 2f / patrolSeconds;
                destination = new Vector3(Mathf.Cos(phase) * 46f, 29f + Mathf.Sin(phase * 2f) * 3f,
                    94f + Mathf.Sin(phase) * 48f);
            }
            Vector3 direction = destination - transform.position;
            transform.position = Vector3.MoveTowards(transform.position, destination, dt * 15f);
            TurnToward(direction, dt);

            tick -= dt;
            if (tick > 0) return;
            tick = .1f;
            visible = !flow.inSanctuary && CanSeePlayer();
            suspicion = Mathf.Clamp01(suspicion + (visible ? (player.crouched ? .024f : .04f) : -.025f));
            if (visible)
            {
                lastSeen = player.transform.position;
                searchTime = 12f;
                if (suspicion >= .35f && mode == Mode.Patrol)
                {
                    mode = Mode.Alert;
                    flow.Notify("The dragon noticed movement. Break line of sight!", 3f);
                }
            }
            if (suspicion >= 1f)
            {
                mode = Mode.Breath;
                breathTime = 2.8f;
                breathTarget = lastSeen;
                warning.transform.position = breathTarget + Vector3.up * .06f;
                warning.SetActive(true);
                flow.Notify("BREATH INCOMING - move away from the orange circle!", 3f);
            }
            else if (!visible && mode == Mode.Alert) mode = Mode.Search;
        }

        void TurnToward(Vector3 direction, float dt)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 1.8f);
        }

        bool CanSeePlayer()
        {
            Vector3 target = player.transform.position + Vector3.up * (player.crouched ? .65f : 1.2f);
            Vector3 eye = head != null ? head.position : transform.position;
            Vector3 delta = target - eye;
            Vector3 look = (transform.forward * .5f + Vector3.down).normalized;
            if (delta.magnitude > 82f || Vector3.Dot(look, delta.normalized) < .42f) return false;
            return HasLineOfSight();
        }

        bool HasLineOfSight()
        {
            Vector3 eye = head != null ? head.position : transform.position;
            float h = player.crouched ? .65f : 1.2f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 target = player.transform.position + Vector3.up * (h + i * .3f);
                Vector3 delta = target - eye;
                if (!Physics.Raycast(eye, delta.normalized, delta.magnitude,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
        }

        public void Lure(Vector3 point)
        {
            if (mode != Mode.Patrol) return;
            lureTarget = point;
            lureTime = 11f;
        }

        public void ResetDetection()
        {
            suspicion = 0;
            visible = false;
            mode = Mode.Patrol;
            lureTime = 0;
            if (warning != null) warning.SetActive(false);
        }

        void AnimateWings()
        {
            float flap = Mathf.Sin(Time.time * 2.1f) * 13f;
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0, 0, -flap);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0, 0, flap);
        }

        void OnDrawGizmos()
        {
            if (!debugView || player == null) return;
            Gizmos.color = visible ? Color.red : Color.green;
            Gizmos.DrawLine(head != null ? head.position : transform.position,
                player.transform.position + Vector3.up);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSeen, 2f);
        }
    }
}
