using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class EEHandToEETarget : MonoBehaviour
{
    public enum Handed { Left, Right }
    public enum Joint { Wrist, Palm, IndexTip, ThumbTip }

    [Header("XR Hands (fallback when visual bones not set)")]
    public Handed handed = Handed.Left;
    public Joint joint = Joint.Palm;
    public bool requireTracked = true;

    [Header("eeTarget (IK Target)")]
    public Transform eeTarget;
    public Vector3 eePosOffset;          // local-space position offset applied after rotation
    public Vector3 eeEulerOffset;        // extra euler after all rotations

    [Header("Sync (close to handle �� link)")]
    public Transform syncAnchor;         // handle(�׸�) ��ġ
    [Min(0f)] public float syncDistance = 0.30f;
    public bool requirePinchToSync = false;
    public bool autoUnsync = false;
    public float unsyncDistance = 0.35f; // synced ���� ���� �Ӱ�Ÿ�(> syncDistance ����)

    [Header("Follow options")]
    public bool snapOnSync = true;          // ����ȭ ���� ��� ����
    public bool followOnlyWhenSynced = true;// ����ȭ ������ eeTarget ���� �� ��

    [Header("Lever (pinch �� degrees)")]
    public MasterLeverController leverController; // Master_left�� �ִ� ��Ʈ�ѷ�
    public float leverMinDeg = 0f;           // �� ����
    public float leverMaxDeg = 25f;          // �� ��
    public float pinchCloseDist = 0.020f;    // ����-���� ����
    public float pinchOpenDist = 0.060f;    // ����-���� ������
    public bool invertPinch = false;          // true: ������ 0, ��� 1 (��û�� ����)

    [Header("State")]
    public bool synced;

    [Header("Use visual hand bones (recommended)")]
    public bool useVisualBones = true;       // ���̴� �� ���� ���� ���(������ ���ʿ�)
    public Transform palmBone;               // Left Hand Tracking/L_Palm
    public Transform indexTipBone;           // .../L_IndexTip
    public Transform thumbTipBone;           // .../L_ThumbTip

    [Header("Orientation Align (handle alignment)")]
    public bool alignToHandle = true;        // ����ȭ ���� �� ȸ���� �׸� �������� ����
    public Transform gripAlign;              // SyncAnchor �ڽ�: "���� �ùٸ��� ����" �̻��� ȸ��
    public Vector3 extraEulerAfterAlign;     // �̼� ����(-90, +90, 0 ��)

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColorNear = new Color(0f, 1f, 0.3f, 0.2f);
    public Color gizmoColorFar = new Color(1f, 0f, 0f, 0.15f);

    XRHandSubsystem _hands;
    Quaternion _rotOffset = Quaternion.identity;

    void OnEnable()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null) _hands = loader.GetLoadedSubsystem<XRHandSubsystem>();
    }

    // ��/�� ���� ���� ���󰡵��� LateUpdate ����
    void LateUpdate()
    {
        if (eeTarget == null) return;

        // 1) ���� ���� ��� (�ð� �� �� �켱)
        Pose p;
        if (!TryGetVisualPose(out p))
        {
            if (!TryGetXRHandPose(out p)) return;
        }

        // 2) ����ȭ ����
        bool near = !syncAnchor || Vector3.Distance(p.position, syncAnchor.position) <= syncDistance;

        if (!synced)
        {
            bool wantSync = near && (!requirePinchToSync || EstimatePinch01() > 0.2f);
            if (wantSync)
            {
                synced = true;

                // ȸ�� ������ ���: handRot * rotOffset = gripAlignRot
                if (alignToHandle && gripAlign != null)
                    _rotOffset = Quaternion.Inverse(p.rotation) * gripAlign.rotation;

                if (snapOnSync)
                    ApplyToEETarget(p);
            }

            if (!followOnlyWhenSynced)
                ApplyToEETarget(p);
        }
        else
        {
            ApplyToEETarget(p);

            if (autoUnsync && syncAnchor)
            {
                float d = Vector3.Distance(p.position, syncAnchor.position);
                if (d > unsyncDistance) synced = false;
            }
        }

        // 3) ����(����ȭ ���Ŀ��� ����)
        if (leverController != null && synced)
        {
            float pinch01 = EstimatePinch01();                 // 0(open) ~ 1(close)
            if (invertPinch) pinch01 = 1f - pinch01;          // ��û �������� ����
            leverController.currentDeg = Mathf.Lerp(leverMinDeg, leverMaxDeg, pinch01);
        }
    }

    void ApplyToEETarget(Pose p)
    {
        // ��ġ
        eeTarget.position = p.position;

        // ȸ��: �� ȸ�� �� (�ɼ�)�׸� ���� �� (�ɼ�)�߰� ���� �� (�ɼ�)���� ������
        Quaternion rot = p.rotation;

        if (alignToHandle)
            rot = rot * _rotOffset;

        if (extraEulerAfterAlign != Vector3.zero)
            rot = rot * Quaternion.Euler(extraEulerAfterAlign);

        if (eeEulerOffset != Vector3.zero)
            rot = rot * Quaternion.Euler(eeEulerOffset);

        eeTarget.rotation = rot;

        // ��ġ �̼� ����(�� ������ ����)
        if (eePosOffset != Vector3.zero)
            eeTarget.Translate(eePosOffset, Space.Self);
    }

    // ===== ���� ���(�ð� �� �� �켱) =====
    bool TryGetVisualPose(out Pose p)
    {
        p = default;
        if (!useVisualBones || palmBone == null) return false;
        p = new Pose(palmBone.position, palmBone.rotation);
        return true;
    }

    bool TryGetXRHandPose(out Pose p)
    {
        p = default;
        if (_hands == null) return false;

        XRHand hand = (handed == Handed.Left) ? _hands.leftHand : _hands.rightHand;
        if (requireTracked && !hand.isTracked) return false;

        XRHandJointID id = XRHandJointID.Palm;
        switch (joint)
        {
            case Joint.Wrist: id = XRHandJointID.Wrist; break;
            case Joint.Palm: id = XRHandJointID.Palm; break;
            case Joint.IndexTip: id = XRHandJointID.IndexTip; break;
            case Joint.ThumbTip: id = XRHandJointID.ThumbTip; break;
        }

        var j = hand.GetJoint(id);
        if (!j.TryGetPose(out p)) return false;
        return true;
    }

    // ===== ��ġ(����-���� �Ÿ�) �� 0..1 =====
    float EstimatePinch01()
    {
        // (1) �ð� �� �� ���
        if (useVisualBones && indexTipBone && thumbTipBone)
        {
            float d = Vector3.Distance(indexTipBone.position, thumbTipBone.position);
            // openDist(ũ��) �� 0, closeDist(�۴�) �� 1
            return Mathf.Clamp01(Mathf.InverseLerp(pinchOpenDist, pinchCloseDist, d));
        }

        // (2) XRHands ��� (���)
        if (_hands == null) return 0f;

        XRHand hand = (handed == Handed.Left) ? _hands.leftHand : _hands.rightHand;
        var a = hand.GetJoint(XRHandJointID.IndexTip);
        var b = hand.GetJoint(XRHandJointID.ThumbTip);

        Pose pa, pb;
        if (!a.TryGetPose(out pa) || !b.TryGetPose(out pb)) return 0f;

        float dist = Vector3.Distance(pa.position, pb.position);
        return Mathf.Clamp01(Mathf.InverseLerp(pinchOpenDist, pinchCloseDist, dist));
    }

    // ===== Gizmos =====
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || syncAnchor == null) return;

        // syncDistance ����
        Gizmos.color = gizmoColorFar;
        Gizmos.DrawSphere(syncAnchor.position, Mathf.Max(syncDistance, 0.001f));

        if (synced)
        {
            Gizmos.color = gizmoColorNear;
            Gizmos.DrawSphere(syncAnchor.position, Mathf.Max(syncDistance * 0.4f, 0.001f));
        }
    }
}
