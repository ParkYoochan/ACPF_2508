using UnityEngine;

/// <summary>
/// Master(마스터) 관절을 읽어 Slave(슬레이브) 관절로 복제하는 드라이버.
/// - 회전축: 기준자세(base) 대비 델타각 → 슬레이브 적용(오프셋/스케일/클램프)
/// - 프리즘: 마스터 Axis_2의 로컬/월드 이동을 '로컬 프리즘 방향 벡터'로 투영해 스칼라 스트로크를 얻고,
///           슬레이브 Axis_2에 지정한 로컬 방향으로 동일 스트로크(부호/배율 포함)를 적용
/// - 레버(0~1, deg) → 집게(Y 회전) 선형 매핑
/// </summary>
public class MasterToSlaveDriver : MonoBehaviour
{
    // ---------- Master joints ----------
    [Header("Master joints (drag from Master_left)")]
    public Transform m_Axis0;     // Revolute X
    public Transform m_Axis1;     // Revolute Z
    public Transform m_Axis2;     // Prismatic source
    public Transform m_H_Y;       // Handle Y
    public Transform m_H_Z;       // Handle Z
    public Transform m_H_X;       // Handle X

    // ---------- Slave joints ----------
    [Header("Slave joints (drag from Slave_left)")]
    public Transform s_Axis0;     // Revolute X
    public Transform s_Axis1;     // Revolute Z
    public Transform s_Axis2;     // Prismatic target
    public Transform s_AxisY;     // Revolute Y
    public Transform s_Axis3;     // Revolute Z
    public Transform s_Axis4;     // Revolute X

    // ---------- Grip ----------
    [Header("Grip (Axis_5_L / Axis_5_R)")]
    public Transform s_FingerL;   // Axis_5_L (Y rot)
    public Transform s_FingerR;   // Axis_5_R (Y rot)
    public float gripOpenDeg = 0f;
    public float gripClosedDeg = 20f;
    public bool fingersOpposite = true;   // R은 부호 반전

    // ---------- Lever ----------
    [Header("Lever input (from MasterLeverController)")]
    public MasterLeverController lever;    // currentDeg 제공
    public float leverMinDeg = 0f;
    public float leverMaxDeg = 25f;

    // ---------- Axis maps ----------
    [System.Serializable]
    public struct AxisMap
    {
        public float offset;   // 도 단위 오프셋
        public float scale;    // 델타각 스케일(부호/배율)
        public float minDeg;   // 하한
        public float maxDeg;   // 상한
        public AxisMap(float o, float s, float min, float max)
        { offset = o; scale = s; minDeg = min; maxDeg = max; }
    }

    [Header("Axis maps (deg): master → slave")]
    public AxisMap x0 = new AxisMap(0, -1, -90, 90);   // m_Axis0(X) -> s_Axis0(X)
    public AxisMap z1 = new AxisMap(0, -1, -110, 180);  // m_Axis1(Z) -> s_Axis1(Z)
    public AxisMap yRot = new AxisMap(0, +1, -90, 90);   // m_H_Y(Y)   -> s_AxisY(Y)
    public AxisMap z3 = new AxisMap(0, -1, -180, 180);  // m_H_Z(Z)   -> s_Axis3(Z)
    public AxisMap x4 = new AxisMap(0, -1, -180, 180);  // m_H_X(X)   -> s_Axis4(X)

    // ---------- Prismatic: vector projection ----------
    [Header("Prismatic (vector projection)")]
    [Tooltip("마스터 Axis_2의 '로컬 프리즘 방향'(단위벡터). 예: (0,-1,0)")]
    public Vector3 masterPrismDirLocal = Vector3.down;
    [Tooltip("슬레이브 Axis_2의 '로컬 프리즘 방향'(단위벡터). 예: (0,-1,0) 또는 (0,1,0)")]
    public Vector3 slavePrismDirLocal = Vector3.down;

    [Tooltip("마스터 Axis_2의 '레스트' 로컬 포지션(접힌 상태)")]
    public Vector3 masterRestLocalPos;
    [Tooltip("슬레이브 Axis_2의 '레스트' 로컬 포지션(접힌 상태)")]
    public Vector3 slaveRestLocalPos;

