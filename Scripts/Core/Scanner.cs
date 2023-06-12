using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

namespace LidarProject
{
    public class Scanner : MonoBehaviour
    {
        [Header("Scan Config")]
        [SerializeField] ScannerConfig scannerBaseConfig;   // Base Scanner Config
        [SerializeField] ScannerConfig scannerLineConfig;   // Line Scanner Config
        [SerializeField] ScannerConfig scannerSweepConfig;  // Sweep Scanner Config

        [Header("Sound Config")]
        [SerializeField] float scanVolume;                  // Scan Volume

        [Header("Reference")]
        [SerializeField] Transform start;                   // Starting position of the lasers
        [SerializeField] VisualEffect laserVFX;             // Laser VFX

        public PlayerInput Input { get => input; set => input = value; }

        ScanManager manager;
        Camera cam;
        AudioSource audioSource;
        PlayerInput input;
        bool inSweeping;
        float sweepHeight, targetRadius;

        // Graphics Buffer ( Store target list and add to the VFX buffer)
        List<Vector3> targetList;
        GraphicsBuffer positionBuffer;
        static readonly int VfxPositionBufferProperty = Shader.PropertyToID("TargetBuffer"); // Buffer name for each laser target
        int bufferInitialCapacity = 200; // Buffer size (Each position/color is equal to 1 place)



        void Start()
        {
            cam = Camera.main;
            audioSource = GetComponent<AudioSource>();
            input = GetComponent<PlayerInput>();
            manager = FindObjectOfType<ScanManager>();
            targetRadius = scannerBaseConfig.radius;
            CreateNewVFX();
        }

        bool isscancircle = false;
        public void GetKeyDown()
        {
            isscancircle = true;
           
        }
        public void GetKeyUp()
        {
            isscancircle = false;
           
        }

        public void AllScan()
        {
            inSweeping = true;
            sweepHeight = 1f;
        }
        void Update()
        {
            if(isscancircle)
            {
                ScanCircle();
            }

            if(isscanline)
            {
                ScanLine();
            }
            ScannerController();

            if (inSweeping)
                HandleSweeping();

            if (targetRadius != scannerBaseConfig.radius)
                scannerBaseConfig.radius = Mathf.Lerp(scannerBaseConfig.radius, targetRadius, scannerBaseConfig.scrollConfig.radiusSpeed);
        }

        void LateUpdate()
        {
            SetLaser();
        }
 
        void ScannerController() // Scanner controller
        {
            // Left Click Scan
            if (Input.actions["Scan"].ReadValue<float>() > 0 && !inSweeping)
            {
                ScanCircle();
                if (!audioSource.isPlaying)
                    StartCoroutine(manager.PlaySound(audioSource, scanVolume));
            }
            // Right Click Scan
            else if (Input.actions["ScanLine"].ReadValue<float>() > 0 && !inSweeping)
            {
                ScanLine();
                if (!audioSource.isPlaying)
                    StartCoroutine(manager.PlaySound(audioSource, scanVolume));
            }
            else if (audioSource.isPlaying && !inSweeping)
                StartCoroutine(manager.StopSound(audioSource));

            // Scan Sweep
            if (Input.actions["Sweep"].triggered && !inSweeping)
            {
                inSweeping = true;
                sweepHeight = 1f;
            }

            // Change scanner radius with scrolling
            var _axisPad = input.actions["ScrollPad"].ReadValue<float>();
            if (input.actions["ScrollPad"].triggered)
            {
                var _power = scannerBaseConfig.scrollConfig.radiusPower;

                // Invert
                if (scannerBaseConfig.scrollConfig.invertScroll)
                    _power = -_power;

                // Add radius
                if (_axisPad > 0)
                    targetRadius -= _power;
                else if (_axisPad < 0)
                    targetRadius += _power;

                // Change radius
                targetRadius = Mathf.Clamp(targetRadius, scannerBaseConfig.scrollConfig.minRadius, scannerBaseConfig.scrollConfig.maxRadius);
            }

            // Switch the color palette
            if (input.actions["Change Mode"].triggered)
            {
                manager.SwitchPalette();
            }
        }

        bool isFly = false;
        public void SetPlayerFly()
        {
            if(isFly)
            {
                isFly = false;
                transform.parent.position = new Vector3(transform.parent.position.x, 12, transform.parent.position.z);
            }
            else
            {
                isFly = true;
                transform.parent.position = new Vector3(transform.parent.position.x, 40, transform.parent.position.z);
            }

        }


        public void ChangeColor()
        {
             manager.SwitchPalette();
        }

        bool isscanline = false;
        public void GetGrabDown()
        {
            isscanline = true;
        }

        public void GetGrabUp()
        {
            isscanline = false;
        }

        public void AddPower()
        {
           targetRadius += 0.2f;
            targetRadius = Mathf.Clamp(targetRadius, scannerBaseConfig.scrollConfig.minRadius, scannerBaseConfig.scrollConfig.maxRadius);
        }

         public void ReducePower()
        {
           targetRadius -= 0.2f;
            targetRadius = Mathf.Clamp(targetRadius, scannerBaseConfig.scrollConfig.minRadius, scannerBaseConfig.scrollConfig.maxRadius);
        }

