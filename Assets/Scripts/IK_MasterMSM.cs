using UnityEngine;

/// <summary>
/// Master MSM�� 3DOF ��ġ IK(+ ������) + ������ 3��(Y��Z��X) ȸ�� ����
/// ��� ����:
/// Axis_0 (+X ȸ��)
///   ���� Axis_1 (+Z ȸ��)
///       ���� Axis_2 (������ +Y �Ǵ� -Y, �ν����� axis2Local�� ����)
///           ���� H_Axis_Y (+Y ȸ��: body_2/3/4)
///               ���� H_Axis_Z (+Z ȸ��: body_5 Z)
///                   ���� H_Axis_X (+X ȸ��: body_5 X)
///                       ���� Handle (����)
///
/// ����:
/// - eeTarget: ����ڰ� �����̴� ��ǥ(��Ʈ�ѷ�/�� ������Ʈ)
/// - dim0/1/2: �� ���� �� ���� ������
/// - axis*_Local, hAxis*_Local: �� ���� ���� ȸ�� ����(���� ����)
/// - Axis2 �������� q2 >= q2Min, <= q2Max�θ� �þ(�� ������ axis2Local ����)
/// </summary>
public class IK_Master3DOF : MonoBehaviour
{
    [Header("=== Chain Roots ===")]
    public Transform axis0;      // +X Revolute
    public Transform axis1;      // +Z Revolute
    public Transform axis2;      // +Y Prismatic (�Ǵ� -Y, axis2Local ����)
    public Transform handle;     // End-effector
    public Transform eeTarget;   // ����ڰ� �����̴� ��ǥ(�̰� ����)

    [Header("=== Local Offsets (from each joint) ===")]
    // axis1.localPosition = dim0
    // axis2.localPosition = dim1 + axis2Local.normalized * q2
    // handle.localPosition = dim2  (���� ������ �� ������)
    public Vector3 dim0 = new Vector3(-0.859f, 0f, 0f);
    public Vector3 dim1 = new Vector3(0f, -1.290f, 0f);
    public Vector3 dim2 = new Vector3(0f, -0.10f, 0f);

    [Header("=== Local Joint Axes (unit vectors) ===")]
    public Vector3 axis0Local = Vector3.right;    // +X
    public Vector3 axis1Local = Vector3.forward;  // +Z
    public Vector3 axis2Local = Vector3.down;     // (0,-1,0) : �Ʒ��� �þ  (���ϸ� up���� �ٲ㵵 ��)

    [Header("=== Joint States ===")]
    public float q0Deg = 0f;   // Axis_0 angle (deg)
    public float q1Deg = 0f;   // Axis_1 angle (deg)
    public float q2 = 0f;      // Axis_2 extension (m)

    [Header("=== Limits ===")]
    public float q0Min = -30f, q0Max = 30f;
    public float q1Min = -20f, q1Max = 90f;
    public float q2Min = 0.00f, q2Max = 0.30f;    // ������ ��Ʈ��ũ

    [Header("=== IK Params (position only) ===")]
    public bool solvePosition = true;
    public int iterations = 64;
    [Range(0.01f, 1f)] public float step = 0.25f;    // Jacobian Transpose ����
    public float stopEps = 1e-3f;

    // ---------------- Handle Rotation (Y -> Z -> X) ----------------
    [Header("=== Handle Axes (Y -> Z -> X) ===")]
    public Transform hAxisY; // +Y : body_2/3/4
    public Transform hAxisZ; // +Z : body_5 (Z)
    public Transform hAxisX; // +X : body_5 (X)

    [Header("Handle Local Axes (unit vectors)")]
    public Vector3 hAxisY_Local = Vector3.up;
    public Vector3 hAxisZ_Local = Vector3.forward;
    public Vector3 hAxisX_Local = Vector3.right;

    [Header("Handle Limits (deg)")]
    public float hYMin = -90f, hYMax = 90f;
    public float hZMin = -90f, hZMax = 90f;
    public float hXMin = -90f, hXMax = 90f;

    [Header("Drive Options")]
    public bool driveHandleRotationFromTarget = true; // eeTarget ȸ���� �ڵ鿡 �ݿ�

    // ---------------------------------------------------------------

    void Start()
    {
        // �ʱ� ���� ��ġ(�θ�-�ڽ� ���� ����)
        if (axis1) axis1.localPosition = dim0;
        if (axis2) axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;

        ApplyPose(); // �ʱ� ����/������ �ݿ�
    }

    void Update()
    {
        if (!axis0 || !axis1 || !axis2 || !handle || !eeTarget) return;

        if (solvePosition)
            SolvePositionIK();

        // ������ ���� ��, �ڵ� ȸ�� ����
        if (driveHandleRotationFromTarget)
            SolveHandleRotation();
    }

