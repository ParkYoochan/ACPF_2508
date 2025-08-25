using UnityEngine;

public class MSM_RodController : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Anchors")]
    public Transform baseAnchor;     // ���� �߽�(���� RollPivot �Ǵ� �� �ٷ� �Ʒ� �� ������Ʈ)
    public Transform target;         // ������(�Ǵ� ���콺/VR ��Ʈ�ѷ�)�� ����Ű�� ��ǥ Transform

    [Header("Rod geometry")]
    public Axis lengthAxis = Axis.Z; // Rod ���̰� �þ�� ���� �� (�𵨿� ���� X/Y/Z �� ����)
    public float minLength = 0.15f;  // ��� �Ѱ�
    public float maxLength = 0.60f;  // ���� �Ѱ�
    public float radiusOffset = 0.00f; // �� ������ ����(���� ����Ǹ� +�� ���� ������)

    [Header("Smoothing")]
    public float lengthLerp = 20f;   // ���� ����(0=���, ���� Ŭ���� ������ ����)
    public float aimLerp = 20f;    // ���� ����

    Vector3 _baseToTarget;
    float _curLen;

    void Start()
    {
        if (!baseAnchor) baseAnchor = transform; // ������ġ
        _curLen = minLength;
    }

    void LateUpdate()
    {
        if (!baseAnchor || !target) return;

        // 1) ����/�Ÿ�
        Vector3 basePos = baseAnchor.position;
        Vector3 tarPos = target.position;
        _baseToTarget = tarPos - basePos;

        float dist = Mathf.Max(0f, _baseToTarget.magnitude - radiusOffset);
        float targetLen = Mathf.Clamp(dist, minLength, maxLength);

        // 2) ȸ��(���밡 lengthAxis �������� target�� �ٶ󺸵���)
        if (_baseToTarget.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(_baseToTarget.normalized, Vector3.up);

            // ���� Z�� �������� �ƴ� �� ���� ȸ��
            Quaternion axisFix = Quaternion.identity;
            if (lengthAxis == Axis.X) axisFix = Quaternion.Euler(0, -90, 0); // Z��X
            else if (lengthAxis == Axis.Y) axisFix = Quaternion.Euler(90, 0, 0);  // Z��Y

            transform.rotation = Quaternion.Slerp(transform.rotation, look * axisFix, 1 - Mathf.Exp(-aimLerp * Time.deltaTime));
        }

        // 3) ���� ������
        _curLen = Mathf.Lerp(_curLen, targetLen, 1 - Mathf.Exp(-lengthLerp * Time.deltaTime));

        Vector3 s = transform.localScale;
        if (lengthAxis == Axis.X) s.x = _curLen;
        else if (lengthAxis == Axis.Y) s.y = _curLen;
        else s.z = _curLen;
        transform.localScale = s;

        // 4) ���� �������� baseAnchor�� ��ġ�� ��ġ ����
        // RodVisual ������ ������ ���̵���, �θ�(RollPivot) ���� localPosition=0�� ����.
        // ���� ���� �������� ���� ������ �ʿ��ϸ� �ּ� ����:
        // transform.position = baseAnchor.position;
    }

    // ��Ʈ: ������ ���� ���� Ȯ���ϱ� ���� �����
    void OnDrawGizmosSelected()
    {
        if (!baseAnchor || !target) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(baseAnchor.position, target.position);
    }
}