    [Tooltip("마스터 → 슬레이브 스트로크 배율/부호(수동 조절). 방향 반대면 -1")]
    public float scalePrismatic = -1f;

    // ---------- (옵션) 공간 선택 ----------
    [Header("Prismatic space options (optional)")]
    [Tooltip("마스터 스트로크를 월드 공간에서 읽기(로컬값이 거의 고정인 리깅 지원)")]
    public bool masterPrismUseWorld = false;  // 원래 방식: 로컬 기반. 필요할 때만 true
    [Tooltip("슬레이브 적용을 월드 공간으로 강제(특수 케이스만 사용, 기본은 로컬 적용)")]
    public bool slavePrismApplyWorld = false;
    [Tooltip("마스터 Axis_2의 레스트 월드 포지션(접힌 상태)")]
    public Vector3 masterRestWorldPos;

    // ---------- Debug ----------
    [Header("Debug / Editor")]
    public bool editorCalibratePrismNow = false; // 인스펙터 토글로 레스트 캘리브
    public bool drawGizmos = false;

    // ---------- Internals ----------
    Quaternion s0Base, s1Base, sYBase, s3Base, s4Base;
    Quaternion m0Base, m1Base, mYBase, m3Base, m4Base;

    void Awake()
    {
        // 회전 기준자세 저장
        if (s_Axis0) s0Base = s_Axis0.localRotation;
        if (s_Axis1) s1Base = s_Axis1.localRotation;
        if (s_AxisY) sYBase = s_AxisY.localRotation;
        if (s_Axis3) s3Base = s_Axis3.localRotation;
        if (s_Axis4) s4Base = s_Axis4.localRotation;

        if (m_Axis0) m0Base = m_Axis0.localRotation;
        if (m_Axis1) m1Base = m_Axis1.localRotation;
        if (m_H_Y) mYBase = m_H_Y.localRotation;
        if (m_H_Z) m3Base = m_H_Z.localRotation;
        if (m_H_X) m4Base = m_H_X.localRotation;

        NormalizePrismDirs();

        // 레스트 자동 초기화(값이 비어있으면 현재값)
        if (m_Axis2 && masterRestLocalPos == Vector3.zero) masterRestLocalPos = m_Axis2.localPosition;
        if (s_Axis2 && slaveRestLocalPos == Vector3.zero) slaveRestLocalPos = s_Axis2.localPosition;
        if (m_Axis2 && masterRestWorldPos == Vector3.zero) masterRestWorldPos = m_Axis2.position;
    }

    void LateUpdate()
    {
        // 1) 회전축 복제
        CopyLocalAngle(m_Axis0, s_Axis0, Vector3.right, x0, s0Base, m0Base);
        CopyLocalAngle(m_Axis1, s_Axis1, Vector3.forward, z1, s1Base, m1Base);
        CopyLocalAngle(m_H_Y, s_AxisY, Vector3.up, yRot, sYBase, mYBase);
        CopyLocalAngle(m_H_Z, s_Axis3, Vector3.forward, z3, s3Base, m3Base);
        CopyLocalAngle(m_H_X, s_Axis4, Vector3.right, x4, s4Base, m4Base);

        // 2) 프리즘(슬라이더)
        if (m_Axis2 && s_Axis2)
        {
            float strokeM;

            if (masterPrismUseWorld)
            {
                // 월드에서 읽기: 로컬 방향을 월드로 변환 후 투영
                Vector3 worldDir = m_Axis2.TransformDirection(masterPrismDirLocal.normalized);
                Vector3 mDeltaW = m_Axis2.position - masterRestWorldPos;
                strokeM = Vector3.Dot(mDeltaW, worldDir);
            }
            else
            {
                // 로컬에서 읽기(기본)
                Vector3 mDeltaL = m_Axis2.localPosition - masterRestLocalPos;
                strokeM = Vector3.Dot(mDeltaL, masterPrismDirLocal.normalized);
            }

            // 부호/배율 수동 조절
            float sStroke = strokeM * scalePrismatic;

            if (slavePrismApplyWorld)
            {
                Vector3 sWorldDir = s_Axis2.TransformDirection(slavePrismDirLocal.normalized);
                Vector3 anchorW = s_Axis2.parent ? s_Axis2.parent.TransformPoint(slaveRestLocalPos) : s_Axis2.position;
                s_Axis2.position = anchorW + sWorldDir * sStroke;
            }
            else
            {
                Vector3 sPos = slaveRestLocalPos + slavePrismDirLocal.normalized * sStroke;
                s_Axis2.localPosition = sPos;
            }

            // 디버그 원하면 주석 해제
            // Debug.Log($"strokeM={strokeM:F4}, scalePrismatic={scalePrismatic}, sStroke={sStroke:F4}, sLocal={s_Axis2.localPosition}");
        }

        // 3) 레버 → 집게
        if (lever && (s_FingerL || s_FingerR))
        {
            float t = Mathf.InverseLerp(leverMinDeg, leverMaxDeg, lever.currentDeg);
            t = Mathf.Clamp01(t);
            float L = Mathf.Lerp(gripOpenDeg, gripClosedDeg, t);
            if (s_FingerL) s_FingerL.localRotation = Quaternion.Euler(0, L, 0);
            if (s_FingerR)
            {
                float R = fingersOpposite ? -L : L;
                s_FingerR.localRotation = Quaternion.Euler(0, R, 0);
            }
        }
    }

