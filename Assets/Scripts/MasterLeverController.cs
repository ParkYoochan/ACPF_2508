using UnityEngine;

public class MasterLeverController : MonoBehaviour
{
    [Header("Lever (�ʼ�)")]
    public Transform leverPivot;             // ������ ���� ��(����). �и��� HandleGrip ��ü�ų� �� �θ� Pivot
    public Vector3 leverAxisLocal = Vector3.right; // ������ ���� '���� ��' (��: X �Ǵ� Z)

    [Header("���� ����(��)")]
    public float minDeg = 0f;   // ������ 'Ǯ��' ����(������ �ʱ� �ڼ�)
    public float maxDeg = 35f;  // ������ '���' ����(��ü�� ���� ����, ���� ����)

    [Header("���� Ű/�ӵ�")]
    public KeyCode pullKey = KeyCode.J;      // ����
    public KeyCode releaseKey = KeyCode.K;   // Ǯ��
    public float speedDegPerSec = 120f;      // �ʴ� ȸ�� �ӵ�(��/��)

    [Header("�ʱ�ȭ �ɼ�")]
    public bool useCurrentAsReleased = true; // ���� ������ 'Ǯ�� ����(minDeg)'�� ���
    public bool clampEveryFrame = true;      // �� ������ ���� Ŭ����

    [Header("�����̺� ����(����)")]
    public bool driveGripWidth = false;      // true�� ���� ������ ������ ��ȯ
    public float halfWidthOpen = 0.06f;     // ������ Ǯ���� �� ���� ����(����)
    public float halfWidthClosed = 0.01f;    // ������ ������ ������� �� ����(����)
    public MasterToSlaveBridge bridge;       // ������ bridge.gripHalfWidth ����

    // �б� ���� ����
    [Range(0, 1)] public float squeeze01;     // 0=Ǯ��, 1=���� ���
    public float currentDeg;                 // ���� ���� ����(��)

    Quaternion baseLocalRot;                 // ���� �����ڼ�(Ǯ�� ������ ���� ȸ��)

    void Awake()
    {
        if (!leverPivot)
        {
            Debug.LogError("[MasterLeverController] leverPivot ��(��) ������ϴ�.");
            enabled = false; return;
        }

        // ���� �� ����ȭ
        if (leverAxisLocal.sqrMagnitude < 1e-9f) leverAxisLocal = Vector3.right;
        leverAxisLocal = leverAxisLocal.normalized;

        baseLocalRot = leverPivot.localRotation;

        if (useCurrentAsReleased)
        {
            // ���� �ڼ��� minDeg�� ����
            currentDeg = minDeg;
            ApplyLeverRotation();
        }
        else
        {
            // base ȸ���� minDeg��ŭ ������ �ʱ��ڼ� ����
            currentDeg = Mathf.Clamp(minDeg, Mathf.Min(minDeg, maxDeg), Mathf.Max(minDeg, maxDeg));
            ApplyLeverRotation();
        }

        UpdateOutputs();
    }

    void Update()
    {
        // �Է� �� ����
        int dir = 0;
        if (Input.GetKey(pullKey)) dir += 1;   // ���(+)
        if (Input.GetKey(releaseKey)) dir -= 1;   // Ǯ��(-)

        if (dir != 0)
        {
            currentDeg += dir * speedDegPerSec * Time.deltaTime;
        }

        if (clampEveryFrame)
            currentDeg = Mathf.Clamp(currentDeg, Mathf.Min(minDeg, maxDeg), Mathf.Max(minDeg, maxDeg));

        ApplyLeverRotation();
        UpdateOutputs();
    }

    void ApplyLeverRotation()
    {
        // ���� ���� ȸ�� = �����ڼ� * (�� ���� ȸ��)
        leverPivot.localRotation = baseLocalRot * Quaternion.AngleAxis(currentDeg, leverAxisLocal);
    }

    void UpdateOutputs()
    {
        // 0~1 ����ȭ (min=Ǯ��, max=���)
        float lo = Mathf.Min(minDeg, maxDeg);
        float hi = Mathf.Max(minDeg, maxDeg);
        squeeze01 = Mathf.InverseLerp(lo, hi, currentDeg);

        // ����: �긴���� �׸� ���� ����(�����̺� ���߿� �� ��)
        if (driveGripWidth && bridge != null)
        {
            float hw = Mathf.Lerp(halfWidthOpen, halfWidthClosed, squeeze01);
            bridge.gripHalfWidth = hw;
        }
    }
}
