using UnityEngine;

public class MasterLeverController : MonoBehaviour
{
    [Header("Lever (필수)")]
    public Transform leverPivot;             // 레버가 도는 축(힌지). 분리한 HandleGrip 자체거나 그 부모 Pivot
    public Vector3 leverAxisLocal = Vector3.right; // 레버가 도는 '로컬 축' (예: X 또는 Z)

    [Header("각도 범위(도)")]
    public float minDeg = 0f;   // 완전히 '풀린' 각도(사진의 초기 자세)
    public float maxDeg = 35f;  // 완전히 '당긴' 각도(본체에 평행 직전, 관통 금지)

    [Header("조작 키/속도")]
    public KeyCode pullKey = KeyCode.J;      // 당기기
    public KeyCode releaseKey = KeyCode.K;   // 풀기
    public float speedDegPerSec = 120f;      // 초당 회전 속도(도/초)

    [Header("초기화 옵션")]
    public bool useCurrentAsReleased = true; // 시작 각도를 '풀린 각도(minDeg)'로 삼기
    public bool clampEveryFrame = true;      // 매 프레임 범위 클램프

    [Header("슬레이브 연동(선택)")]
    public bool driveGripWidth = false;      // true면 레버 각도를 폭으로 변환
    public float halfWidthOpen = 0.06f;     // 레버가 풀렸을 때 한쪽 반폭(미터)
    public float halfWidthClosed = 0.01f;    // 레버가 끝까지 당겨졌을 때 반폭(미터)
    public MasterToSlaveBridge bridge;       // 있으면 bridge.gripHalfWidth 갱신

    // 읽기 전용 상태
    [Range(0, 1)] public float squeeze01;     // 0=풀림, 1=완전 당김
    public float currentDeg;                 // 현재 레버 각도(도)

    Quaternion baseLocalRot;                 // 레버 기준자세(풀린 상태의 로컬 회전)

    void Awake()
    {
        if (!leverPivot)
        {
            Debug.LogError("[MasterLeverController] leverPivot 이(가) 비었습니다.");
            enabled = false; return;
        }

        // 로컬 축 안전화
        if (leverAxisLocal.sqrMagnitude < 1e-9f) leverAxisLocal = Vector3.right;
        leverAxisLocal = leverAxisLocal.normalized;

        baseLocalRot = leverPivot.localRotation;

        if (useCurrentAsReleased)
        {
            // 현재 자세를 minDeg로 간주
            currentDeg = minDeg;
            ApplyLeverRotation();
        }
        else
        {
            // base 회전에 minDeg만큼 돌려서 초기자세 적용
            currentDeg = Mathf.Clamp(minDeg, Mathf.Min(minDeg, maxDeg), Mathf.Max(minDeg, maxDeg));
            ApplyLeverRotation();
        }

        UpdateOutputs();
    }

    void Update()
    {
        // 입력 → 방향
        int dir = 0;
        if (Input.GetKey(pullKey)) dir += 1;   // 당김(+)
        if (Input.GetKey(releaseKey)) dir -= 1;   // 풀기(-)

        if (dir != 0)
        {
            currentDeg += dir * speedDegPerSec * Time.deltaTime;
        }

        if (clampEveryFrame)
            currentDeg = Mathf.Clamp(currentDeg, Mathf.Min(minDeg, maxDeg), Mathf.Max(minDeg, maxDeg));

        ApplyLeverRotation();
        UpdateOutputs();
    }

    void ApplyLeverRotation()
    {
        // 레버 로컬 회전 = 기준자세 * (축 기준 회전)
        leverPivot.localRotation = baseLocalRot * Quaternion.AngleAxis(currentDeg, leverAxisLocal);
    }

    void UpdateOutputs()
    {
        // 0~1 정규화 (min=풀림, max=당김)
        float lo = Mathf.Min(minDeg, maxDeg);
        float hi = Mathf.Max(minDeg, maxDeg);
        squeeze01 = Mathf.InverseLerp(lo, hi, currentDeg);

        // 선택: 브릿지에 그립 반폭 전달(슬레이브 나중에 쓸 값)
        if (driveGripWidth && bridge != null)
        {
            float hw = Mathf.Lerp(halfWidthOpen, halfWidthClosed, squeeze01);
            bridge.gripHalfWidth = hw;
        }
    }
}
