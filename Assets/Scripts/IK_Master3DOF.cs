using UnityEngine;

public class IK_Master3DOF : MonoBehaviour
{
    [Header("Chain Roots")]
    public Transform axis0;   // +X ȸ��
    public Transform axis1;   // +Z ȸ��
    public Transform axis2;   // +Y ������
    public Transform handle;  // ����(Handle) ������
    public Transform eeTarget;// ��ǥ(�̰� ������!)

    [Header("Local Offsets (from each joint)")]
    // axis1�� localPosition = dim0
    // axis2�� localPosition = dim1 + axis2Local * q2
    // handle�� localPosition = dim2
    public Vector3 dim0 = new Vector3(-0.566f, 0f, 0f);
    public Vector3 dim1 = new Vector3(0f, -1.263f, 0f);
    public Vector3 dim2 = new Vector3(-0.05f, 0f, 0f); // ������ �� ������(�ʿ�� ����)

    [Header("Local Joint Axes")]
    public Vector3 axis0Local = Vector3.right;   // +X
    public Vector3 axis1Local = Vector3.forward; // +Z
    public Vector3 axis2Local = Vector3.up;      // +Y (������ �������)

    [Header("Joint States")]
    public float q0Deg = 0f; // Axis_0 ����(��)
    public float q1Deg = 0f; // Axis_1 ����(��)
    public float q2 = 0f;    // Axis_2 ����(����)

    [Header("Limits")]
    public float q0Min = -30f, q0Max = 30f;
    public float q1Min = -20f, q1Max = 90f;
    public float q2Min = 0.00f, q2Max = 0.30f;   // ������ ��Ʈ��ũ ����(������Ʈ�� �°� ����)

    [Header("IK Params")]
    public int iterations = 64;
    public float step = 0.25f;     // ������Ʈ ����(0.1~0.5 ���̿��� ����)
    public float stopEps = 1e-3f;  // ��ġ ���� ���� �Ӱ谪(����)

    void Start()
    {
        // �ʱ� ��ġ(����)
        axis1.localPosition = dim0;
        axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;
        ApplyPose();
    }

    void Update()
    {
        if (!axis0 || !axis1 || !axis2 || !handle || !eeTarget) return;

        for (int k = 0; k < iterations; k++)
        {
            // ���� ���� ����(Transform ����)
            ApplyPose();

            // ���� ��ǥ��
            Vector3 p0 = axis0.position;
            Vector3 p1 = axis1.position;
            Vector3 p2w = axis2.position;
            Vector3 pe = handle.position;           // end-effector (handle)
            Vector3 pt = eeTarget.position;         // target

            Vector3 err = pt - pe;                  // 3x1
            if (err.sqrMagnitude < stopEps * stopEps) break;

            // ���� ��ǥ ȸ����(��������)
            Vector3 w0 = axis0.TransformDirection(axis0Local).normalized;
            Vector3 w1 = axis1.TransformDirection(axis1Local).normalized;
            Vector3 w2 = axis2.TransformDirection(axis2Local).normalized; // prismatic

            // 3x3 ���ں���� �� �÷�(��ġ ����)
            Vector3 j0 = Vector3.Cross(w0, (pe - p0)); // revolute
            Vector3 j1 = Vector3.Cross(w1, (pe - p1)); // revolute
            Vector3 j2 = w2;                           // prismatic

            // Jacobian Transpose ������Ʈ
            // dq0, dq1: ����, dq2: ����
            float dq0 = step * Vector3.Dot(j0, err);   // rad
            float dq1 = step * Vector3.Dot(j1, err);   // rad
            float dq2 = step * Vector3.Dot(j2, err);   // m

            // ����(+Ŭ����)
            q0Deg = Mathf.Clamp(q0Deg + dq0 * Mathf.Rad2Deg, q0Min, q0Max);
            q1Deg = Mathf.Clamp(q1Deg + dq1 * Mathf.Rad2Deg, q1Min, q1Max);
            q2 = Mathf.Clamp(q2 + dq2, q2Min, q2Max);

            // ������ ���� ��ġ ������Ʈ
            axis2.localPosition = dim1 + axis2Local.normalized * q2;
        }
        // ���� ���� �ݿ� 1ȸ
        ApplyPose();
    }

    void ApplyPose()
    {
        // �θ�-�ڽ� ���� ��ġ ����
        axis1.localPosition = dim0;
        axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;

        // ���� ����(���� �� ����)
        axis0.localRotation = Quaternion.AngleAxis(q0Deg, axis0Local);
        axis1.localRotation = Quaternion.AngleAxis(q1Deg, axis1Local);
        // �������� ȸ�� ����(�ʿ��ϸ� ���� ������ ȸ�� �߰� ����)
    }
}
