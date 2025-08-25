using UnityEngine;

/// <summary>
/// MSM 슬레이브용 간단 IK:
/// - 3DOF 위치: Revolute(+X), Revolute(+Z), Prismatic(±Y)
/// - 손목 3축: Y -> Z -> X 순서로 타깃 회전 추종
/// - 그립(핀처): 타깃 R/L 간 거리(width)를 현재 폭에 맞춰 대칭 회전으로 수렴(P제어)
///
/// 계층(권장):
/// S_Axis_0 (+X) → S_Axis_1 (+Z) → S_Axis_2 (프리즘 ±Y)
///   → S_WristY(+Y) → S_WristZ(+Z) → S_WristX(+X) → S_Tool (말단)
///
/// 타깃:
/// - eeTarget: 말단 포지션/자세 타깃 (Master 브릿지가 계속 갱신)
/// - eeTargetR/L: 그립 폭 타깃(있으면 사용; 없으면 eeTarget 기준 합성 가능)
///
/// 주의:
/// - 로컬 축은 반드시 실제 힌지 방향과 일치하도록 배치(로컬 핸들 기준).
/// - 프리즘은 axis2Local 방향으로 q2가 증가.
/// </summary>
public class IK_SlaveMSM : MonoBehaviour
{
    // ------------ Chain (3DOF) ------------
    [Header("=== Chain (3DOF) ===")]
    public Transform sAxis0;    // +X Revolute
    public Transform sAxis1;    // +Z Revolute
    public Transform sAxis2;    //  ±Y Prismatic (axis2Local 방향)
    public Transform sTool;     // End-effector(말단)

    [Header("Targets")]
    public Transform eeTarget;      // 필수: 말단 위치/자세 타깃
    public Transform eeTargetR;     // 선택: 그립 R 타깃(있으면 폭 직접 사용)
    public Transform eeTargetL;     // 선택: 그립 L 타깃

    [Header("Local Offsets (from each joint)")]
    // sAxis1.localPosition = sDim0
    // sAxis2.localPosition = sDim1 + axis2Local * q2
    // sTool.localPosition  = sDim2
    public Vector3 sDim0 = new Vector3(-0.566f, 0f, 0f);   // 슬레이브 길이에 맞게 조정
    public Vector3 sDim1 = new Vector3(0f, -1.263f, 0f);
    public Vector3 sDim2 = Vector3.zero;                   // 요청대로 0이 기본

    [Header("Local Joint Axes (unit vectors)")]
    public Vector3 axis0Local = Vector3.right;    // +X
    public Vector3 axis1Local = Vector3.forward;  // +Z
    public Vector3 axis2Local = Vector3.down;     // (0,-1,0): 아래로 늘어남 (원하면 up)

    [Header("Joint States")]
    public float q0Deg = 0f;   // S_Axis_0 angle (deg)
    public float q1Deg = 0f;   // S_Axis_1 angle (deg)
    public float q2 = 0f;   // S_Axis_2 extension (m)

    [Header("Limits")]
    public float q0Min = -30f, q0Max = 30f;
    public float q1Min = -20f, q1Max = 90f;
    public float q2Min = 0.0f, q2Max = 1.0f;     // 요청 반영: 최대 1m

    [Header("IK Params (position only)")]
    public bool solvePosition = true;
    public int iterations = 64;
    [Range(0.01f, 1f)] public float step = 0.25f;   // Jacobian Transpose gain
    public float stopEps = 1e-3f;

    // ------------ Wrist (Y -> Z -> X) ------------
    [Header("=== Wrist (Y -> Z -> X) ===")]
    public Transform sWristY; // +Y
    public Transform sWristZ; // +Z
    public Transform sWristX; // +X

    [Header("Wrist Local Axes")]
    public Vector3 wAxisY_Local = Vector3.up;
    public Vector3 wAxisZ_Local = Vector3.forward;
    public Vector3 wAxisX_Local = Vector3.right;

    [Header("Wrist Limits (deg)")]
    public float wYMin = -90f, wYMax = 90f;
    public float wZMin = -90f, wZMax = 90f;
    public float wXMin = -90f, wXMax = 90f;

    public bool driveWristFromTarget = true;  // eeTarget.rotation을 손목에 반영