    // ---------- Utilities ----------
    void CopyLocalAngle(Transform m, Transform s, Vector3 axis, AxisMap map, Quaternion sBase, Quaternion mBase)
    {
        if (!m || !s) return;
        float delta = SignedAngleOnAxis(mBase, m.localRotation, axis);
        float deg = Mathf.Clamp(map.offset + delta * map.scale, map.minDeg, map.maxDeg);
        s.localRotation = sBase * Quaternion.AngleAxis(deg, axis);
    }

    float SignedAngleOnAxis(Quaternion baseRot, Quaternion curRot, Vector3 localAxis)
    {
        Quaternion d = Quaternion.Inverse(baseRot) * curRot;
        d.ToAngleAxis(out float ang, out Vector3 ax);
        if (ang > 180f) ang -= 360f;
        float sign = Vector3.Dot(ax, localAxis.normalized) >= 0 ? 1f : -1f;
        return ang * sign;
    }

    void NormalizePrismDirs()
    {
        if (masterPrismDirLocal.sqrMagnitude < 1e-9f) masterPrismDirLocal = Vector3.down;
        if (slavePrismDirLocal.sqrMagnitude < 1e-9f) slavePrismDirLocal = Vector3.down;
        masterPrismDirLocal = masterPrismDirLocal.normalized;
        slavePrismDirLocal = slavePrismDirLocal.normalized;
    }

    [ContextMenu("Calibrate Prism Rest (use current)")]
    void CalibratePrismRest()
    {
        if (m_Axis2)
        {
            masterRestLocalPos = m_Axis2.localPosition;
            masterRestWorldPos = m_Axis2.position;
        }
        if (s_Axis2) slaveRestLocalPos = s_Axis2.localPosition;

        Debug.Log($"[CalibratePrism] mRestL={masterRestLocalPos}, mRestW={masterRestWorldPos}, sRestL={slaveRestLocalPos}");
    }

    void OnValidate()
    {
        NormalizePrismDirs();
        if (m_Axis2 && masterRestWorldPos == Vector3.zero) masterRestWorldPos = m_Axis2.position;

        if (editorCalibratePrismNow)
        {
            editorCalibratePrismNow = false;
            CalibratePrismRest();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.cyan;
        if (m_Axis2)
        {
            Vector3 a = m_Axis2.TransformPoint(Vector3.zero);
            Gizmos.DrawLine(a, a + m_Axis2.TransformDirection(masterPrismDirLocal) * 0.3f);
        }
        if (s_Axis2)
        {
            Vector3 b = s_Axis2.TransformPoint(Vector3.zero);
            Gizmos.DrawLine(b, b + s_Axis2.TransformDirection(slavePrismDirLocal) * 0.3f);
        }
    }
}
