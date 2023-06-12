using UnityEngine;
using UnityEngine.InputSystem;

namespace LidarProject
{
    public class BasicPlayerMovement : MonoBehaviour
    {
        [Header("Base Config")]
        [SerializeField] LayerMask groundMask;
        [SerializeField] bool useRunBool;

        [Header("Movement")]
        [SerializeField] float speed;
        [SerializeField] float sprintMultiplier;
        [SerializeField] float gravity;
        [SerializeField] float jumpHeight;
        
        [Header("Camera")]
        [SerializeField] float cameraSensitivity = 100f;
        
        [Header("Reference")]
        [SerializeField] Transform groundCheck;
        
        public PlayerInput Input { get => input; private set => input = value; }

        Vector2 inputDir, camAxis;
        Vector3 velocity;
        CharacterController controller;
        Camera cam;
        PlayerInput input;
        bool isGrounded, isRun, jump;
        float xRotation, actualSpeed, stepOffset, slopeLimit;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            cam = Camera.main;
            input = GetComponent<PlayerInput>();
            Cursor.lockState = CursorLockMode.Locked;
            stepOffset = controller.stepOffset;
            slopeLimit = controller.slopeLimit;
        }

        void Update()
        {
            HandleInput();
            Movement();
            CameraMovement();
        }

        void HandleInput() // Handle the input
        {
            // Camera movement
            camAxis = Input.actions["Look"].ReadValue<Vector2>() * cameraSensitivity * Time.deltaTime;

            // Movement
            inputDir = Input.actions["Move"].ReadValue<Vector2>();

            // Run
            if (useRunBool && isGrounded) // Player switch between on and off
            {
                if (Input.actions["Run"].triggered)
                    isRun = !isRun;
            }
            else if (!useRunBool && isGrounded) // Player must hold down to run
            {
                if (Input.actions["Run"].ReadValue<float>() > 0)
                    isRun = true;
                else
                    isRun = false;
            }

            // Jump
            if (Input.actions["Jump"].triggered && isGrounded)
                jump = true;

            // Cursor Lock
            if (Input.actions["CursorMode"].triggered)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                    Cursor.lockState = CursorLockMode.None;
                else
                    Cursor.lockState = CursorLockMode.Locked;
            }
        }

        void Movement() // Apply the global movement
        {
            // Check ground & applyed base gravity for smooth exit platform
            isGrounded = Physics.CheckSphere(groundCheck.position, 0.58f, groundMask);
            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;

            // Change the stepOffset and slope limit for avoid "climbing" if you don't have the jump distance
            controller.stepOffset = isGrounded ? stepOffset : 0f;
            controller.slopeLimit = isGrounded ? slopeLimit : 100f;

            // Control speed is run or not
            if (isGrounded)
                actualSpeed = isRun ? speed * sprintMultiplier : speed;
            
            // Horizontal movement
            Vector3 move = Vector3.Normalize(transform.right * inputDir.x + transform.forward * inputDir.y);
            controller.Move(move * actualSpeed * Time.deltaTime);

            // Jump
            if (jump)
            {
                jump = false;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Roof Collision
            if ((controller.collisionFlags & CollisionFlags.Above) != 0)
                velocity.y = -2f;
        }

        void CameraMovement() // Apply the camera movement
        {
            xRotation -= camAxis.y;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * camAxis.x);
        }
    }
}