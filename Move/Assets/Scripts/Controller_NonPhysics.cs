using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_NonPhysics : MonoBehaviour
{
    Vector3 input = Vector3.zero;
    Vector3 _velocity = Vector3.zero;

    [SerializeField, Range(0,100)]
    float moveSpeed = default;
    [SerializeField, Range(0, 100)]
    float maxAcceleration = default;
    [SerializeField, Range(0, 1)]
    float bounciness = default;
    [SerializeField]
    Rect field = new Rect(-5, -5, 10, 10);

    void Update()
    {
        input.x = Input.GetAxis("Horizontal");
        input.z = Input.GetAxis("Vertical");

        //입력값의 크기를 무조건 1로 고정
        //input.Normalize();

        //입력값을 0~1사이로 고정
        //만약 크기가 1을 초과할 경우만 1로 지정
        input = Vector3.ClampMagnitude(input, 1);
        Debug.Log(input);
    }

    private void FixedUpdate()
    {

        /*Vector3 velocity = input * moveSpeed;
        Vector3 displacement = velocity * Time.fixedDeltaTime
        transform.position += velocity;*/

        //가속(조종하기 어려움)
        /*Vector3 acceleration = input * moveSpeed;
        _velocity += acceleration * Time.fixedDeltaTime;
        transform.localPosition += _velocity * Time.fixedDeltaTime;*/


         Vector3 desireVelocity = input * moveSpeed;
         float currAccel = maxAcceleration * Time.fixedDeltaTime;
         //가속도가 원하는 속도보다 느리거나 빠를경우 매프레임마다 가속 속도만큼 속도가 변화한다. 
 /*        if (velocity.x < desireVelocity.x)
         {
             _velocity.x = Mathf.Min(_velocity.x+ currAccel, desireVelocity.x);
         }
         else if(_velocity.x > desireVelocity.x)
         {
             _velocity.x = Mathf.Max(_velocity.x - currAccel, desireVelocity.x);
         }*/

         //Mathf함수를 사용하여 쉽게 구현가능하다.
         _velocity.x = Mathf.MoveTowards(_velocity.x, desireVelocity.x, currAccel);
         _velocity.z = Mathf.MoveTowards(_velocity.z, desireVelocity.z, currAccel);

        // transform.localPosition += _velocity * Time.fixedDeltaTime; 

        //가속 미사용
        //Vector3 position = transform.position + input * Time.fixedDeltaTime * moveSpeed;
        
        //가속사용
        Vector3 position = _velocity * Time.fixedDeltaTime + transform.position;
        
        /* if (!field.Contains(new Vector2(position.x, position.z)))
         {
             //position = transform.localPosition;
             position.x = Mathf.Clamp(position.x, field.xMin, field.xMax);
             position.z = Mathf.Clamp(position.z, field.yMin, field.yMax);
         }*/

        //만약에 속도에 가속이 적용되어 있을 경우
        //해당 방향으로 가속이 계속 적용되서 반대 방향으로 벗어날때 그만큼 시간이 걸린다.
        //바로 벗어나기 위해서 해당 방향의 속도를 0으로 만든다.
/*        if (position.x < field.xMin)
        {
            position.x = field.xMin;
            _velocity.x = 0;
        }
        if (position.x > field.xMax)
        {
            position.x = field.xMax;
            _velocity.x = 0;
        }
        if (position.z < field.yMin)
        {
            position.z = field.yMin;
            _velocity.z = 0;
        }
        if (position.z > field.yMax)
        {
            position.z = field.yMax;
            _velocity.z = 0;
        }*/

        //또는 바운스를 줘서 해당 방향의 역으로 이동하게 할수도있다.
        if (position.x < field.xMin)
        {
            position.x = field.xMin;
            _velocity.x = -_velocity.x * bounciness;
        }
        if (position.x > field.xMax)
        {
            position.x = field.xMax;
            _velocity.x = -_velocity.x * bounciness;
        }
        if (position.z < field.yMin)
        {
            position.z = field.yMin;
            _velocity.z = -_velocity.z * bounciness;
        }
        if (position.z > field.yMax)
        {
            position.z = field.yMax;
            _velocity.z = -_velocity.z * bounciness;
        }

        transform.position = position;
    }
}
