using UnityEngine;
using UnityEngine.UI;

namespace LidarProject
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("FrameRate")]
        [SerializeField] bool displayFrameRate;
        [SerializeField] int maximumFrameRate;
        [Header("Particle Count")]
        [SerializeField] bool displayParticleCount;
        [SerializeField] int particleCount;
        [Header("Reference")]
        [SerializeField] Text fpsText;
        [SerializeField] Text particleText;
        
        public int ParticleCount { get => particleCount; set => particleCount = value; }
        public GameObject Player { get => player; set => player = value; }

        // Private //
        GameObject player;
        private float time;
        private int frameCount;

        private void Awake()
        {
            instance = this;
            player = FindObjectOfType<BasicPlayerMovement>().gameObject;
            if (maximumFrameRate != 0)
                Application.targetFrameRate = maximumFrameRate;
        }

        private void Update()
        {
            DisplayFrameRate();
            DisplayParticlesCount();
        }

        private void DisplayFrameRate() // Display the framerate
        {
            if (fpsText == null)
                return;
            
            // Display or Hide the Fps count
            if (displayFrameRate && !fpsText.IsActive())
                fpsText.enabled = true;
            else if(!displayFrameRate && fpsText.IsActive())
            {
                fpsText.enabled = false;
                return;
            }

            time += Time.deltaTime;
            frameCount++;

            if (time >= 1)
            {
                var _frameRate = Mathf.RoundToInt(frameCount / time);
                fpsText.text = _frameRate.ToString() + " FPS";

                time -= 1;
                frameCount = 0;
            }
        }

        void DisplayParticlesCount() // Display the Particles Count
        {
            if (particleText == null)
                return;
            
            // Display or Hide the Fps count
            if (displayParticleCount && !particleText.IsActive())
                particleText.enabled = true;
            else if (!displayParticleCount && particleText.IsActive())
            {
                particleText.enabled = false;
                return;
            }

            particleText.text = particleCount.ToString() + " particles";
        }
    }
}