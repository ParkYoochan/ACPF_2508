using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubeController : MonoBehaviour
{
    public enum MoveMode { Force, Position }

    [Header("Mode")]
    public MoveMode mode = MoveMode.Force;   // 시작 모드

    [Header("Force Mode")]
    public float forceAcceleration = 20f;    // 가속도(힘)
    public float maxSpeed = 8f;              // 수평 최대 속도

    [Header("Position Mode")]
    public float moveSpeed = 5f;             // 좌표 이동 속도

    private Rigidbody rb;
    private Vector3 input;                   // 프레임 간 입력 캐시

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyModeSettings();
    }

    void Update()
    {
        // 기본 입력(수평/수직: WASD 및 방향키 기본 매핑)
        input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        // M 키로 모드 토글
        if (Input.GetKeyDown(KeyCode.M))
        {
            mode = (mode == MoveMode.Force) ? MoveMode.Position : MoveMode.Force;
            ApplyModeSettings();
        }

        // Position 모드: Transform 기반 이동 (프레임 독립)
        if (mode == MoveMode.Position)
        {
            transform.position += input * moveSpeed * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (mode != MoveMode.Force) return;

        // Force 모드: 물리 가속도 적용
        if (input.sqrMagnitude > 0f)
        {
            rb.AddForce(input * forceAcceleration, ForceMode.Acceleration);

            // 수평 속도 클램프(너무 미끄러지지 않게)
            Vector3 v = rb.velocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            if (horiz.magnitude > maxSpeed)
            {
                horiz = horiz.normalized * maxSpeed;
                rb.velocity = new Vector3(horiz.x, v.y, horiz.z);
            }
        }
        else
        {
            // 입력 없을 때 약간 감쇠해서 빨리 멈춤
            Vector3 v = rb.velocity;
            rb.velocity = new Vector3(v.x * 0.90f, v.y, v.z * 0.90f);
        }
    }

    // 모드 전환 시 Rigidbody 설정
    void ApplyModeSettings()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        if (mode == MoveMode.Force)
        {
            rb.isKinematic = false; // 물리 활성화
        }
        else
        {
            rb.isKinematic = true;  // 좌표 이동 시 물리 비활성화
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // 화면 좌상단에 현재 모드 표시(선택 사항)
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 24), $"Mode: {mode} (press M to switch)");
    }
}
