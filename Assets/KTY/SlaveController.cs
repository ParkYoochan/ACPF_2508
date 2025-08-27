using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SlaveController : MonoBehaviour
{
    [Header("Follow Target (Master)")]
    public Transform target;                 // Master_Sphere Transform�� �巡���ؼ� �Ҵ�

    [Header("Motion Limits")]
    public float maxSpeed = 8f;              // ���� �ִ� �ӵ� (m/s)
    public float maxAcceleration = 30f;      // �ִ� ���ӵ� (m/s^2)

    [Header("Arrive Tuning")]
    public float slowRadius = 2.0f;          // �� �Ÿ����� ���� ����
    public float arriveTolerance = 0.05f;    // �� ���̸� ���� ó��
    public float timeToTarget = 0.1f;        // ���ϴ� �ӵ��� �����ϴ� �� �ɸ��� �ð� (PD�� D ����)

    [Header("Plane Lock")]
    public bool freezeY = true;              // ����鸸 �̵�
    public bool freezeRotation = true;       // ȸ�� ����

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;

        // ȸ��/���� ���� �ɼ�
        if (freezeRotation) rb.constraints |= RigidbodyConstraints.FreezeRotation;
        if (freezeY) rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void FixedUpdate()
    {
        if (!target) return;

        // ��ǥ������ ����(����� ���� ����)
        Vector3 toTarget = target.position - transform.position;
        if (freezeY) toTarget.y = 0f;

        float distance = toTarget.magnitude;

        // ���� ó��: �̼� ����� �ܿ� �ӵ� ���� ���̱�
        if (distance < arriveTolerance)
        {
            Vector3 v = rb.velocity;
            if (freezeY) v.y = 0f;
            rb.velocity = Vector3.Lerp(v, Vector3.zero, 0.2f); // �ε巴�� ����
            return;
        }

        // ���ϴ� �ӵ�: �������� ����(Arrive)
        float targetSpeed = maxSpeed;
        if (distance < slowRadius)
        {
            targetSpeed = Mathf.Lerp(0f, maxSpeed, distance / slowRadius);
        }

        Vector3 desiredVelocity = (distance > 0.0001f) ? (toTarget / distance) * targetSpeed : Vector3.zero;

        // Steering ���ӵ�: (��ǥ�ӵ� - ����ӵ�) / �ð�
        Vector3 currentVel = rb.velocity;
        if (freezeY) currentVel.y = 0f;

        Vector3 steering = (desiredVelocity - currentVel) / Mathf.Max(0.0001f, timeToTarget);

        // ���ӵ� ����
        if (steering.sqrMagnitude > maxAcceleration * maxAcceleration)
            steering = steering.normalized * maxAcceleration;

        // ���� ���� ���ӵ� ����
        rb.AddForce(steering, ForceMode.Acceleration);

        // ���� �ӵ� Ŭ���� (�ʹ� �̲������� �ʰ�)
        Vector3 vAfter = rb.velocity;
        Vector3 horiz = new Vector3(vAfter.x, 0f, vAfter.z);
        if (horiz.magnitude > maxSpeed)
        {
            horiz = horiz.normalized * maxSpeed;
            rb.velocity = new Vector3(horiz.x, vAfter.y, horiz.z);
        }
    }

    void OnValidate()
    {
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        maxAcceleration = Mathf.Max(0.01f, maxAcceleration);
        slowRadius = Mathf.Max(0.01f, slowRadius);
        timeToTarget = Mathf.Clamp(timeToTarget, 0.01f, 1f);
    }
}
