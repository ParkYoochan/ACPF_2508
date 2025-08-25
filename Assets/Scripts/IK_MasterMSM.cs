using UnityEngine;

/// <summary>
/// Master MSM용 3DOF 위치 IK(+ 프리즘) + 손잡이 3축(Y→Z→X) 회전 구동
/// 기대 계층:
/// Axis_0 (+X 회전)
///   └─ Axis_1 (+Z 회전)
///       └─ Axis_2 (프리즘 +Y 또는 -Y, 인스펙터 axis2Local로 결정)
///           └─ H_Axis_Y (+Y 회전: body_2/3/4)
///               └─ H_Axis_Z (+Z 회전: body_5 Z)
///                   └─ H_Axis_X (+X 회전: body_5 X)
///                       └─ Handle (말단)
///
/// 사용법:
/// - eeTarget: 사용자가 움직이는 목표(컨트롤러/빈 오브젝트)
/// - dim0/1/2: 각 관절 간 로컬 오프셋
/// - axis*_Local, hAxis*_Local: 각 축의 로컬 회전 방향(단위 벡터)
/// - Axis2 프리즘은 q2 >= q2Min, <= q2Max로만 늘어남(축 방향은 axis2Local 방향)
/// </summary>
public class IK_Master3DOF : MonoBehaviour
{
    [Header("=== Chain Roots ===")]
    public Transform axis0;      // +X Revolute
    public Transform axis1;      // +Z Revolute
    public Transform axis2;      // +Y Prismatic (또는 -Y, axis2Local 방향)
    public Transform handle;     // End-effector
    public Transform eeTarget;   // 사용자가 움직이는 목표(이걸 따라감)

    [Header("=== Local Offsets (from each joint) ===")]
    // axis1.localPosition = dim0
    // axis2.localPosition = dim1 + axis2Local.normalized * q2
    // handle.localPosition = dim2  (실제 손잡이 끝 오프셋)
    public Vector3 dim0 = new Vector3(-0.859f, 0f, 0f);
    public Vector3 dim1 = new Vector3(0f, -1.290f, 0f);
    public Vector3 dim2 = new Vector3(0f, -0.10f, 0f);

    [Header("=== Local Joint Axes (unit vectors) ===")]
    public Vector3 axis0Local = Vector3.right;    // +X
    public Vector3 axis1Local = Vector3.forward;  // +Z
    public Vector3 axis2Local = Vector3.down;     // (0,-1,0) : 아래로 늘어남  (원하면 up으로 바꿔도 됨)

    [Header("=== Joint States ===")]
    public float q0Deg = 0f;   // Axis_0 angle (deg)
    public float q1Deg = 0f;   // Axis_1 angle (deg)
    public float q2 = 0f;      // Axis_2 extension (m)

    [Header("=== Limits ===")]
    public float q0Min = -30f, q0Max = 30f;
    public float q1Min = -20f, q1Max = 90f;
    public float q2Min = 0.00f, q2Max = 0.30f;    // 프리즘 스트로크

    [Header("=== IK Params (position only) ===")]
    public bool solvePosition = true;
    public int iterations = 64;
    [Range(0.01f, 1f)] public float step = 0.25f;    // Jacobian Transpose 게인
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
    public bool driveHandleRotationFromTarget = true; // eeTarget 회전을 핸들에 반영

    // ---------------------------------------------------------------

    void Start()
    {
        // 초기 로컬 배치(부모-자식 계층 전제)
        if (axis1) axis1.localPosition = dim0;
        if (axis2) axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;

        ApplyPose(); // 초기 각도/프리즘 반영
    }

    void Update()
    {
        if (!axis0 || !axis1 || !axis2 || !handle || !eeTarget) return;

        if (solvePosition)
            SolvePositionIK();

        // 포지션 수렴 후, 핸들 회전 구동
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
            ApplyPose(); // 현재 q들을 Transform에 반영

            Vector3 p0 = axis0.position;
            Vector3 p1 = axis1.position;
            Vector3 pe = handle.position;
            Vector3 pt = eeTarget.position;
            Vector3 err = pt - pe;

            if (err.sqrMagnitude < stopEps * stopEps) break;

            // 월드 좌표 회전/이동 축
            Vector3 w0 = axis0.TransformDirection(a0);
            Vector3 w1 = axis1.TransformDirection(a1);
            Vector3 w2 = axis2.TransformDirection(a2); // prismatic

            // 야코비안 컬럼(위치만)
            Vector3 j0 = Vector3.Cross(w0, pe - p0); // revolute
            Vector3 j1 = Vector3.Cross(w1, pe - p1); // revolute
            Vector3 j2 = w2;                         // prismatic

            // Jacobian Transpose 업데이트
            float dq0 = step * Vector3.Dot(j0, err);         // rad
            float dq1 = step * Vector3.Dot(j1, err);         // rad
            float dq2m = step * Vector3.Dot(j2, err);        // meter

            q0Deg = Mathf.Clamp(q0Deg + dq0 * Mathf.Rad2Deg, q0Min, q0Max);
            q1Deg = Mathf.Clamp(q1Deg + dq1 * Mathf.Rad2Deg, q1Min, q1Max);
            q2 = Mathf.Clamp(q2 + dq2m, q2Min, q2Max);

            // 프리즘 로컬 위치 갱신
            axis2.localPosition = dim1 + a2 * q2;
        }

