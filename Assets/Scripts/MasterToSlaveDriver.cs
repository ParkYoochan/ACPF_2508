using UnityEngine;

/// <summary>
/// Master(������) ������ �о� Slave(�����̺�) ������ �����ϴ� ����̹�.
/// - ȸ����: �����ڼ�(base) ��� ��Ÿ�� �� �����̺� ����(������/������/Ŭ����)
/// - ������: ������ Axis_2�� ����/���� �̵��� '���� ������ ���� ����'�� ������ ��Į�� ��Ʈ��ũ�� ���,
///           �����̺� Axis_2�� ������ ���� �������� ���� ��Ʈ��ũ(��ȣ/���� ����)�� ����
/// - ����(0~1, deg) �� ����(Y ȸ��) ���� ����
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
    public bool fingersOpposite = true;   // R�� ��ȣ ����

    // ---------- Lever ----------
    [Header("Lever input (from MasterLeverController)")]
    public MasterLeverController lever;    // currentDeg ����
    public float leverMinDeg = 0f;
    public float leverMaxDeg = 25f;

    // ---------- Axis maps ----------
    [System.Serializable]
    public struct AxisMap
    {
        public float offset;   // �� ���� ������
        public float scale;    // ��Ÿ�� ������(��ȣ/����)
        public float minDeg;   // ����
        public float maxDeg;   // ����
        public AxisMap(float o, float s, float min, float max)
        { offset = o; scale = s; minDeg = min; maxDeg = max; }
    }

    [Header("Axis maps (deg): master �� slave")]
    public AxisMap x0 = new AxisMap(0, -1, -90, 90);   // m_Axis0(X) -> s_Axis0(X)
    public AxisMap z1 = new AxisMap(0, -1, -110, 180);  // m_Axis1(Z) -> s_Axis1(Z)
    public AxisMap yRot = new AxisMap(0, +1, -90, 90);   // m_H_Y(Y)   -> s_AxisY(Y)
    public AxisMap z3 = new AxisMap(0, -1, -180, 180);  // m_H_Z(Z)   -> s_Axis3(Z)
    public AxisMap x4 = new AxisMap(0, -1, -180, 180);  // m_H_X(X)   -> s_Axis4(X)

    // ---------- Prismatic: vector projection ----------
    [Header("Prismatic (vector projection)")]
    [Tooltip("������ Axis_2�� '���� ������ ����'(��������). ��: (0,-1,0)")]
    public Vector3 masterPrismDirLocal = Vector3.down;
    [Tooltip("�����̺� Axis_2�� '���� ������ ����'(��������). ��: (0,-1,0) �Ǵ� (0,1,0)")]
    public Vector3 slavePrismDirLocal = Vector3.down;

    [Tooltip("������ Axis_2�� '����Ʈ' ���� ������(���� ����)")]
    public Vector3 masterRestLocalPos;
    [Tooltip("�����̺� Axis_2�� '����Ʈ' ���� ������(���� ����)")]
    public Vector3 slaveRestLocalPos;

    [Tooltip("������ �� �����̺� ��Ʈ��ũ ����/��ȣ(���� ����). ���� �ݴ�� -1")]
    public float scalePrismatic = -1f;

    // ---------- (�ɼ�) ���� ���� ----------
    [Header("Prismatic space options (optional)")]
    [Tooltip("������ ��Ʈ��ũ�� ���� �������� �б�(���ð��� ���� ������ ���� ����)")]
    public bool masterPrismUseWorld = false;  // ���� ���: ���� ���. �ʿ��� ���� true
    [Tooltip("�����̺� ������ ���� �������� ����(Ư�� ���̽��� ���, �⺻�� ���� ����)")]
    public bool slavePrismApplyWorld = false;
    [Tooltip("������ Axis_2�� ����Ʈ ���� ������(���� ����)")]
    public Vector3 masterRestWorldPos;

    // ---------- Debug ----------
    [Header("Debug / Editor")]
    public bool editorCalibratePrismNow = false; // �ν����� ��۷� ����Ʈ Ķ����
    public bool drawGizmos = false;

    // ---------- Internals ----------
    Quaternion s0Base, s1Base, sYBase, s3Base, s4Base;
    Quaternion m0Base, m1Base, mYBase, m3Base, m4Base;

    void Awake()
    {
        // ȸ�� �����ڼ� ����
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

        // ����Ʈ �ڵ� �ʱ�ȭ(���� ��������� ���簪)
        if (m_Axis2 && masterRestLocalPos == Vector3.zero) masterRestLocalPos = m_Axis2.localPosition;
        if (s_Axis2 && slaveRestLocalPos == Vector3.zero) slaveRestLocalPos = s_Axis2.localPosition;
        if (m_Axis2 && masterRestWorldPos == Vector3.zero) masterRestWorldPos = m_Axis2.position;
    }

    void LateUpdate()
    {
        // 1) ȸ���� ����
        CopyLocalAngle(m_Axis0, s_Axis0, Vector3.right, x0, s0Base, m0Base);
        CopyLocalAngle(m_Axis1, s_Axis1, Vector3.forward, z1, s1Base, m1Base);
        CopyLocalAngle(m_H_Y, s_AxisY, Vector3.up, yRot, sYBase, mYBase);
        CopyLocalAngle(m_H_Z, s_Axis3, Vector3.forward, z3, s3Base, m3Base);
        CopyLocalAngle(m_H_X, s_Axis4, Vector3.right, x4, s4Base, m4Base);

        // 2) ������(�����̴�)
        if (m_Axis2 && s_Axis2)
        {
            float strokeM;

            if (masterPrismUseWorld)
            {
                // ���忡�� �б�: ���� ������ ����� ��ȯ �� ����
                Vector3 worldDir = m_Axis2.TransformDirection(masterPrismDirLocal.normalized);
                Vector3 mDeltaW = m_Axis2.position - masterRestWorldPos;
                strokeM = Vector3.Dot(mDeltaW, worldDir);
            }
            else
            {
                // ���ÿ��� �б�(�⺻)
                Vector3 mDeltaL = m_Axis2.localPosition - masterRestLocalPos;
                strokeM = Vector3.Dot(mDeltaL, masterPrismDirLocal.normalized);
            }

            // ��ȣ/���� ���� ����
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

            // ����� ���ϸ� �ּ� ����
            // Debug.Log($"strokeM={strokeM:F4}, scalePrismatic={scalePrismatic}, sStroke={sStroke:F4}, sLocal={s_Axis2.localPosition}");
        }

        // 3) ���� �� ����
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
