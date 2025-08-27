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
    public Joint joint = Joint.Palm;      // eeTarget을 구동할 관절(보통 Palm)
    public bool requireTracked = true;

    [Header("eeTarget (IK 목표)")]
    [Tooltip("IK_MasterMSM 이 참조하는 eeTarget")]
    public Transform eeTarget;
    public Vector3 eePosOffset;           // 로컬 위치 보정(필요 시)
    public Vector3 eeEulerOffset;         // 로컬 회전 보정(필요 시)

    [Header("동기화(Sync) 트리거")]
    [Tooltip("손잡이 그립 위치에 둔 빈 오브젝트. 손이 이 위치에 근접하면 Sync 시작")]
    public Transform syncAnchor;
    public float syncDistance = 0.10f;  // 근접 임계값
    public bool requirePinchToSync = false; // 핀치 중일 때만 동기화하려면 On
    public KeyCode syncKey = KeyCode.None;     // 키로 수동 동기화(옵션)
    [Tooltip("동기화 해제 조건을 쓰고 싶으면 On")]
    public bool autoUnsync = false;
    public float unsyncDistance = 0.30f;

    [Header("손 쥠(핀치) → 레버")]
    public float pinchCloseDist = 0.020f;   // 검지-엄지 거리 <= → 1.0
    public float pinchOpenDist = 0.060f;   // 검지-엄지 거리 >= → 0.0
    public MasterLeverController leverController; // 여기에 currentDeg를 직접 입력
    public float leverMinDeg = 0f;
    public float leverMaxDeg = 20f;

    [Header("상태")]
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

        // --- 동기화 트리거 ---
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
            // 동기화 전에는 eeTarget을 절대 건드리지 않음
        }
        else
        {
            // --- 동기화 이후: XR Hand 포즈를 eeTarget에 그대로 전달 (고정 X) ---
            if (TryGetJointPose(hand, joint, out Pose pose))
            {
                eeTarget.SetPositionAndRotation(pose.position, pose.rotation);
                eeTarget.Translate(eePosOffset, Space.Self);
                eeTarget.Rotate(eeEulerOffset, Space.Self);

                if (autoUnsync)
                {
                    // 손이 anchor/eeTarget에서 멀어지면 해제(선택)
                    Vector3 refPos = (syncAnchor ? syncAnchor.position : eeTarget.position);
                    float dist = Vector3.Distance(pose.position, refPos);
                    if (dist > unsyncDistance) synced = false;
                }
            }
        }

        // --- 핀치 → 레버 currentDeg (동기화 여부와 무관) ---
        if (leverController != null && synced)
        {
            float grip01 = EstimatePinch01(hand);
            leverController.currentDeg = Mathf.Lerp(leverMinDeg, leverMaxDeg, grip01);
        }
    }

    // ===== 유틸 =====
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