        ApplyPose(); // 루프 후 최종 반영
    }

    // ---------------- Handle Rotation: Y -> Z -> X ----------------
    void SolveHandleRotation()
    {
        if (!hAxisY || !hAxisZ || !hAxisX) return;

        // 1) Y (부모=axis2 기준, 목표 forward의 수평 방향을 맞춤)
        Vector3 Ay = hAxisY.parent.TransformDirection(hAxisY_Local.normalized);
        Vector3 fParY = hAxisY.parent.forward;
        Vector3 fTgtY = Vector3.ProjectOnPlane(eeTarget.forward, Ay).normalized;
        Vector3 fParYproj = Vector3.ProjectOnPlane(fParY, Ay).normalized;
        float yDeg = SafeSignedAngle(fParYproj, fTgtY, Ay);
        yDeg = Mathf.Clamp(yDeg, hYMin, hYMax);
        hAxisY.localRotation = Quaternion.AngleAxis(yDeg, hAxisY_Local);

        // 2) Z (부모=hAxisY 기준, 목표 up을 맞춤)
        Vector3 Az = hAxisZ.parent.TransformDirection(hAxisZ_Local.normalized);
        Vector3 upParZ = hAxisZ.parent.up;
        Vector3 upTgtZ = Vector3.ProjectOnPlane(eeTarget.up, Az).normalized;
        Vector3 upParZproj = Vector3.ProjectOnPlane(upParZ, Az).normalized;
        float zDeg = SafeSignedAngle(upParZproj, upTgtZ, Az);
        zDeg = Mathf.Clamp(zDeg, hZMin, hZMax);
        hAxisZ.localRotation = Quaternion.AngleAxis(zDeg, hAxisZ_Local);

        // 3) X (부모=hAxisZ 기준, 목표 forward를 맞춤)
        Vector3 Ax = hAxisX.parent.TransformDirection(hAxisX_Local.normalized);
        Vector3 fParX = hAxisX.parent.forward;
        Vector3 fTgtX = Vector3.ProjectOnPlane(eeTarget.forward, Ax).normalized;
        Vector3 fParXproj = Vector3.ProjectOnPlane(fParX, Ax).normalized;
        float xDeg = SafeSignedAngle(fParXproj, fTgtX, Ax);
        xDeg = Mathf.Clamp(xDeg, hXMin, hXMax);
        hAxisX.localRotation = Quaternion.AngleAxis(xDeg, hAxisX_Local);
    }

    // 수치적으로 안전한 SignedAngle
    static float SafeSignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        if (from.sqrMagnitude < 1e-12f || to.sqrMagnitude < 1e-12f)
            return 0f;
        from.Normalize(); to.Normalize(); axis.Normalize();
        return Vector3.SignedAngle(from, to, axis);
    }

    // 현재 q들을 Transform에 적용(부모-자식 오프셋 포함)
    void ApplyPose()
    {
        if (axis1) axis1.localPosition = dim0;
        if (axis2) axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;

        if (axis0) axis0.localRotation = Quaternion.AngleAxis(q0Deg, axis0Local.normalized);
        if (axis1) axis1.localRotation = Quaternion.AngleAxis(q1Deg, axis1Local.normalized);
        // axis2는 프리즘(회전 X). 필요하면 고정 오프셋 회전 추가 가능.
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 축 벡터가 (90,0,0) 같은 값으로 들어가도 정규화해서 사용
        if (axis0Local.sqrMagnitude < 1e-9f) axis0Local = Vector3.right;
        if (axis1Local.sqrMagnitude < 1e-9f) axis1Local = Vector3.forward;
        if (axis2Local.sqrMagnitude < 1e-9f) axis2Local = Vector3.down;

        if (hAxisY_Local.sqrMagnitude < 1e-9f) hAxisY_Local = Vector3.up;
        if (hAxisZ_Local.sqrMagnitude < 1e-9f) hAxisZ_Local = Vector3.forward;
        if (hAxisX_Local.sqrMagnitude < 1e-9f) hAxisX_Local = Vector3.right;

        // q2 범위 자동 정리
        q0Deg = Mathf.Clamp(q0Deg, q0Min, q0Max);
        q1Deg = Mathf.Clamp(q1Deg, q1Min, q1Max);
        q2 = Mathf.Clamp(q2, q2Min, q2Max);
    }
#endif
}
