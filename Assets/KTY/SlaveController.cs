using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SlaveController : MonoBehaviour
{
    [Header("Follow Target (Master)")]
    public Transform target;                 // Master_Sphere Transform을 드래그해서 할당

    [Header("Motion Limits")]
    public float maxSpeed = 8f;              // 수평 최대 속도 (m/s)
    public float maxAcceleration = 30f;      // 최대 가속도 (m/s^2)

    [Header("Arrive Tuning")]
    public float slowRadius = 2.0f;          // 이 거리부터 감속 시작
    public float arriveTolerance = 0.05f;    // 이 안이면 도착 처리
    public float timeToTarget = 0.1f;        // 원하는 속도에 근접하는 데 걸리는 시간 (PD의 D 성격)

    [Header("Plane Lock")]
    public bool freezeY = true;              // 수평면만 이동
    public bool freezeRotation = true;       // 회전 고정

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;

        // 회전/높이 고정 옵션
        if (freezeRotation) rb.constraints |= RigidbodyConstraints.FreezeRotation;
        if (freezeY) rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void FixedUpdate()
    {
        if (!target) return;

        // 목표까지의 벡터(수평면 기준 선택)
        Vector3 toTarget = target.position - transform.position;
        if (freezeY) toTarget.y = 0f;

        float distance = toTarget.magnitude;

        // 도착 처리: 미세 감쇠로 잔여 속도 빨리 줄이기
        if (distance < arriveTolerance)
        {
            Vector3 v = rb.velocity;
            if (freezeY) v.y = 0f;
            rb.velocity = Vector3.Lerp(v, Vector3.zero, 0.2f); // 부드럽게 정지
            return;
        }

        // 원하는 속도: 가까울수록 감속(Arrive)
        float targetSpeed = maxSpeed;
        if (distance < slowRadius)
        {
            targetSpeed = Mathf.Lerp(0f, maxSpeed, distance / slowRadius);
        }

        Vector3 desiredVelocity = (distance > 0.0001f) ? (toTarget / distance) * targetSpeed : Vector3.zero;

        // Steering 가속도: (목표속도 - 현재속도) / 시간
        Vector3 currentVel = rb.velocity;
        if (freezeY) currentVel.y = 0f;

        Vector3 steering = (desiredVelocity - currentVel) / Mathf.Max(0.0001f, timeToTarget);

        // 가속도 제한
        if (steering.sqrMagnitude > maxAcceleration * maxAcceleration)
            steering = steering.normalized * maxAcceleration;

        // 질량 무관 가속도 적용
        rb.AddForce(steering, ForceMode.Acceleration);

        // 수평 속도 클램프 (너무 미끄러지지 않게)
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