        void ScanCircle() // Launch scan circle
        {
            for (int i = 0; i < scannerBaseConfig.particleNumb; i++)
            {
                // Circle raycast on the screen
                RaycastHit _hit;
                Vector3 randomPoint;
                if (scannerBaseConfig.useSpread)
                    randomPoint = (Random.insideUnitSphere * scannerBaseConfig.radius * 10) + (-cam.transform.forward * 10) + cam.transform.position;
                else
                    randomPoint = (Random.insideUnitSphere.normalized * scannerBaseConfig.radius * 9.5f) + (-cam.transform.forward * 10) + cam.transform.position;
                Vector3 dir = (cam.transform.position - randomPoint).normalized;
                if (Physics.Raycast(cam.transform.position, dir, out _hit, scannerBaseConfig.range - (scannerBaseConfig.radius * scannerBaseConfig.rangeMultiplier)))
                    SetScan(_hit);
                else if (scannerBaseConfig.laserAwlaysVisible)
                    AddList(cam.transform.position + dir * (scannerBaseConfig.range - (scannerBaseConfig.radius * scannerBaseConfig.rangeMultiplier)));
            }
        }

        void ScanLine() // Launch scan line
        {
            for (int i = 0; i < scannerLineConfig.particleNumb; i++)
            {
                // Circle raycast on the screen
                var _pos = cam.pixelWidth / 2f;
                var _random = Random.Range(-scannerLineConfig.lineConfig.randomHeight, scannerLineConfig.lineConfig.randomHeight);
                RaycastHit _hit;
                Vector3 randomPoint = new Vector3(Random.Range(_pos - (scannerLineConfig.lineConfig.lineWidth * _pos), _pos + (scannerLineConfig.lineConfig.lineWidth * _pos)), (cam.pixelHeight / 2f) + _random, 0);
                Ray ray = cam.ScreenPointToRay(randomPoint);
                if (Physics.Raycast(cam.transform.position, ray.direction, out _hit, scannerLineConfig.range))
                    SetScan(_hit);
                else if (scannerLineConfig.laserAwlaysVisible)
                    AddList(cam.transform.position + ray.direction * scannerLineConfig.range);
            }
        }

        void ScanSweep() // Launch scan sweep
        {
            for (int i = 0; i < scannerSweepConfig.particleNumb; i++)
            {
                // Circle raycast on the screen
                var _pos = cam.pixelWidth / 2f;
                var _random = Random.Range(-scannerSweepConfig.lineConfig.randomHeight, scannerSweepConfig.lineConfig.randomHeight);
                RaycastHit _hit;
                Vector3 randomPoint = new Vector3(Random.Range(_pos - (scannerSweepConfig.lineConfig.lineWidth * _pos), _pos + (scannerSweepConfig.lineConfig.lineWidth * _pos)), (cam.pixelHeight * sweepHeight) + _random, 0);
                Ray ray = cam.ScreenPointToRay(randomPoint);
                if (Physics.Raycast(cam.transform.position, ray.direction, out _hit, scannerSweepConfig.range))
                    SetScan(_hit);
                else if (scannerSweepConfig.laserAwlaysVisible)
                    AddList(cam.transform.position + ray.direction * scannerSweepConfig.range);
            }
        }

        void HandleSweeping() // Sweep
        {
            sweepHeight -= scannerSweepConfig.lineConfig.sweepSpeed * Time.deltaTime;
            ScanSweep();
            if (sweepHeight <= 0)
                inSweeping = false;
            if (!audioSource.isPlaying)
                StartCoroutine(manager.PlaySound(audioSource, scanVolume));
        }

        void SetScan(RaycastHit _hit) // Send the scan to scan manager
        {
            manager.AddParticle(_hit);
            AddList(_hit.point);
        }

        void AddList(Vector3 _pos) // Add target to the list
        {
            targetList.Add(_pos);
        }

        void CreateNewVFX() // Create the vfx
        {
            laserVFX = Instantiate(laserVFX, start.transform);
            targetList = new List<Vector3>(bufferInitialCapacity);
            manager.EnsureBufferCapacity(ref positionBuffer, bufferInitialCapacity, 12, laserVFX, VfxPositionBufferProperty);
        }

        void SetLaser() // Set targets in VFX
        {
            if (targetList.Count <= 0)
                return;
            manager.EnsureBufferCapacity(ref positionBuffer, targetList.Count, 12, laserVFX, VfxPositionBufferProperty);
            positionBuffer.SetData(targetList);
            laserVFX.SetFloat("LifeTime", 0.05f);
            laserVFX.SetInt("Count", scannerBaseConfig.particleNumb / scannerBaseConfig.laserDivider);
            laserVFX.Play();
            targetList = new List<Vector3>(bufferInitialCapacity);
        }

        void OnDestroy() // Release on destroy
        {
            manager.ReleaseBuffer(ref positionBuffer);
        }
    }

    [System.Serializable]
    public class ScannerConfig
    {
        [Header("Global Scan")]
        [Range(1, 500)] public int particleNumb;    // Number of particle created
        [Range(1, 50)] public int laserDivider;     // Divide the number of lasers created by particle number
        public float range;                         // Range of the laser
        public bool laserAwlaysVisible;             // Laser always visible else only visible if laser hit

        [Header("Base Scan Only")]
        public float radius;                        // Scanner Radius
        public float rangeMultiplier;               // Multiplier of the range based on radius
        public bool useSpread;                      // Scanner spread or no
        public ScrollConfig scrollConfig;           // Scanner scroll config

        [Header("Line & Sweep Only")]
        public ScannerLineConfig lineConfig;
    }

    [System.Serializable]
    public class ScannerLineConfig
    {
        [Range(0f, 1f)] public float lineWidth;
        public float randomHeight;
        public float sweepSpeed;
    }

    [System.Serializable]
    public class ScrollConfig
    {
        public bool invertScroll;     // Invert the scroll
        public float radiusPower;     // Power of the radius for each scroll
        public float radiusSpeed;     // Radius speed
        public float minRadius;       // Minimum radius
        public float maxRadius;       // Maximum radius
    }

}