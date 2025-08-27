using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class EEHandToEETarget : MonoBehaviour
{
    public enum Handed { Left, Right }
    public enum Joint { Wrist, Palm, IndexTip, ThumbTip }

    [Header("XR Hands Source")]
    public Handed handed = Handed.Right;
    public Joint joint = Joint.Palm;      // eeTarget�� ������ ����(���� Palm)
    public bool requireTracked = true;

    [Header("eeTarget (IK ��ǥ)")]
    [Tooltip("IK_MasterMSM �� �����ϴ� eeTarget")]
    public Transform eeTarget;
    public Vector3 eePosOffset;           // ���� ��ġ ����(�ʿ� ��)
    public Vector3 eeEulerOffset;         // ���� ȸ�� ����(�ʿ� ��)

    [Header("����ȭ(Sync) Ʈ����")]
    [Tooltip("������ �׸� ��ġ�� �� �� ������Ʈ. ���� �� ��ġ�� �����ϸ� Sync ����")]
    public Transform syncAnchor;
    public float syncDistance = 0.10f;  // ���� �Ӱ谪
    public bool requirePinchToSync = false; // ��ġ ���� ���� ����ȭ�Ϸ��� On
    public KeyCode syncKey = KeyCode.None;     // Ű�� ���� ����ȭ(�ɼ�)
    [Tooltip("����ȭ ���� ������ ���� ������ On")]
    public bool autoUnsync = false;
    public float unsyncDistance = 0.30f;

    [Header("�� ��(��ġ) �� ����")]
    public float pinchCloseDist = 0.020f;   // ����-���� �Ÿ� <= �� 1.0
    public float pinchOpenDist = 0.060f;   // ����-���� �Ÿ� >= �� 0.0
    public MasterLeverController leverController; // ���⿡ currentDeg�� ���� �Է�
    public float leverMinDeg = 0f;
    public float leverMaxDeg = 20f;

    [Header("����")]
    public bool synced;

    XRHandSubsystem _hands;

    void OnEnable()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
            _hands = loader.GetLoadedSubsystem<XRHandSubsystem>();
    }

    void Update()
    {
        if (_hands == null || eeTarget == null) return;

        XRHand hand = (handed == Handed.Left) ? _hands.leftHand : _hands.rightHand;
        if (requireTracked && !hand.isTracked) return;

        // --- ����ȭ Ʈ���� ---
        if (!synced)
        {
            if (TryGetJointPose(hand, joint, out Pose p))
            {
                bool wantSync = false;

                if (syncAnchor)
                {
                    float dist = Vector3.Distance(p.position, syncAnchor.position);
                    wantSync = dist <= syncDistance;
                }
                if (syncKey != KeyCode.None && Input.GetKeyDown(syncKey))
                    wantSync = true;

                if (requirePinchToSync)
                    wantSync &= (EstimatePinch01(hand) > 0.2f);

                if (wantSync) synced = true;
            }
            // ����ȭ ������ eeTarget�� ���� �ǵ帮�� ����
        }
        else
        {
            // --- ����ȭ ����: XR Hand ��� eeTarget�� �״�� ���� (���� X) ---
            if (TryGetJointPose(hand, joint, out Pose pose))
            {
                eeTarget.SetPositionAndRotation(pose.position, pose.rotation);
                eeTarget.Translate(eePosOffset, Space.Self);
                eeTarget.Rotate(eeEulerOffset, Space.Self);

                if (autoUnsync)
                {
                    // ���� anchor/eeTarget���� �־����� ����(����)
                    Vector3 refPos = (syncAnchor ? syncAnchor.position : eeTarget.position);
                    float dist = Vector3.Distance(pose.position, refPos);
                    if (dist > unsyncDistance) synced = false;
                }
            }
        }

        // --- ��ġ �� ���� currentDeg (����ȭ ���ο� ����) ---
        if (leverController != null && synced)
        {
            float grip01 = EstimatePinch01(hand);
            leverController.currentDeg = Mathf.Lerp(leverMinDeg, leverMaxDeg, grip01);
        }
    }

    // ===== ��ƿ =====
    bool TryGetJointPose(XRHand hand, Joint j, out Pose pose)
    {
        XRHandJointID id = XRHandJointID.Palm;
        switch (j)
        {
            case Joint.Wrist: id = XRHandJointID.Wrist; break;
            case Joint.Palm: id = XRHandJointID.Palm; break;
            case Joint.IndexTip: id = XRHandJointID.IndexTip; break;
            case Joint.ThumbTip: id = XRHandJointID.ThumbTip; break;
        }
        var joint = hand.GetJoint(id);
        if (joint.TryGetPose(out pose)) return true;
        pose = default;
        return false;
    }

    float EstimatePinch01(XRHand hand)
    {
        if (!TryGetJointPose(hand, Joint.IndexTip, out Pose a)) return 0f;
        if (!TryGetJointPose(hand, Joint.ThumbTip, out Pose b)) return 0f;
        float d = Vector3.Distance(a.position, b.position);
        return Mathf.Clamp01(1f - Mathf.InverseLerp(pinchOpenDist, pinchCloseDist, d));
    }
}
