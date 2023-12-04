using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Controller_Physics : MonoBehaviour
{
    [SerializeField]
    LayerMask probeMask = -1;
    [SerializeField]
    LayerMask stairMask = -1;
    [SerializeField]
    Rigidbody rb;
    [SerializeField]
    Transform playerInputSpace = default;
    [SerializeField]
    TrailRenderer trailRenderer;

    Vector3 input = Vector3.zero;
    Vector3 inputMouse = Vector3.zero;
    Vector3 velocity = Vector3.zero;
    Vector3 desireVelocity = Vector3.zero;
    Vector3 contactNormal;
    Vector3 steepNormal;

    float minGroundDotProduct;
    float minObjectDotProduct;
    float currMouseSpeed = 0;

    bool desireJump = false;
    bool OnGround => groundContactCount>0;
    bool OnSteep => steepContactCount>0;

    int jumpPhase = 0;
    int groundContactCount = 0;
    int steepContactCount = 0;
    int stepsSinceLastGrounded = 0;
    int stepsSinceLastJump = 0;

    [SerializeField, Range(0, 100)]
    float MouseSpeed = default;
    [SerializeField, Range(0,100)]
    float moveSpeed = default;
    [SerializeField, Range(0, 10f)]
    float jumpHeight = default;
    [SerializeField, Min(0f)]
    float probeDistance = default;

    [SerializeField, Range(0, 100)]
    float maxMouseAcceleration = default;
    [SerializeField, Range(0, 100)]
    float maxAcceleration = default;
    [SerializeField, Range(0, 100)]
    float maxAirAcceleration = default;
    [SerializeField, Range(0, 90f)]
    float maxGroundAngle = default;
    [SerializeField, Range(0, 90f)]
    float maxObjectAngle = default;
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = default;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = default;

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minObjectDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        OnValidate();
    }

    void Update()
    {
        //입력
        input.x = Input.GetAxis("Horizontal");
        input.z = Input.GetAxis("Vertical");
        desireJump |= Input.GetButtonDown("Jump");

        inputMouse.x = Input.GetAxis("Mouse X");
        inputMouse.y = Input.GetAxis("Mouse Y");

        //입력값 변환
        input = Vector3.ClampMagnitude(input, 1);
        inputMouse = Vector3.ClampMagnitude(inputMouse, 1);

        if (inputMouse.magnitude != 0)
        {
            currMouseSpeed += inputMouse.x * MouseSpeed;
            currMouseSpeed = Mathf.Clamp(currMouseSpeed, -maxMouseAcceleration, maxMouseAcceleration);
        }
        else
        {
            currMouseSpeed = 0;
        }

        transform.Rotate(Vector3.up, currMouseSpeed);

        if (playerInputSpace)
        {
            Vector3 forward = playerInputSpace.forward;
            Vector3 right = playerInputSpace.right;
            forward.y = 0f;
            forward.Normalize();
            right.y = 0f;
            right.Normalize();

            desireVelocity = (forward*input.z + right*input.x) * moveSpeed;
        }
        else
        {

            desireVelocity = input * moveSpeed;
        }

        //디버그
        GetComponent<Renderer>().material.SetColor(
            "_Color", OnGround ? Color.black : Color.white
            //Color.white * (groundContactCount * 0.25f)
        );
    }

    private void FixedUpdate()
    {
        //상태 업데이트
        UpdateState();
        //속도 계산
        AdjustVelocity();

        //점프
        if (desireJump)
        {
            desireJump = false;
            Jump();
        }
        //이동 
        rb.velocity = velocity;
        
        //상태 초기화
        ClearState();
    }

 

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }


    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void UpdateState()
    {
        //마지막 그라운드에서 몇 프레임이 지났는지 저장하기 위한 변수
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        velocity = rb.velocity;

        //확실히 땅이거나, 땅에 붙어있을 경우
        if (OnGround || SnapToGround() || CheckSteepContacts())
        {
            //그라운드 변수 초기화
            //점프 횟수 초기화
            //만약 땅에 1개 이상 밟고 잇을 경우 단위 벡터로 초기화
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }

        //공중에 있을 경우
        else
        {
            //접촉 방향 Vector3.up(평지)
            contactNormal = Vector3.up;
        }
    }

    void AdjustVelocity()
    {
        //기준이 되는 축을 해당 각도로 올린다.
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        //이동 방향을 현재 기울기에 따라 힘 조절한다. 
        float currX = Vector3.Dot(velocity, xAxis);
        float currZ = Vector3.Dot(velocity, zAxis);

        //가속
        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        //원하는 속도로 가속/감속한다.
        float newX = Mathf.MoveTowards(currX, desireVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currZ, desireVelocity.z, maxSpeedChange);

        //새로운 속도와 현재 속도의 차이만큼 가속 시킨다.
        velocity += xAxis * (newX - currX) + zAxis * (newZ - currZ);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        //vector를 normal의 각도 만큼 Projection 한다.
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    void Jump()
    {
        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 &&jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }

        jumpDirection = (jumpDirection + Vector3.up).normalized;


        //땅에 있거나, 연속 점프 기회가 남아있을 경우
        //if (OnGround || jumpPhase < maxAirJumps)
        {
            stepsSinceLastJump = 0;
            jumpPhase += 1;

            //Root(-2*g*h) = 점프 속도
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            //float alignSpeed = Vector3.Dot(velocity, contactNormal);
            float alignSpeed = Vector3.Dot(velocity, jumpDirection);
            //계산전에 항상 이전 속도를 뺀다.
            //만약 이전 속도가 점프속도보다 빠를경우를 대비하여 0으로 빠르게 떨어질 경우 잠깐 멈추게 만든다.
            if (alignSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignSpeed, 0f);
            }
            //velocity.y += jumpSpeed;
            //velocity += contactNormal * jumpSpeed;
            velocity += jumpDirection * jumpSpeed;
        }
    }
    void EvaluateCollision(Collision collision)
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        //모든 contact를 검사하여 일정 각도 이상의 평면을 모두 저장한다.
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            //onGround |= normal.y >= minGroundDotProduct;

            //cos에서 y값은 1->-1로 가므로 높을수록 각도는 낮은 각도
            if (normal.y >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {

                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }

    private void ClearState()
    {
        //매 프레임 마다 초기화한다.(FixedUpdate의 마지막)
        //행동(fixedUpdate)->초기화(fixedUpdate)->입력(update)->충돌처리(oncollision)->...
        //땅
        groundContactCount = 0;
        contactNormal = Vector3.zero;
        //가파른 경사
        steepContactCount = 0;
        steepNormal = Vector3.zero;
    }

    bool SnapToGround()
    {
        //만약 땅에서 벗어나고 2프레임 이상 진행 됐을경우
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump<=2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        //만약 현재 속도가 일정 속도 이상이라면
        if(speed> maxSnapSpeed)
        {
            return false;
        }

        //땅으로 레이를 쐈을 때 hit되지않을 경우
        if(!Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask))
        {
            return false;
        }

        //hit한 곳이 유효하지 않은 경사일경우(maxSlopeAngle을 넘길경우)
        if(hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }

        //1프레임까지는 붙어있다고 판단.
        groundContactCount = 1;
        contactNormal = hit.normal;

        float dot = Vector3.Dot(velocity, hit.normal);

        //만약 velocity가 바닥을 향하고 있다면 더 느려지는 경우가 있기 때문에 이 경우를 제외한다.
        if (dot > 0f)
        {
            //velocity를 사영하여 각도를 바꾸고 magnitude를 곱해서 동일한 힘을 준다.
           velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        return true;
    }

    float GetMinDot(int layer)
    {
        return (stairMask & (1 << layer)) == 0 ? minGroundDotProduct : minObjectDotProduct;
    }

    bool CheckSteepContacts()
    {
        //만약 가파른 경사를 2개 이상 얻고있다면
        //가상의 경사를 만든다.
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

}