    // ------------ Gripper ------------
    [Header("=== Gripper ===")]
    public Transform gAxisR;      // 오른쪽 힌지(로컬 회전축 기본 +Y)
    public Transform gAxisL;      // 왼쪽  힌지(로컬 회전축 기본 +Y, 부호 반전 적용)
    public Transform gTipR;       // 폭 측정용 R 끝점(선택, 없으면 축 위치 사용)
    public Transform gTipL;       // 폭 측정용 L 끝점
    public Vector3 gAxisLocal = Vector3.up;  // 그립 회전 로컬 축
    public float gAngleR = 0f, gAngleL = 0f; // 누적 각도(도)
    public float gMin = -60f, gMax = 60f;    // 각도 제한(도)
    [Range(0f, 10f)] public float gripGain = 1200f; // deg/m, 폭 오차→각도 변환 이득
    public float gripDamp = 0.2f;                       // 부드럽게
    public float targetWidthFallback = 0.08f;           // R/L 타깃 없을 때 목표 폭(미터)

    void Start()
    {
        if (sAxis1) sAxis1.localPosition = sDim0;
        if (sAxis2) sAxis2.localPosition = sDim1 + axis2Local.normalized * q2;
        if (sTool) sTool.localPosition = sDim2;
        ApplyPose();
    }

    void Update()
    {
        if (!eeTarget || !sAxis0 || !sAxis1 || !sAxis2 || !sTool) return;

        if (solvePosition) SolvePositionIK();

        if (driveWristFromTarget) SolveWristRotation();

        SolveGripper(); // R/L 타깃이 있으면 폭 추종, 없으면 fallback 폭 유지
    }

    // ---------------- 3DOF Position IK (Jacobian Transpose) ----------------
    void SolvePositionIK()
    {
        Vector3 a0 = axis0Local.normalized;
        Vector3 a1 = axis1Local.normalized;
        Vector3 a2 = axis2Local.normalized;

        for (int i = 0; i < iterations; i++)
        {
            ApplyPose();

            Vector3 p0 = sAxis0.position;
            Vector3 p1 = sAxis1.position;
            Vector3 pe = sTool.position;
            Vector3 pt = eeTarget.position;
            Vector3 err = pt - pe;
            if (err.sqrMagnitude < stopEps * stopEps) break;

            Vector3 w0 = sAxis0.TransformDirection(a0);
            Vector3 w1 = sAxis1.TransformDirection(a1);
            Vector3 w2 = sAxis2.TransformDirection(a2);

            Vector3 j0 = Vector3.Cross(w0, pe - p0); // revolute
            Vector3 j1 = Vector3.Cross(w1, pe - p1); // revolute
            Vector3 j2 = w2;                         // prismatic

            float dq0 = step * Vector3.Dot(j0, err);      // rad
            float dq1 = step * Vector3.Dot(j1, err);      // rad
            float dq2m = step * Vector3.Dot(j2, err);     // m

            q0Deg = Mathf.Clamp(q0Deg + dq0 * Mathf.Rad2Deg, q0Min, q0Max);
            q1Deg = Mathf.Clamp(q1Deg + dq1 * Mathf.Rad2Deg, q1Min, q1Max);
            q2 = Mathf.Clamp(q2 + dq2m, q2Min, q2Max);

            sAxis2.localPosition = sDim1 + a2 * q2;
        }

        ApplyPose();
    }

    // ---------------- Wrist: Y -> Z -> X ----------------
    void SolveWristRotation()
    {
        if (!sWristY || !sWristZ || !sWristX) return;

        // 1) Y
        Vector3 Ay = sWristY.parent.TransformDirection(wAxisY_Local.normalized);
        Vector3 fParY = sWristY.parent.forward;
        Vector3 fTgtY = Vector3.ProjectOnPlane(eeTarget.forward, Ay).normalized;
        Vector3 fParYproj = Vector3.ProjectOnPlane(fParY, Ay).normalized;
        float yDeg = SafeSignedAngle(fParYproj, fTgtY, Ay);
        yDeg = Mathf.Clamp(yDeg, wYMin, wYMax);
        sWristY.localRotation = Quaternion.AngleAxis(yDeg, wAxisY_Local);

        // 2) Z
        Vector3 Az = sWristZ.parent.TransformDirection(wAxisZ_Local.normalized);
        Vector3 upParZ = sWristZ.parent.up;
        Vector3 upTgtZ = Vector3.ProjectOnPlane(eeTarget.up, Az).normalized;
        Vector3 upParZproj = Vector3.ProjectOnPlane(upParZ, Az).normalized;
        float zDeg = SafeSignedAngle(upParZproj, upTgtZ, Az);
        zDeg = Mathf.Clamp(zDeg, wZMin, wZMax);
        sWristZ.localRotation = Quaternion.AngleAxis(zDeg, wAxisZ_Local);

        // 3) X
        Vector3 Ax = sWristX.parent.TransformDirection(wAxisX_Local.normalized);
        Vector3 fParX = sWristX.parent.forward;
        Vector3 fTgtX = Vector3.ProjectOnPlane(eeTarget.forward, Ax).normalized;
        Vector3 fParXproj = Vector3.ProjectOnPlane(fParX, Ax).normalized;
        float xDeg = SafeSignedAngle(fParXproj, fTgtX, Ax);
        xDeg = Mathf.Clamp(xDeg, wXMin, wXMax);
        sWristX.localRotation = Quaternion.AngleAxis(xDeg, wAxisX_Local);
    }