    // ---------------- IK: Position (Jacobian Transpose) ----------------
    void SolvePositionIK()
    {
        Vector3 a0 = axis0Local.normalized;
        Vector3 a1 = axis1Local.normalized;
        Vector3 a2 = axis2Local.normalized;

        for (int k = 0; k < iterations; k++)
        {
            ApplyPose(); // ���� q���� Transform�� �ݿ�

            Vector3 p0 = axis0.position;
            Vector3 p1 = axis1.position;
            Vector3 pe = handle.position;
            Vector3 pt = eeTarget.position;
            Vector3 err = pt - pe;

            if (err.sqrMagnitude < stopEps * stopEps) break;

            // ���� ��ǥ ȸ��/�̵� ��
            Vector3 w0 = axis0.TransformDirection(a0);
            Vector3 w1 = axis1.TransformDirection(a1);
            Vector3 w2 = axis2.TransformDirection(a2); // prismatic

            // ���ں�� �÷�(��ġ��)
            Vector3 j0 = Vector3.Cross(w0, pe - p0); // revolute
            Vector3 j1 = Vector3.Cross(w1, pe - p1); // revolute
            Vector3 j2 = w2;                         // prismatic

            // Jacobian Transpose ������Ʈ
            float dq0 = step * Vector3.Dot(j0, err);         // rad
            float dq1 = step * Vector3.Dot(j1, err);         // rad
            float dq2m = step * Vector3.Dot(j2, err);        // meter

            q0Deg = Mathf.Clamp(q0Deg + dq0 * Mathf.Rad2Deg, q0Min, q0Max);
            q1Deg = Mathf.Clamp(q1Deg + dq1 * Mathf.Rad2Deg, q1Min, q1Max);
            q2 = Mathf.Clamp(q2 + dq2m, q2Min, q2Max);

            // ������ ���� ��ġ ����
            axis2.localPosition = dim1 + a2 * q2;
        }

        ApplyPose(); // ���� �� ���� �ݿ�
    }

    // ---------------- Handle Rotation: Y -> Z -> X ----------------
    void SolveHandleRotation()
    {
        if (!hAxisY || !hAxisZ || !hAxisX) return;

        // 1) Y (�θ�=axis2 ����, ��ǥ forward�� ���� ������ ����)
        Vector3 Ay = hAxisY.parent.TransformDirection(hAxisY_Local.normalized);
        Vector3 fParY = hAxisY.parent.forward;
        Vector3 fTgtY = Vector3.ProjectOnPlane(eeTarget.forward, Ay).normalized;
        Vector3 fParYproj = Vector3.ProjectOnPlane(fParY, Ay).normalized;
        float yDeg = SafeSignedAngle(fParYproj, fTgtY, Ay);
        yDeg = Mathf.Clamp(yDeg, hYMin, hYMax);
        hAxisY.localRotation = Quaternion.AngleAxis(yDeg, hAxisY_Local);

        // 2) Z (�θ�=hAxisY ����, ��ǥ up�� ����)
        Vector3 Az = hAxisZ.parent.TransformDirection(hAxisZ_Local.normalized);
        Vector3 upParZ = hAxisZ.parent.up;
        Vector3 upTgtZ = Vector3.ProjectOnPlane(eeTarget.up, Az).normalized;
        Vector3 upParZproj = Vector3.ProjectOnPlane(upParZ, Az).normalized;
        float zDeg = SafeSignedAngle(upParZproj, upTgtZ, Az);
        zDeg = Mathf.Clamp(zDeg, hZMin, hZMax);
        hAxisZ.localRotation = Quaternion.AngleAxis(zDeg, hAxisZ_Local);

        // 3) X (�θ�=hAxisZ ����, ��ǥ forward�� ����)
        Vector3 Ax = hAxisX.parent.TransformDirection(hAxisX_Local.normalized);
        Vector3 fParX = hAxisX.parent.forward;
        Vector3 fTgtX = Vector3.ProjectOnPlane(eeTarget.forward, Ax).normalized;
        Vector3 fParXproj = Vector3.ProjectOnPlane(fParX, Ax).normalized;
        float xDeg = SafeSignedAngle(fParXproj, fTgtX, Ax);
        xDeg = Mathf.Clamp(xDeg, hXMin, hXMax);
        hAxisX.localRotation = Quaternion.AngleAxis(xDeg, hAxisX_Local);
    }

    // ��ġ������ ������ SignedAngle
    static float SafeSignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        if (from.sqrMagnitude < 1e-12f || to.sqrMagnitude < 1e-12f)
            return 0f;
        from.Normalize(); to.Normalize(); axis.Normalize();
        return Vector3.SignedAngle(from, to, axis);
    }

    // ���� q���� Transform�� ����(�θ�-�ڽ� ������ ����)
    void ApplyPose()
    {
        if (axis1) axis1.localPosition = dim0;
        if (axis2) axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;

        if (axis0) axis0.localRotation = Quaternion.AngleAxis(q0Deg, axis0Local.normalized);
        if (axis1) axis1.localRotation = Quaternion.AngleAxis(q1Deg, axis1Local.normalized);
        // axis2�� ������(ȸ�� X). �ʿ��ϸ� ���� ������ ȸ�� �߰� ����.
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // �� ���Ͱ� (90,0,0) ���� ������ ���� ����ȭ�ؼ� ���
        if (axis0Local.sqrMagnitude < 1e-9f) axis0Local = Vector3.right;
        if (axis1Local.sqrMagnitude < 1e-9f) axis1Local = Vector3.forward;
        if (axis2Local.sqrMagnitude < 1e-9f) axis2Local = Vector3.down;

        if (hAxisY_Local.sqrMagnitude < 1e-9f) hAxisY_Local = Vector3.up;
        if (hAxisZ_Local.sqrMagnitude < 1e-9f) hAxisZ_Local = Vector3.forward;
        if (hAxisX_Local.sqrMagnitude < 1e-9f) hAxisX_Local = Vector3.right;

        // q2 ���� �ڵ� ����
        q0Deg = Mathf.Clamp(q0Deg, q0Min, q0Max);
        q1Deg = Mathf.Clamp(q1Deg, q1Min, q1Max);
        q2 = Mathf.Clamp(q2, q2Min, q2Max);
    }
#endif
}
