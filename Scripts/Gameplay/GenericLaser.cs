using UnityEngine;
using UnityEngine.VFX;

namespace LidarProject
{
    public class GenericLaser : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField, Range(1, 200)] int laserNumb;          // Number of laser created
        [SerializeField, Range(1, 10)] int laserDivider;        // Divide the number of lasers created by Laser number
        [SerializeField] float laserRange;                      // Range of the laser
        [SerializeField, Range(0.01f, 0.5f)] float laserRadius; // Laser Radius
        [SerializeField, Range(0f, 0.05f)] float laserCooldown; // Laser Cooldown
        [SerializeField] bool alwaysVisible;                    // Laser always visible else only visible if laser hit
        [SerializeField] bool activated;

        [Header("Sound Config")]
        [Tooltip("Scan Volume")] public float scanVolume;       // Scan Volume

        [Header("Reference")]
        [SerializeField] VisualEffect VFX;

        ScanManager manager;

        private void Start()
        {
            manager = FindObjectOfType<ScanManager>();
        }
        
        void Update()
        {
            if (activated)
                Scan();
        }

        void Scan() // Check if can scan
        {
            for (int i = 0; i < laserNumb; i++)
                LaunchLaser();
        }

        void LaunchLaser() // Launch laser
        {
            RaycastHit hit;
            Vector3 randomPoint = (Random.insideUnitSphere * laserRadius * 10) + (-transform.forward * 10) + transform.position;
            Vector3 dir = (transform.position - randomPoint).normalized;
            if (Physics.Raycast(transform.position, dir, out hit, laserRange - laserRadius))
            {
                manager.AddParticle(hit);
                manager.SetLaser(VFX, hit.point, laserCooldown, Color.white, false);
            }
            else if (alwaysVisible)
                manager.SetLaser(VFX, transform.position + dir * laserRange, laserCooldown, Color.white, false);
        }
    }
}