using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubeController : MonoBehaviour
{
    public enum MoveMode { Force, Position }

    [Header("Mode")]
    public MoveMode mode = MoveMode.Force;   // ���� ���

    [Header("Force Mode")]
    public float forceAcceleration = 20f;    // ���ӵ�(��)
    public float maxSpeed = 8f;              // ���� �ִ� �ӵ�

    [Header("Position Mode")]
    public float moveSpeed = 5f;             // ��ǥ �̵� �ӵ�

    private Rigidbody rb;
    private Vector3 input;                   // ������ �� �Է� ĳ��

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyModeSettings();
    }

    void Update()
    {
        // �⺻ �Է�(����/����: WASD �� ����Ű �⺻ ����)
        input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        // M Ű�� ��� ���
        if (Input.GetKeyDown(KeyCode.M))
        {
            mode = (mode == MoveMode.Force) ? MoveMode.Position : MoveMode.Force;
            ApplyModeSettings();
        }

        // Position ���: Transform ��� �̵� (������ ����)
        if (mode == MoveMode.Position)
        {
            transform.position += input * moveSpeed * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (mode != MoveMode.Force) return;

        // Force ���: ���� ���ӵ� ����
        if (input.sqrMagnitude > 0f)
        {
            rb.AddForce(input * forceAcceleration, ForceMode.Acceleration);

            // ���� �ӵ� Ŭ����(�ʹ� �̲������� �ʰ�)
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
            // �Է� ���� �� �ణ �����ؼ� ���� ����
            Vector3 v = rb.velocity;
            rb.velocity = new Vector3(v.x * 0.90f, v.y, v.z * 0.90f);
        }
    }

    // ��� ��ȯ �� Rigidbody ����
    void ApplyModeSettings()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        if (mode == MoveMode.Force)
        {
            rb.isKinematic = false; // ���� Ȱ��ȭ
        }
        else
        {
            rb.isKinematic = true;  // ��ǥ �̵� �� ���� ��Ȱ��ȭ
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ȭ�� �»�ܿ� ���� ��� ǥ��(���� ����)
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 24), $"Mode: {mode} (press M to switch)");
    }
}
