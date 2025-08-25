using UnityEngine;

public class MasterToSlaveDriver : MonoBehaviour
{
    [Header("Master joints (drag from Master_left)")]
    public Transform m_Axis0;
    public Transform m_Axis1;
    public Transform m_Axis2;
    public Transform m_H_Y;
    public Transform m_H_Z;
    public Transform m_H_X;

    [Header("Slave joints (drag from Slave_left)")]
    public Transform s_Axis0;
    public Transform s_Axis1;
    public Transform s_Axis2;
    public Transform s_AxisY;
    public Transform s_Axis3;
    public Transform s_Axis4;

    [Header("Grip (Axis_5_L / Axis_5_R)")]
    public Transform s_FingerL;
    public Transform s_FingerR;
    public float gripOpenDeg = 0f;
    public float gripClosedDeg = 20f;
    public bool fingersOpposite = true;

    [Header("Lever input (from MasterLeverController)")]
    public MasterLeverController lever;
    public float leverMinDeg = 0f;
    public float leverMaxDeg = 25f;

    [System.Serializable]
    public struct AxisMap
    {
        public float offset;
        public float scale;
        public float minDeg;
        public float maxDeg;
        public AxisMap(float o, float s, float min, float max)
        { offset = o; scale = s; minDeg = min; maxDeg = max; }
    }

    [Header("Axis maps (deg): master ¡æ slave")]
    public AxisMap x0 = new AxisMap(0, -1, -90, 90);
    public AxisMap z1 = new AxisMap(0, -1, -110, 180);
    public AxisMap yRot = new AxisMap(0, +1, -90, 90);
    public AxisMap z3 = new AxisMap(0, -1, -180, 180);
    public AxisMap x4 = new AxisMap(0, -1, -180, 180);

    [Header("Prismatic (vector projection)")]
    public Vector3 masterPrismDirLocal = Vector3.down;
    public Vector3 slavePrismDirLocal = Vector3.down;
    public Vector3 masterRestLocalPos;
    public Vector3 slaveRestLocalPos;
    public float scalePrismatic = -1f;

    [Header("Debug / Editor")]
    public bool editorCalibratePrismNow = false;
    public bool drawGizmos = false;

    Quaternion s0Base, s1Base, sYBase, s3Base, s4Base;
    Quaternion m0Base, m1Base, mYBase, m3Base, m4Base;

    void Awake()
    {
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

        if (masterPrismDirLocal.sqrMagnitude < 1e-9f) masterPrismDirLocal = Vector3.down;
        if (slavePrismDirLocal.sqrMagnitude < 1e-9f) slavePrismDirLocal = Vector3.down;
        masterPrismDirLocal = masterPrismDirLocal.normalized;
        slavePrismDirLocal = slavePrismDirLocal.normalized;

        if (m_Axis2 && masterRestLocalPos == Vector3.zero) masterRestLocalPos = m_Axis2.localPosition;
        if (s_Axis2 && slaveRestLocalPos == Vector3.zero) slaveRestLocalPos = s_Axis2.localPosition;
    }

    void LateUpdate()
    {
        CopyLocalAngle(m_Axis0, s_Axis0, Vector3.right, x0, s0Base, m0Base);
        CopyLocalAngle(m_Axis1, s_Axis1, Vector3.forward, z1, s1Base, m1Base);
        CopyLocalAngle(m_H_Y, s_AxisY, Vector3.up, yRot, sYBase, mYBase);
        CopyLocalAngle(m_H_Z, s_Axis3, Vector3.forward, z3, s3Base, m3Base);
        CopyLocalAngle(m_H_X, s_Axis4, Vector3.right, x4, s4Base, m4Base);

        if (m_Axis2 && s_Axis2)
        {
            Vector3 mDelta = m_Axis2.localPosition - masterRestLocalPos;
            float strokeM = Vector3.Dot(mDelta, masterPrismDirLocal);
            Vector3 sPos = slaveRestLocalPos + slavePrismDirLocal * (strokeM * scalePrismatic);
            s_Axis2.localPosition = sPos;
        }

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

    void OnValidate()
    {
        if (editorCalibratePrismNow)
        {
            editorCalibratePrismNow = false;
            CalibratePrismRest();
            Debug.Log("[MasterToSlaveDriver] Calibrated prism rest via OnValidate.");
        }
        if (masterPrismDirLocal.sqrMagnitude < 1e-9f) masterPrismDirLocal = Vector3.down;
        if (slavePrismDirLocal.sqrMagnitude < 1e-9f) slavePrismDirLocal = Vector3.down;
        masterPrismDirLocal = masterPrismDirLocal.normalized;
        slavePrismDirLocal = slavePrismDirLocal.normalized;
    }

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

    [ContextMenu("Calibrate Prism Rest (use current)")]
    void CalibratePrismRest()
    {
        if (m_Axis2) masterRestLocalPos = m_Axis2.localPosition;
        if (s_Axis2) slaveRestLocalPos = s_Axis2.localPosition;
        Debug.Log($"[CalibratePrism] masterRestLocalPos={masterRestLocalPos}, slaveRestLocalPos={slaveRestLocalPos}");
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.cyan;
        if (m_Axis2)
        {
            Vector3 a = m_Axis2.TransformPoint(Vector3.zero);
            Gizmos.DrawLine(a, a + m_Axis2.TransformVector(masterPrismDirLocal) * 0.3f);
        }
        if (s_Axis2)
        {
            Vector3 b = s_Axis2.TransformPoint(Vector3.zero);
            Gizmos.DrawLine(b, b + s_Axis2.TransformVector(slavePrismDirLocal) * 0.3f);
        }
    }
}
