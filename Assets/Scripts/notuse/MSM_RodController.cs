using UnityEngine;

public class MSM_RodController : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Anchors")]
    public Transform baseAnchor;     // 힌지 중심(보통 RollPivot 또는 그 바로 아래 빈 오브젝트)
    public Transform target;         // 손잡이(또는 마우스/VR 컨트롤러)가 가리키는 목표 Transform

    [Header("Rod geometry")]
    public Axis lengthAxis = Axis.Z; // Rod 길이가 늘어나는 로컬 축 (모델에 맞춰 X/Y/Z 중 선택)
    public float minLength = 0.15f;  // 축소 한계
    public float maxLength = 0.60f;  // 신장 한계
    public float radiusOffset = 0.00f; // 모델 반지름 보정(끝이 관통되면 +로 조금 더해줌)

    [Header("Smoothing")]
    public float lengthLerp = 20f;   // 길이 보간(0=즉시, 값이 클수록 빠르게 따라감)
    public float aimLerp = 20f;    // 조준 보간

    Vector3 _baseToTarget;
    float _curLen;

    void Start()
    {
        if (!baseAnchor) baseAnchor = transform; // 안전장치
        _curLen = minLength;
    }

    void LateUpdate()
    {
        if (!baseAnchor || !target) return;

        // 1) 방향/거리
        Vector3 basePos = baseAnchor.position;
        Vector3 tarPos = target.position;
        _baseToTarget = tarPos - basePos;

        float dist = Mathf.Max(0f, _baseToTarget.magnitude - radiusOffset);
        float targetLen = Mathf.Clamp(dist, minLength, maxLength);

        // 2) 회전(막대가 lengthAxis 방향으로 target을 바라보도록)
        if (_baseToTarget.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(_baseToTarget.normalized, Vector3.up);

            // 로컬 Z가 길이축이 아닐 때 보정 회전
            Quaternion axisFix = Quaternion.identity;
            if (lengthAxis == Axis.X) axisFix = Quaternion.Euler(0, -90, 0); // Z→X
            else if (lengthAxis == Axis.Y) axisFix = Quaternion.Euler(90, 0, 0);  // Z→Y

            transform.rotation = Quaternion.Slerp(transform.rotation, look * axisFix, 1 - Mathf.Exp(-aimLerp * Time.deltaTime));
        }

        // 3) 길이 스케일
        _curLen = Mathf.Lerp(_curLen, targetLen, 1 - Mathf.Exp(-lengthLerp * Time.deltaTime));

        Vector3 s = transform.localScale;
        if (lengthAxis == Axis.X) s.x = _curLen;
        else if (lengthAxis == Axis.Y) s.y = _curLen;
        else s.z = _curLen;
        transform.localScale = s;

        // 4) 막대 시작점이 baseAnchor와 겹치게 위치 보정
        // RodVisual 원점이 힌지에 놓이도록, 부모(RollPivot) 기준 localPosition=0을 권장.
        // 만약 월드 기준으로 강제 정렬이 필요하면 주석 해제:
        // transform.position = baseAnchor.position;
    }

    // 힌트: 씬에서 길이 축을 확인하기 위한 기즈모
    void OnDrawGizmosSelected()
    {
        if (!baseAnchor || !target) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(baseAnchor.position, target.position);
    }
}
