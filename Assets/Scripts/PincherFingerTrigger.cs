using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PincherFingerTrigger : MonoBehaviour
{
    [Tooltip("�� �հ����� �˸��� ���� �׸� ��Ʈ�ѷ� (����θ� �θ𿡼� �ڵ� �˻�)")]
    public PincherGrasp owner;

    [Header("Debug")]
    public bool debugLog = true;
    [Tooltip("���� �� �ճ� Ʈ���ſ� ��� �ִ� Grabbable �� (�б����� ǥ�ÿ�)")]
    public int debugTouchCount;
    [Tooltip("���������� ������ ������Ʈ �̸� (�б����� ǥ�ÿ�)")]
    public string debugLastHit;

    // ���� �� �հ��� Ʈ���ſ� ���� �ִ� Grabbable���� �ݶ��̴� ����
    private readonly HashSet<GameObject> touching = new HashSet<GameObject>();
    public IReadOnlyCollection<GameObject> TouchingObjects => touching;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // �ݵ�� Trigger

        if (!GetComponent<Rigidbody>())
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Transform �����̶� kinematic ����
        }
    }

    void Awake()
    {
        if (!owner) owner = GetComponentInParent<PincherGrasp>();
        if (owner && owner.autoWireFingers)
        {
            // PincherGrasp�� �ڵ��輱 ���ϸ� �ű� slots ä��� ����
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
        // �ճ� ��ġ Ȯ�ο�
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.01f);
    }
}
