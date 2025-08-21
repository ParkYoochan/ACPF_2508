using UnityEngine;

public class MSM2AxisController : MonoBehaviour
{
    [Header("Pivots")]
    public Transform yawPivot;    // 자신(YawPivot)
    public Transform pitchPivot;  // 자식(PitchPivot)

    [Header("Speeds (deg/sec)")]
    public float yawSpeed = 90f;
    public float pitchSpeed = 90f;

    [Header("Angle Limits (deg, local)")]
    public float yawMin = -35f;
    public float yawMax = 35f;
    public float pitchMin = -25f;
    public float pitchMax = 25f;

    [Header("Invert")]
    public bool invertYaw = false;
    public bool invertPitch = false;

    float _yaw;   // 누적 각도(로컬)
    float _pitch;

    void Awake()
    {
        if (!yawPivot) yawPivot = transform;              // 이 스크립트가 붙은 곳
        if (!pitchPivot) pitchPivot = transform.GetChild(0); // 관례: 첫 자식이 Pitch
        // 초기 각도 읽어 두기 (있다면)
        _yaw = yawPivot.localEulerAngles.y;
        _yaw = (_yaw > 180f) ? _yaw - 360f : _yaw;
        _pitch = pitchPivot.localEulerAngles.x;
        _pitch = (_pitch > 180f) ? _pitch - 360f : _pitch;
    }

    void Update()
    {
        // 입력: A/D(좌우=Yaw), W/S(앞뒤=Pitch) — 나중에 VR로 교체
        float yawInput = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
        float pitchInput = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);

        if (invertYaw) yawInput = -yawInput;
        if (invertPitch) pitchInput = -pitchInput;

        _yaw = Mathf.Clamp(_yaw + yawInput * yawSpeed * Time.deltaTime, yawMin, yawMax);
        _pitch = Mathf.Clamp(_pitch + pitchInput * pitchSpeed * Time.deltaTime, pitchMin, pitchMax);

        // 축 정의:
        // - Yaw : YawPivot의 로컬 Y축 회전
        // - Pitch : PitchPivot의 로컬 X축 회전  (필요 시 아래 축 바꿔도 됨)
        var yawEuler = yawPivot.localEulerAngles;
        yawEuler.y = _yaw;
        yawPivot.localEulerAngles = yawEuler;

        var pitchEuler = pitchPivot.localEulerAngles;
        pitchEuler.x = _pitch;
        pitchPivot.localEulerAngles = pitchEuler;
    }
}
