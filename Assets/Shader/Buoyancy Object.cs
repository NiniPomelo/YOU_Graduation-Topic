using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyancyObject : MonoBehaviour
{
    public Vector3 direction = new Vector3(1, 0, 0);
    public float frequency = 1f;
    public float speed = 1f;
    public float peek = 0.5f;
    public float waterHeight = 0f;

    public float floatingPower = 30f;
    public float velocityDamping = 5f;

    public float waterDrag = 3f;
    public float airDrag = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 dir = direction.normalized;

        float phase = Vector3.Dot(transform.position, dir) * frequency
                    + Time.time * speed;

        float waveHeight = Mathf.Sin(phase) * peek;
        float waterLevel = waterHeight + waveHeight;

        float displacement = waterLevel - transform.position.y;

        float springForce = displacement * floatingPower;
        float dampingForce = -rb.linearVelocity.y * velocityDamping;

        float totalForce = springForce + dampingForce;

        rb.AddForce(Vector3.up * totalForce, ForceMode.Force);

        if (transform.position.y < waterLevel)
        {
            rb.linearDamping = waterDrag;
        }
        else
        {
            rb.linearDamping = airDrag;
        }

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(-horizontalVel * 0.5f, ForceMode.Force);
    }
}