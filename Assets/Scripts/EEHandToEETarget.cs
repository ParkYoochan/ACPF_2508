using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class EEHandToEETarget : MonoBehaviour
{
    public enum Handed { Left, Right }
    public enum Joint { Wrist, Palm, IndexTip, ThumbTip }

    [Header("XR Hands (fallback)")]
    public Handed handed = Handed.Left;
    public Joint joint = Joint.Palm;
    public bool requireTracked = true;

    [Header("eeTarget (IK 목표)")]
    public Transform eeTarget;
    public Vector3 eePosOffset;
    public Vector3 eeEulerOffset;

    [Header("Sync (손잡이 근접시 연결)")]
    public Transform syncAnchor;            // 손잡이(그립) 위치
    [Min(0f)] public float syncDistance = 0.30f;
    public bool requirePinchToSync = false;
    public bool autoUnsync = false;
    public float unsyncDistance = 0.35f;

    [Header("Follow 옵션")]
    public bool snapOnSync = true;          // 동기화 순간 손 위치로 즉시 스냅
    public bool followOnlyWhenSynced = true;// 동기화 전에는 eeTarget 미갱신

    [Header("레버(핀치 → 각도)")]
    public MasterLeverController leverController;
    public float leverMinDeg = 0f;          // 손 펴짐
    public float leverMaxDeg = 25f;         // 손 쥠
    public float pinchCloseDist = 0.020f;   // 엄지-검지 붙음
    public float pinchOpenDist = 0.060f;   // 엄지-검지 벌어짐
    public bool invertPinch = true;         // true: 벌리면 0, 쥐면 1

    [Header("상태")]
    public bool synced;

    [Header("시각 손 본을 직접 사용(권장)")]
    public bool useVisualBones = true;      // 오프셋 없이 '보이는 손' 기준 사용
    public Transform palmBone;              // Left Hand Tracking/L_Palm
    public Transform indexTipBone;          // .../L_IndexTip
    public Transform thumbTipBone;          // .../L_ThumbTip

    [Header("디버그/Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColorNear = new Color(0f, 1f, 0.3f, 0.2f);
    public Color gizmoColorFar = new Color(1f, 0f, 0f, 0.15f);

    XRHandSubsystem _hands;

    void OnEnable()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null) _hands = loader.GetLoadedSubsystem<XRHandSubsystem>();
    }

    // 손/본이 갱신된 후 적용되도록 LateUpdate 권장
    void LateUpdate()
    {
        if (eeTarget == null) return;

        // 1) 기준 포즈 얻기 (시각 손 본 우선)
        Pose p;
        if (!TryGetVisualPose(out p))
        {
            if (!TryGetXRHandPose(out p)) return;
        }

        // 2) 동기화 판정
        bool near = !syncAnchor || Vector3.Distance(p.position, syncAnchor.position) <= syncDistance;
        if (!synced)
        {
            bool wantSync = near && (!requirePinchToSync || EstimatePinch01() > 0.2f);
            if (wantSync)
            {
                synced = true;
                if (snapOnSync) ApplyToEETarget(p);
            }

            if (!followOnlyWhenSynced) ApplyToEETarget(p);
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

        // 3) 레버(동기화 이후에만)
        if (leverController != null && synced)
        {
            float pinch01 = EstimatePinch01();
            if (invertPinch) pinch01 = 1f - pinch01;
            leverController.currentDeg = Mathf.Lerp(leverMinDeg, leverMaxDeg, pinch01);
        }
    }

    void ApplyToEETarget(Pose p)
    {
        eeTarget.SetPositionAndRotation(p.position, p.rotation);
        eeTarget.Translate(eePosOffset, Space.Self);
        eeTarget.Rotate(eeEulerOffset, Space.Self);
    }

    // ===== 손 포즈 취득 =====
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

    // ===== 핀치(엄지-검지 거리) → 0..1 =====
    float EstimatePinch01()
    {
        // 시각 손 본 우선
        if (useVisualBones && indexTipBone && thumbTipBone)
        {
            float d = Vector3.Distance(indexTipBone.position, thumbTipBone.position);
            return Mathf.Clamp01(1f - Mathf.InverseLerp(pinchOpenDist, pinchCloseDist, d));
        }

        // XRHands 백업 경로
        if (_hands == null) return 0f;
        XRHand hand = (handed == Handed.Left) ? _hands.leftHand : _hands.rightHand;
        var a = hand.GetJoint(XRHandJointID.IndexTip);
        var b = hand.GetJoint(XRHandJointID.ThumbTip);
        Pose pa, pb;
        if (!a.TryGetPose(out pa) || !b.TryGetPose(out pb)) return 0f;
        float dist = Vector3.Distance(pa.position, pb.position);
        return Mathf.Clamp01(1f - Mathf.InverseLerp(pinchOpenDist, pinchCloseDist, dist));
    }

    // ===== Gizmos =====
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || syncAnchor == null) return;
        // syncDistance
        Gizmos.color = gizmoColorFar;
        Gizmos.DrawSphere(syncAnchor.position, Mathf.Max(syncDistance, 0.001f));
        if (synced)
        {
            Gizmos.color = gizmoColorNear;
            Gizmos.DrawSphere(syncAnchor.position, Mathf.Max(syncDistance * 0.4f, 0.001f));
        }
    }
}
