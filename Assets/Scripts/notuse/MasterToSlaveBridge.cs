using UnityEngine;

public class MasterToSlaveBridge : MonoBehaviour
{
    [Header("Master source")]
    public Transform masterHandle;            // 마스터 핸들(말단)
    public Transform masterAnchor;            // (선택) 마스터 좌표계 기준점

    [Header("Slave targets (IKwithTWEE)")]
    public Transform slaveEETarget;           // 슬레이브 ee_Target
    public Transform slaveEETargetR;          // (선택) 슬레이브 ee_Target_R
    public Transform slaveEETargetL;          // (선택) 슬레이브 ee_Target_L
    public Transform slaveAnchor;             // (선택) 슬레이브 좌표계 기준점

    [Header("Pose mapping")]
    public Vector3 positionOffsetLocal = Vector3.zero; // 마스터 핸들 로컬에서의 추가 위치 오프셋
    public Vector3 rotationOffsetEuler = Vector3.zero; // 추가 회전 오프셋(도)
    public bool useAnchors = false;                     // 좌표계를 앵커 기준으로 변환할지
    public bool mirrorX = false, mirrorY = false, mirrorZ = false; // 축 반전 옵션

    [Header("Smoothing")]
    [Range(0f, 1f)] public float posLerp = 1f;          // 1=즉시, 0.2~0.5=부드럽게
    [Range(0f, 1f)] public float rotSlerp = 1f;         // 1=즉시

    [Header("Gripper driving")]
    public Transform masterGripR;   // (선택) 마스터에서 오른쪽 프록시 포인트(있으면 우선 사용)
    public Transform masterGripL;   // (선택) 마스터에서 왼쪽 프록시 포인트
    public float gripHalfWidth = 0.05f; // 프록시 없을 때 핸들.right 기준 반폭(미터)

    void Reset()
    {
        posLerp = rotSlerp = 1f;
    }

    void Update()
    {
        if (!masterHandle || !slaveEETarget) return;

        // 1) 원본 포즈 읽기
        Vector3 srcPos = masterHandle.position;
        Quaternion srcRot = masterHandle.rotation;

        // 로컬 오프셋을 핸들 로컬축 기준으로 더함
        srcPos += masterHandle.TransformVector(positionOffsetLocal);
        srcRot = srcRot * Quaternion.Euler(rotationOffsetEuler);

        // 2) (선택) 앵커 기준 변환
        if (useAnchors && masterAnchor && slaveAnchor)
        {
            // master local
            Vector3 localP = masterAnchor.InverseTransformPoint(srcPos);
            Quaternion localR = Quaternion.Inverse(masterAnchor.rotation) * srcRot;

            // (선택) 축 반전
            if (mirrorX || mirrorY || mirrorZ)
            {
                localP = new Vector3(
                    mirrorX ? -localP.x : localP.x,
                    mirrorY ? -localP.y : localP.y,
                    mirrorZ ? -localP.z : localP.z
                );

                Vector3 e = localR.eulerAngles;
                e = new Vector3(
                    mirrorX ? -e.x : e.x,
                    mirrorY ? -e.y : e.y,
                    mirrorZ ? -e.z : e.z
                );
                localR = Quaternion.Euler(e);
            }

            // slave world
            srcPos = slaveAnchor.TransformPoint(localP);
            srcRot = slaveAnchor.rotation * localR;
        }

        // 3) 슬레이브 ee_Target에 복제 (부드럽게)
        if (posLerp >= 1f) slaveEETarget.position = srcPos;
        else slaveEETarget.position = Vector3.Lerp(slaveEETarget.position, srcPos, posLerp);

        if (rotSlerp >= 1f) slaveEETarget.rotation = srcRot;
        else slaveEETarget.rotation = Quaternion.Slerp(slaveEETarget.rotation, srcRot, rotSlerp);

        // 4) 그립(R/L) 타깃 세팅
        if (slaveEETargetR && slaveEETargetL)
        {
            Vector3 rPos, lPos;

            if (masterGripR && masterGripL)
            {
                // 마스터에 프록시 포인트가 이미 있는 경우 → 그대로 복제
                rPos = masterGripR.position;
                lPos = masterGripL.position;

                if (useAnchors && masterAnchor && slaveAnchor)
                {
                    Vector3 rLoc = masterAnchor.InverseTransformPoint(rPos);
                    Vector3 lLoc = masterAnchor.InverseTransformPoint(lPos);

                    if (mirrorX || mirrorY || mirrorZ)
                    {
                        rLoc = new Vector3(mirrorX ? -rLoc.x : rLoc.x, mirrorY ? -rLoc.y : rLoc.y, mirrorZ ? -rLoc.z : rLoc.z);
                        lLoc = new Vector3(mirrorX ? -lLoc.x : lLoc.x, mirrorY ? -lLoc.y : lLoc.y, mirrorZ ? -lLoc.z : lLoc.z);
                    }

                    rPos = slaveAnchor.TransformPoint(rLoc);
                    lPos = slaveAnchor.TransformPoint(lLoc);
                }
            }
            else
            {
                // 프록시가 없으면: 핸들 기준 right축으로 폭 합성
                Vector3 right = srcRot * Vector3.right;
                rPos = srcPos + right * gripHalfWidth;
                lPos = srcPos - right * gripHalfWidth;
            }

            if (posLerp >= 1f)
            {
                slaveEETargetR.position = rPos;
                slaveEETargetL.position = lPos;
            }
            else
            {
                slaveEETargetR.position = Vector3.Lerp(slaveEETargetR.position, rPos, posLerp);
                slaveEETargetL.position = Vector3.Lerp(slaveEETargetL.position, lPos, posLerp);
            }
        }
    }
}
