using UnityEngine;

public class PincherGrasp : MonoBehaviour
{
    [Header("Finger triggers (손가락 끝에 붙은 Trigger)")]
    public PincherFingerTrigger fingerA;   // 왼손가락 트리거
    public PincherFingerTrigger fingerB;   // 오른손가락 트리거

    [Header("Pinch distance (between tips)")]
    public Transform A_d;                  // 왼손가락 끝 포인트
    public Transform B_d;                  // 오른손가락 끝 포인트
    public float closeDistance = 0.02f;    // 붙일 임계값(테스트는 넉넉히)
    public float releaseDistance = 0.05f;  // 뗄 임계값(히스테리시스)

    [Header("Target filter")]
    public string grabbableTag = "Grabbable";

    [Header("Attach options")]
    [Tooltip("비우면 자동 생성해서 손가락 사이 가운데로 스냅")]
    public Transform snapPoint;
    public bool makeKinematicWhileHeld = true;

    [Header("Auto snapPoint")]
    public bool autoCreateSnapPoint = true;
    public bool autoCenterOnAttach = true;

    [Header("Finger auto-wire")]
    [Tooltip("자식에서 PincherFingerTrigger를 찾아 fingerA/B 자동 배선")]
    public bool autoWireFingers = true;

    [Header("Hold tuning")]
    public float releaseDwell = 0.2f;
    public bool holdDisableGravity = true;
    public bool holdFreezeRotation = true;

    [Header("Manual detach")]
    public KeyCode detachKey = KeyCode.Space;

    [Header("Debug")]
    public bool debugLog = true;
    [Tooltip("현재 집게 끝 거리 (읽기전용 표시용)")]
    public float debugPinchDistance;
    [Tooltip("현재 후보 타깃 이름 (읽기전용 표시용)")]
    public string debugCandidateName;
    [Tooltip("현재 잡고 있는 타깃 이름 (읽기전용 표시용)")]
    public string debugHoldingName;

    // 내부 상태
    private GameObject target;
    private Transform savedParent;
    private Rigidbody targetRb;
    private Vector3 savedVel, savedAngVel;
    private bool savedUseGravity;
    private RigidbodyConstraints savedConstraints;
    private float releaseTimer = 0f;

    void Awake()
    {
        if (autoWireFingers)
        {
            // 자식에서 자동으로 A/B를 찾아 채운다(이름에 L/R, _L/_R 등이 있으면 더 쉽게 매칭됨)
            if (!fingerA || !fingerB)
            {
                var triggers = GetComponentsInChildren<PincherFingerTrigger>(true);
                foreach (var t in triggers)
                {
                    if (!t.owner) t.owner = this;
                }
                // 간단히 첫 2개 매핑
                if (triggers.Length >= 1 && !fingerA) fingerA = triggers[0];
                if (triggers.Length >= 2 && !fingerB) fingerB = triggers[1];
            }
        }

        if (!snapPoint && autoCreateSnapPoint)
        {
            var go = new GameObject("SnapPoint(auto)");
            snapPoint = go.transform;
            snapPoint.SetParent(transform, false);
            if (A_d && B_d) SnapPointToMidpointWorld();
            else snapPoint.localPosition = Vector3.zero;
            snapPoint.localRotation = Quaternion.identity;
        }
        else if (snapPoint && A_d && B_d && autoCenterOnAttach)
        {
            SnapPointToMidpointWorld();
        }
    }

    void Update()
    {
        debugPinchDistance = CurrentPinchDistance();

        if (target && Input.GetKeyDown(detachKey))
            DetachObject();

        if (!target)
        {
            var candidate = GetCommonTouchedObject();
            debugCandidateName = candidate ? candidate.name : "";

            if (candidate && debugPinchDistance <= closeDistance)
            {
                if (autoCenterOnAttach && snapPoint && A_d && B_d)
                    SnapPointToMidpointWorld();
                AttachObject(candidate);
            }
        }
        else
        {
            bool bothStillTouching =
                (fingerA && fingerA.IsTouching(target)) &&
                (fingerB && fingerB.IsTouching(target));
            bool distanceTooFar = debugPinchDistance >= releaseDistance;

            if (!bothStillTouching || distanceTooFar) releaseTimer += Time.deltaTime;
            else releaseTimer = 0f;

            if (releaseTimer >= releaseDwell)
                DetachObject();

            debugHoldingName = target ? target.name : "";
        }
    }

    // 손가락에서 자동 배선을 원할 때 호출
    public void TryAutoWire(PincherFingerTrigger trigger)
    {
        if (!fingerA) fingerA = trigger;
        else if (!fingerB && trigger != fingerA) fingerB = trigger;
    }

    private float CurrentPinchDistance()
    {
        if (!A_d || !B_d) return float.MaxValue;
        return Vector3.Distance(A_d.position, B_d.position);
    }

    private void SnapPointToMidpointWorld()
    {
        if (!A_d || !B_d || !snapPoint) return;
        Vector3 midW = (A_d.position + B_d.position) * 0.5f;
        snapPoint.position = midW;
        snapPoint.rotation = transform.rotation;
    }

    private GameObject GetCommonTouchedObject()
    {
        if (!fingerA || !fingerB) return null;
        foreach (var a in fingerA.TouchingObjects)
        {
            if (!a || !a.CompareTag(grabbableTag)) continue;
            foreach (var b in fingerB.TouchingObjects)
                if (a == b) return a;
        }
        return null;
    }

    private void AttachObject(GameObject obj)
    {
        target = obj;
        targetRb = target.GetComponent<Rigidbody>();
        savedParent = target.transform.parent;

        if (targetRb)
        {
            savedVel = targetRb.velocity;
            savedAngVel = targetRb.angularVelocity;
            savedUseGravity = targetRb.useGravity;
            savedConstraints = targetRb.constraints;

            if (makeKinematicWhileHeld) targetRb.isKinematic = true;
            if (holdDisableGravity) targetRb.useGravity = false;
            if (holdFreezeRotation) targetRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        var p = snapPoint ? snapPoint : transform;
        target.transform.SetParent(p, false);
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;

        releaseTimer = 0f;
        debugHoldingName = target.name;
        if (debugLog) Debug.Log($"[Grasp] ATTACH {target.name}");
    }

    public void DetachObject()
    {
        if (!target) return;

        target.transform.SetParent(savedParent, true);
        if (targetRb)
        {
            if (makeKinematicWhileHeld) targetRb.isKinematic = false;
            targetRb.useGravity = savedUseGravity;
            targetRb.constraints = savedConstraints;
            targetRb.velocity = savedVel;
            targetRb.angularVelocity = savedAngVel;
        }

        if (debugLog) Debug.Log($"[Grasp] DETACH");
        target = null; targetRb = null; savedParent = null; releaseTimer = 0f;
        debugHoldingName = "";
        debugCandidateName = "";
    }

    public void OnFingerTouchChanged() { /* 확장용 */ }

    void OnDrawGizmosSelected()
    {
        if (A_d && B_d)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(A_d.position, B_d.position);
        }
        if (snapPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(snapPoint.position, 0.01f);
        }
    }
}