    // ---------------- Gripper (폭 추종: 간단 P제어) ----------------
    void SolveGripper()
    {
        if (!gAxisR || !gAxisL) return;

        // 현재 폭
        Vector3 rPos = gTipR ? gTipR.position : gAxisR.position;
        Vector3 lPos = gTipL ? gTipL.position : gAxisL.position;
        float widthNow = Vector3.Distance(rPos, lPos);

        // 목표 폭
        float widthTarget;
        if (eeTargetR && eeTargetL) widthTarget = Vector3.Distance(eeTargetR.position, eeTargetL.position);
        else widthTarget = targetWidthFallback;

        float err = widthTarget - widthNow; // +면 더 벌려야 함
        // 각도 변경량(도): deg = gain * m * dt(가정: Update의 dt≈1프레임. 필요시 Time.deltaTime 곱해도 됨)
        float dDeg = gripGain * err * (1f - gripDamp);

        gAngleR = Mathf.Clamp(gAngleR + dDeg, gMin, gMax);
        gAngleL = Mathf.Clamp(gAngleL - dDeg, gMin, gMax); // 반대 방향

        // 적용(로컬 +Y 축 기준; 필요시 gAxisLocal 변경)
        gAxisR.localRotation = Quaternion.AngleAxis(gAngleR, gAxisLocal.normalized);
        gAxisL.localRotation = Quaternion.AngleAxis(gAngleL, gAxisLocal.normalized);
    }

    // ---------------- Utils ----------------
    static float SafeSignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        if (from.sqrMagnitude < 1e-12f || to.sqrMagnitude < 1e-12f) return 0f;
        from.Normalize(); to.Normalize(); axis.Normalize();
        return Vector3.SignedAngle(from, to, axis);
    }

    void ApplyPose()
    {
        if (sAxis1) sAxis1.localPosition = sDim0;
        if (sAxis2) sAxis2.localPosition = sDim1 + axis2Local.normalized * q2;
        if (sTool) sTool.localPosition = sDim2;

        if (sAxis0) sAxis0.localRotation = Quaternion.AngleAxis(q0Deg, axis0Local.normalized);
        if (sAxis1) sAxis1.localRotation = Quaternion.AngleAxis(q1Deg, axis1Local.normalized);
        // sAxis2는 프리즘(회전 없음)
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (axis0Local.sqrMagnitude < 1e-9f) axis0Local = Vector3.right;
        if (axis1Local.sqrMagnitude < 1e-9f) axis1Local = Vector3.forward;
        if (axis2Local.sqrMagnitude < 1e-9f) axis2Local = Vector3.down;

        if (wAxisY_Local.sqrMagnitude < 1e-9f) wAxisY_Local = Vector3.up;
        if (wAxisZ_Local.sqrMagnitude < 1e-9f) wAxisZ_Local = Vector3.forward;
        if (wAxisX_Local.sqrMagnitude < 1e-9f) wAxisX_Local = Vector3.right;

        if (gAxisLocal.sqrMagnitude < 1e-9f) gAxisLocal = Vector3.up;

        q0Deg = Mathf.Clamp(q0Deg, q0Min, q0Max);
        q1Deg = Mathf.Clamp(q1Deg, q1Min, q1Max);
        q2 = Mathf.Clamp(q2, q2Min, q2Max);
        gAngleR = Mathf.Clamp(gAngleR, gMin, gMax);
        gAngleL = Mathf.Clamp(gAngleL, gMin, gMax);
    }
#endif
}
