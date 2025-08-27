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

    [Header("Sync (close to handle → link)")]
    public Transform syncAnchor;         // handle(그립) 위치
    [Min(0f)] public float syncDistance = 0.30f;
    public bool requirePinchToSync = false;
    public bool autoUnsync = false;
    public float unsyncDistance = 0.35f; // synced 상태 해제 임계거리(> syncDistance 권장)

    [Header("Follow options")]
    public bool snapOnSync = true;          // 동기화 순간 즉시 스냅
    public bool followOnlyWhenSynced = true;// 동기화 전에는 eeTarget 갱신 안 함

    [Header("Lever (pinch → degrees)")]
    public MasterLeverController leverController; // Master_left에 있는 컨트롤러
    public float leverMinDeg = 0f;           // 손 펴짐
    public float leverMaxDeg = 25f;          // 손 쥠
    public float pinchCloseDist = 0.020f;    // 엄지-검지 붙음
    public float pinchOpenDist = 0.060f;    // 엄지-검지 벌어짐
    public bool invertPinch = false;          // true: 벌리면 0, 쥐면 1 (요청한 방향)

    [Header("State")]
    public bool synced;

    [Header("Use visual hand bones (recommended)")]
    public bool useVisualBones = true;       // 보이는 손 본을 직접 사용(오프셋 불필요)
    public Transform palmBone;               // Left Hand Tracking/L_Palm
    public Transform indexTipBone;           // .../L_IndexTip
    public Transform thumbTipBone;           // .../L_ThumbTip

    [Header("Orientation Align (handle alignment)")]
    public bool alignToHandle = true;        // 동기화 순간 손 회전을 그립 기준으로 정렬
    public Transform gripAlign;              // SyncAnchor 자식: "손이 올바르게 잡은" 이상적 회전
    public Vector3 extraEulerAfterAlign;     // 미세 보정(-90, +90, 0 등)

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

    // 손/본 갱신 이후 따라가도록 LateUpdate 권장
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

                // 회전 오프셋 계산: handRot * rotOffset = gripAlignRot
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

        // 3) 레버(동기화 이후에만 반응)
        if (leverController != null && synced)
        {
            float pinch01 = EstimatePinch01();                 // 0(open) ~ 1(close)
            if (invertPinch) pinch01 = 1f - pinch01;          // 요청 방향으로 반전
            leverController.currentDeg = Mathf.Lerp(leverMinDeg, leverMaxDeg, pinch01);
        }
    }

    void ApplyToEETarget(Pose p)
    {
        // 위치
        eeTarget.position = p.position;

        // 회전: 손 회전 → (옵션)그립 정렬 → (옵션)추가 보정 → (옵션)기존 오프셋
        Quaternion rot = p.rotation;

        if (alignToHandle)
            rot = rot * _rotOffset;

        if (extraEulerAfterAlign != Vector3.zero)
            rot = rot * Quaternion.Euler(extraEulerAfterAlign);

        if (eeEulerOffset != Vector3.zero)
            rot = rot * Quaternion.Euler(eeEulerOffset);

        eeTarget.rotation = rot;

        // 위치 미세 보정(손 로컬축 기준)
        if (eePosOffset != Vector3.zero)
            eeTarget.Translate(eePosOffset, Space.Self);
    }

    // ===== 포즈 취득(시각 손 본 우선) =====
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
        // (1) 시각 손 본 경로
        if (useVisualBones && indexTipBone && thumbTipBone)
        {
            float d = Vector3.Distance(indexTipBone.position, thumbTipBone.position);
            // openDist(크다) → 0, closeDist(작다) → 1
            return Mathf.Clamp01(Mathf.InverseLerp(pinchOpenDist, pinchCloseDist, d));
        }

        // (2) XRHands 경로 (백업)
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

        // syncDistance 범위
        Gizmos.color = gizmoColorFar;
        Gizmos.DrawSphere(syncAnchor.position, Mathf.Max(syncDistance, 0.001f));

        if (synced)
        {
            Gizmos.color = gizmoColorNear;
            Gizmos.DrawSphere(syncAnchor.position, Mathf.Max(syncDistance * 0.4f, 0.001f));
        }
    }
}
