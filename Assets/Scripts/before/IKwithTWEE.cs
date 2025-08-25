/*
이 코드는 로봇의 팔을 원하는 위치로 움직이기 위한 계산을 수행합니다. 
CalcIK 함수는 이 모든 계산을 통합하여 실행하는 주요 함수입니다.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra.Single;


public class IKwithTWEE : MonoBehaviour
{
    //목표 위치
    public GameObject ee_Target;
    // 로봇과 관련된 변수들
    private GameObject[] joint = new GameObject[6];
    private float[] angle = new float[5];
    private float[] prevAngle = new float[5];
    private Vector3[] dim = new Vector3[5];             // local dimensions of each Joint
    private Vector3[] point = new Vector3[6];           // world position of joint end
    private Vector3[] axis = new Vector3[5];            // local direction of each axis
    private Quaternion[] rotation = new Quaternion[5];  // local rotation(quaternion) of joint relative to its parent
    private Quaternion[] wRotation = new Quaternion[5]; // world rotation(quaternion) of joint
    private Vector3 pos;                                // reference(target) position
    private Vector3 rot;                                // reference(target) pose
    private float lambda = 0.1f;
    private float[] minAngle = new float[5];            // limits of joint rotatation
    private float[] maxAngle = new float[5];
    private float forpris = new float();


    public GameObject ee_Target_R;
    public GameObject ee_Target_L;
    private GameObject[] twee = new GameObject[5];
    private float[] angle_twee = new float[5];
    private float[] prevAngle_twee = new float[5];
    private Vector3[] dim_twee = new Vector3[4];// local dimensions of each Joint
    private Vector3[] point_twee = new Vector3[5];
    private Vector3 axis_twee = new Vector3(0f, 1f, 0f);
    private Quaternion[] rotation_twee = new Quaternion[5];
    private Quaternion[] wRotation_twee = new Quaternion[3];
    private Vector3 pos_R, pos_L;                                // reference(target) position
    private Vector3 rot_R, rot_L;
    private float[] length = new float[2];
    private float length_goal, length_target, length_now = new float();
    private float angle_goal = new float();
    private float angle_now = new float();

    Renderer sr_R;
    Renderer sr_L;
    public GameObject Link_6_R;
    public GameObject Link_6_L;


    /*
게임이나 시뮬레이션을 시작할 때 로봇 팔의 초기 설정을 합니다. 
로봇 팔의 각 관절과 사용자가 조종할 수 있는 도구들을 찾아서 리스트에 추가하고, 
각 관절의 크기, 회전 축, 초기 회전 각도, 회전할 수 있는 최대/최소 각도를 설정합니다.
*/
    void Start()
    {
        // 로봇의 관절을 찾아서 리스트에 추가합니다.
        for (int i = 0; i < joint.Length; i++)
        {
            joint[i] = GameObject.Find("Axis_" + i.ToString());
        }

        // 각 관절의 크기와 회전 축을 설정합니다.
        dim[0] = new Vector3(-0.566f, 0f, 0f);
        dim[1] = new Vector3(0f, -1.263f, 0f);
        dim[2] = new Vector3(0f, -0.1066998f, 0f);
        dim[3] = new Vector3(-0.03949997f, 0f, 0f);
        dim[4] = new Vector3(-0.04258f, 0f, 0f);



        // IK5의 관절 회전방향: [x z y z]
        axis[0] = new Vector3(1f, 0f, 0f);
        axis[1] = new Vector3(0f, 0f, 1f);
        axis[2] = new Vector3(0f, 1f, 0f); // -y방향으로 평행이동함
        axis[3] = new Vector3(0f, 0f, 1f);
        axis[4] = new Vector3(1f, 0f, 0f);


        // 각 관절의 초기 회전 각도를 설정합니다. 초기 관절자세
        angle[0] = 0f;
        angle[1] = 0f;
        angle[2] = 0f;
        angle[3] = 0f;
        angle[4] = 0f;

        // 각 관절이 회전할 수 있는 최대, 최소 각도를 설정합니다.
        minAngle[0] = -30f;
        maxAngle[0] = 30f;
        minAngle[1] = -20f;
        maxAngle[1] = 90f;
        minAngle[2] = -176;
        maxAngle[2] = 176;
        minAngle[3] = -110f;
        maxAngle[3] = 180f;
        minAngle[4] = -180f;
        maxAngle[4] = 180f;

        forpris = 0;

        twee[0] = GameObject.Find("Axis_5");
        twee[1] = GameObject.Find("Axis_5_R");
        twee[2] = GameObject.Find("Axis_5_L");
        twee[3] = GameObject.Find("Axis_6_R");
        twee[4] = GameObject.Find("Axis_6_L");

        for (int i = 0; i < twee.Length; i++)
        {
            if (twee[i] == null)
            {
                Debug.LogError($"twee[{i}]가 null입니다. GameObject 이름을 확인하세요.");
            }
            else
            {
                Debug.Log($"twee[{i}]가 정상적으로 할당되었습니다: {twee[i].name}");
            }
        }

        dim_twee[0] = new Vector3(-0.004520106f, 0f, 0.02950001f);  //R
        dim_twee[1] = new Vector3(-0.00297f, 0f, -0.0332f);  //L
        dim_twee[2] = new Vector3(-0.0732f, 0f, 0.0479f);
        dim_twee[3] = new Vector3(-0.0696f, 0f, -0.0533f);

        angle_twee[0] = 0f;
        angle_twee[1] = 0f;
        angle_twee[2] = 0f;
        angle_twee[3] = 0f;
        angle_twee[4] = 0f;

        length[0] = Vector3.Distance(twee[1].transform.position, twee[2].transform.position);
        length[1] = Vector3.Distance(twee[1].transform.position, twee[3].transform.position);
        //Debug.Log(twee[1].transform.position);
        //Debug.Log(twee[2].transform.position);


        Link_6_R = GameObject.Find("Link_6_R");
        Link_6_L = GameObject.Find("Link_6_L");
        sr_R = Link_6_R.GetComponent<Renderer>();
        sr_L = Link_6_L.GetComponent<Renderer>();




    }

    /*
    게임이나 시뮬레이션 중에 사용자가 움직인 슬라이더의 값을 읽어와서 
    로봇 팔의 위치와 회전을 설정합니다. 그리고 IK 기법을 사용해서 로봇 팔을 움직입니다.
    */
    void Update()
    {

        // 목표 위치를 EE_Target에서 읽어옵니다.
        pos.x = ee_Target.transform.position.x;
        pos.y = ee_Target.transform.position.y;
        pos.z = ee_Target.transform.position.z;
        rot.x = ee_Target.transform.eulerAngles.x;
        rot.y = ee_Target.transform.eulerAngles.y;
        rot.z = ee_Target.transform.eulerAngles.z;

        pos_R = ee_Target_R.transform.position;
        pos_L = ee_Target_L.transform.position;


        // IK 기법을 사용해서 로봇 팔을 움직입니다.
        CalcIK();
        //Debug.Log(wRotation[4]);
        CalcIK_twee();
    }

    /*
    로봇의 팔이 원하는 위치와 방향으로 어떻게 움직일지 계산합니다.
    100번 동안 반복하여 팔의 각도를 조절하며 최선의 위치를 찾아나갑니다.
    팔의 각도가 허용 범위를 벗어나거나 100번 안에 원하는 위치를 찾지 못하면, 초기 설정으로 되돌립니다.
    */
    void CalcIK()
    {
        int count = 0;
        bool outOfLimit = false;
     
        // 100번 동안 계속 팔을 움직여서 손의 위치를 찾습니다.
        for (int i = 0; i < 100; i++)   // iteration
        {
            count = i;
            // 손의 위치를 찾아봅니다.
            ForwardKinematics();

            // 손이 얼마나 잘못된 위치에 있는지 확인합니다.
            var err = CalcErr();    // 6x1 matrix(vector)
            float err_norm = (float)err.L2Norm();
            // 만약 손이 거의 정확한 위치에 있다면, 이제 팔을 움직이지 않아도 됩니다.
            if (err_norm < 1E-3)
            {
                for (int ii = 0; ii < joint.Length - 1; ii++)
                {
                    // 하지만 팔의 각도가 너무 크다면 문제가 생깁니다!
                    if (angle[ii] < minAngle[ii] || angle[ii] > maxAngle[ii])
                    {
                        outOfLimit = true;
                        sr_R.material.color = Color.red;
                        sr_L.material.color = Color.red;
                        break;
                    }
                    else 
                    {   sr_R.material.color = new Color(249/255f,160/255f, 237/255f, 255/255f);
                        sr_L.material.color = new Color(186 / 255f, 252 / 255f, 192 / 255f, 255 / 255f);
                    }
                }
                break;
            }

            // 팔의 각도를 얼마나 조절해야 하는지 계산합니다.
            var J = CalcJacobian(); // 6x6 matrix



            // 관절 각도 수정
            var dAngle = lambda * J.PseudoInverse() * err; // 6x1 matrix
            angle[0] += dAngle[0, 0] * Mathf.Rad2Deg;
            angle[1] += dAngle[1, 0] * Mathf.Rad2Deg;

            forpris = dAngle[2, 0];
            dim[1] += forpris * axis[2];

            angle[2] += dAngle[3, 0] * Mathf.Rad2Deg;
            angle[3] += dAngle[4, 0] * Mathf.Rad2Deg;
            angle[4] += dAngle[5, 0] * Mathf.Rad2Deg;


            /*for (int ii = 0; ii < 4; ii++)
            {
                angle[ii] += dAngle[ii, 0] * Mathf.Rad2Deg;
            }  */

        }

        // 만약 100번 안에 손의 위치를 제대로 못 찾았거나, 팔의 각도가 너무 크다면,
        // 처음부터 다시 시도합니다.
        if (count == 99 || outOfLimit == true)  // did not converge or angle out of limit
        {
            for (int i = 0; i < joint.Length-1; i++) // reset slider
            {
                Debug.Log("NG");

                angle[i] = prevAngle[i];
            }
        }
        // 아니면 새로 계산한 팔의 각도로 로봇을 움직입니다. 
        else
        {
            for (int i = 0; i < 2; i++)
            {
                rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                joint[i].transform.localRotation = rotation[i];
                prevAngle[i] = angle[i];
            }

            joint[2].transform.position = (joint[1].transform.position + rotation[0] * rotation[1] * dim[1]);
            joint[2].transform.localRotation = rotation[2];

            for (int i = 3; i < 5; i++)
            {
                rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                joint[i].transform.localRotation = rotation[i];
                prevAngle[i] = angle[i];
            }


            /*Debug.Log(dim[2] + "*");
            Debug.Log(joint[1].transform.position);
                Debug.Log(joint[2].transform.position);
            Debug.Log(rotation[0]);
            Debug.Log(rotation[1]);
            Debug.Log(rotation[2]);*/

        }
    }

    void CalcIK_twee()
    {

        length_now = Vector3.Distance(twee[3].transform.position, twee[4].transform.position);
        length_now = Mathf.Clamp(length_now, 0.01f, 0.13f);
        length_target = Mathf.Clamp(Vector3.Distance(pos_R, pos_L), 0.01f, 0.13f);
        length_goal = Mathf.Clamp(length_target * (1), 0.01f, 0.13f);

        angle_now = Mathf.Acos((length_now - length[0]) / (2 * length[1])) * Mathf.Rad2Deg;
        angle_now = Mathf.Clamp(angle_now, 0f, 120f);
        angle_goal = Mathf.Acos((length_goal - length[0]) / (2 * length[1])) * Mathf.Rad2Deg;
        angle_goal = Mathf.Clamp(angle_goal, 0f, 120f);

        //Debug.Log(angle_now + " " + angle_goal);
        
        angle_twee[1] += -(angle_goal - angle_now);
        //angle_twee[1] = Mathf.Clamp(angle_twee[1], -30f, 30f);
        angle_twee[2] = -angle_twee[1];
        angle_twee[3] = -angle_twee[1];
        angle_twee[4] = -angle_twee[2];
        //Debug.Log(angle_twee[1]);

        // 아니면 새로 계산한 팔의 각도로 로봇을 움직입니다. 

        for (int i = 1; i < 5; i++)
        {
            rotation_twee[i] = Quaternion.AngleAxis(angle_twee[i], axis_twee);
            twee[i].transform.localRotation = rotation_twee[i];
            prevAngle_twee[i] = angle_twee[i];
        }

        
    }

    /*
    현재의 팔의 각도를 기반으로 로봇의 손의 위치를 계산합니다.
    팔의 각 부분을 연결하며 손의 최종 위치를 찾아나갑니다.
    */
    void ForwardKinematics()
    {
        point[0] = joint[0].transform.position;
        wRotation[0] = Quaternion.AngleAxis(angle[0], axis[0]);

        for (int i = 1; i < 5; i++) //angle[2]값은 0으로 고정되어있음, dim[1]값은 업데이트됨
        {
            point[i] = wRotation[i - 1] * dim[i - 1] + point[i - 1];
            rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
            wRotation[i] = wRotation[i - 1] * rotation[i];
        }

        point[5] = wRotation[4] * dim[4] + point[4];


        /*for (int i = 1; i < joint.Length; i++)
        {
            point[i] = wRotation[i - 1] * dim[i - 1] + point[i - 1];
            rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
            wRotation[i] = wRotation[i - 1] * rotation[i];
        }
        point[joint.Length] = wRotation[joint.Length - 1] * dim[joint.Length - 1] + point[joint.Length - 1];*/
    }
 

    /*
    로봇의 손이 원하는 위치와 현재 위치 사이의 오차를 계산합니다.
    위치 오차와 방향 오차를 모두 포함하여 오차를 반환합니다.
    */
    DenseMatrix CalcErr()
    {
        // position error
        Vector3 perr = pos - point[5];
        // pose error
        Quaternion rerr = Quaternion.Euler(rot) * Quaternion.Inverse(wRotation[4]);
        // make error vector
        Vector3 rerrVal = new Vector3(rerr.eulerAngles.x, rerr.eulerAngles.y, rerr.eulerAngles.z);
        if (rerrVal.x > 180f) rerrVal.x -= 360f;
        if (rerrVal.y > 180f) rerrVal.y -= 360f;
        if (rerrVal.z > 180f) rerrVal.z -= 360f;
        var err = DenseMatrix.OfArray(new float[,]
        {
                { perr.x },
                { perr.y },
                { perr.z },
                { rerrVal.x * Mathf.Deg2Rad},
                { rerrVal.y * Mathf.Deg2Rad},
                { rerrVal.z * Mathf.Deg2Rad}
        });
        return err;
    }


    /*
    팔의 움직임과 손의 위치 사이의 관계를 나타내는 행렬을 계산합니다.
    이 행렬을 사용하여 손의 위치 오차를 최소화하는 팔의 움직임을 계산합니다.
    */
    DenseMatrix CalcJacobian()
    {
        // 여기서는 각 팔의 부분에 대한 계산을 합니다.
        Vector3 w0 = wRotation[0] * axis[0];
        Vector3 w1 = wRotation[1] * axis[1];
        Vector3 w2 = new Vector3(0f, 0f, 0f);
        Vector3 w3 = wRotation[2] * axis[2];
        Vector3 w4 = wRotation[3] * axis[3];
        Vector3 w5 = wRotation[4] * axis[4];


        Vector3 p0 = Vector3.Cross(w0, point[5] - point[0]);
        Vector3 p1 = Vector3.Cross(w1, point[5] - point[1]);
        Vector3 p2 = wRotation[2] * axis[2];
        Vector3 p3 = Vector3.Cross(w3, point[5] - point[2]);
        Vector3 p4 = Vector3.Cross(w4, point[5] - point[3]);
        Vector3 p5 = Vector3.Cross(w5, point[5] - point[4]);



        var J = DenseMatrix.OfArray(new float[,]
        {
                
                /* 각 팔의 부분에 대한 계산 결과를 행렬로 만듭니다.*/
                { p0.x, p1.x, p2.x, p3.x, p4.x, p5.x },
                { p0.y, p1.y, p2.y, p3.y, p4.y, p5.y },
                { p0.z, p1.z, p2.z, p3.z, p4.z, p5.z },
                { w0.x, w1.x, w2.x, w3.x, w4.x, w5.x },
                { w0.y, w1.y, w2.y, w3.y, w4.y, w5.y },
                { w0.z, w1.z, w2.z, w3.z, w4.z, w5.z }
        });
        return J;

    }

}
