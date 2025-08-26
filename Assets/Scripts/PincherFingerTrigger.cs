using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PincherFingerTrigger : MonoBehaviour
{
    [Tooltip("이 손가락이 알림을 보낼 그립 컨트롤러 (비워두면 부모에서 자동 검색)")]
    public PincherGrasp owner;

    [Header("Debug")]
    public bool debugLog = true;
    [Tooltip("현재 이 손끝 트리거에 닿아 있는 Grabbable 수 (읽기전용 표시용)")]
    public int debugTouchCount;
    [Tooltip("마지막으로 접촉한 오브젝트 이름 (읽기전용 표시용)")]
    public string debugLastHit;

    // 현재 이 손가락 트리거에 들어와 있는 Grabbable들의 콜라이더 집합
    private readonly HashSet<GameObject> touching = new HashSet<GameObject>();
    public IReadOnlyCollection<GameObject> TouchingObjects => touching;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 반드시 Trigger

        if (!GetComponent<Rigidbody>())
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Transform 구동이라 kinematic 권장
        }
    }

    void Awake()
    {
        if (!owner) owner = GetComponentInParent<PincherGrasp>();
        if (owner && owner.autoWireFingers)
        {
            // PincherGrasp가 자동배선 원하면 거기 slots 채우게 통지
            owner.TryAutoWire(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!owner) return;
        if (!other.CompareTag(owner.grabbableTag)) return;

        touching.Add(other.gameObject);
        debugTouchCount = touching.Count;
        debugLastHit = other.name;
        if (debugLog) Debug.Log($"[{name}] Enter -> {other.name}");
        owner.OnFingerTouchChanged();
    }

    void OnTriggerExit(Collider other)
    {
        if (!owner) return;
        if (!other.CompareTag(owner.grabbableTag)) return;

        touching.Remove(other.gameObject);
        debugTouchCount = touching.Count;
        debugLastHit = other.name;
        if (debugLog) Debug.Log($"[{name}] Exit -> {other.name}");
        owner.OnFingerTouchChanged();
    }

    public bool IsTouching(GameObject go) => go && touching.Contains(go);

    void OnDrawGizmosSelected()
    {
        // 손끝 위치 확인용
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.01f);
    }
}
