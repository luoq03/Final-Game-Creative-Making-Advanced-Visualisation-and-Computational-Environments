using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace LidarProject
{
    public class ScanManager : MonoBehaviour
    {
        [Header("Global")]
        [SerializeField] Texture2D particlesTexture;                    // The texture of the particle
        [SerializeField] Vector2 particlesSize;                         // Set the Particles Size
        [SerializeField] LayerMask laserMask;                           // Lets you know which object should apply the particles
        [SerializeField] bool scanUseNormal;                            // Particles Follow Normals

        [Header("Fade")]
        [SerializeField] bool scanUseFade;                              // Use Particles Fade
        [SerializeField] AnimationCurve fadeCurve;                      // Set the curve of the particles fade
        [SerializeField] Vector2 fadeDistanceMinMax;                    // Set the distance between the particles and the player
        [SerializeField] float fadeMinimumAlpha;                        // Set the minimum alpha of the particles

        [Header("Color")]
        [SerializeField] int selectedPalette;                           // Set the color palette used ( Switch with X )
        [SerializeField] public ParticlesColorConfig particlesConfig;   // Color palette for change the color of particles
        
        [Header("Reference")]
        [SerializeField] public VisualEffectAsset vfx;                  // The VFX reference

        // All VFX list created at runtime
        List<VisualEffect> vfxs = new List<VisualEffect>();
        VisualEffect currentVFX;

        // Graphics Buffer ( Store position and color list and add to the VFX buffer)
        List<Vector3> positionList;
        List<Vector3> normalList;
        List<Color> colorList;
        GraphicsBuffer positionBuffer;
        GraphicsBuffer normalBuffer;
        GraphicsBuffer colorBuffer;
        static readonly int VfxPositionBufferProperty = Shader.PropertyToID("PositionBuffer");  // Buffer name for each particle position
        static readonly int VfxNormalBufferProperty = Shader.PropertyToID("NormalBuffer");      // Buffer name for each particle normal
        static readonly int VfxColorBufferProperty = Shader.PropertyToID("ColorBuffer");        // Buffer name for each particle color
        int bufferInitialCapacity = 500;                                                        // Buffer size (Each position/color is equal to 1 place)


        void Start()
        {
            InitScan();
        }

        void LateUpdate()
        {
            if (currentVFX.aliveParticleCount >= 10000000)
                CreateNewVFX();
            else if (positionList.Count > 0)
                SetParticle();

            if (scanUseFade) currentVFX.SetVector3("PlayerPos", GameManager.instance.Player.transform.position);
        }

        public void SwitchPalette() // Switch the color palette
        {
            selectedPalette++;
            if (selectedPalette > particlesConfig.colorPalette.Count)
                selectedPalette = 1;
        }
        
        void SetParticle() // Set Particles in VFX
        {
            EnsureBufferCapacity(ref positionBuffer, positionList.Count, 12, currentVFX, VfxPositionBufferProperty);
            EnsureBufferCapacity(ref normalBuffer, normalList.Count, 12, currentVFX, VfxNormalBufferProperty);
            EnsureBufferCapacity(ref colorBuffer, colorList.Count, 16, currentVFX, VfxColorBufferProperty);
            positionBuffer.SetData(positionList); // Add the position list in the buffer
            normalBuffer.SetData(normalList); // Add the normal list in the buffer
            colorBuffer.SetData(colorList); // Add the color list in the buffer
            currentVFX.SetInt("Count", positionList.Count); // Set the number of particle who should be created
            currentVFX.Play(); // Create the particles in the game
            ResetList(); // Reset the list for the loop
        }

        void InitScan() // Create VFX and set buffer
        {
            CreateNewVFX();
            UpdateVFXProperties(currentVFX);
            EnsureBufferCapacity(ref positionBuffer, bufferInitialCapacity, 12, currentVFX, VfxPositionBufferProperty);
            EnsureBufferCapacity(ref normalBuffer, bufferInitialCapacity, 12, currentVFX, VfxNormalBufferProperty);
            EnsureBufferCapacity(ref colorBuffer, bufferInitialCapacity, 16, currentVFX, VfxColorBufferProperty);
        }

        public void EnsureBufferCapacity(ref GraphicsBuffer buffer, int capacity, int stride, VisualEffect vfx, int vfxProperty) // Check the capacity of buffer and release if full
        {
            // Reallocate new buffer only when null or capacity is not sufficient
            if (buffer == null || buffer.count < capacity)
            {
                // Buffer memory must be released
                buffer?.Release();
                // Vfx Graph uses structured buffer
                buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, stride);
                // Update buffer referenece
                vfx.SetGraphicsBuffer(vfxProperty, buffer);
            }
        }

        public void AddParticle(RaycastHit _hit) // Add particle to the list
        {
            if (laserMask == (laserMask | (1 << _hit.transform.gameObject.layer))) // If the hit object have correct layer
            {
                var _type = GetParticleTag(_hit);
                positionList.Add(_hit.point); // Add the hit position to the list
                normalList.Add(Quaternion.FromToRotation(Vector3.forward, _hit.normal).eulerAngles); // Add the normal rotation to the list
                colorList.Add(GetParticleColor(_type)); // Add Color to the list
                GameManager.instance.ParticleCount++;
            }
        }

        public void SetLaser(VisualEffect _vfx, Vector3 _target, float _lifetime, Color _color, bool _usebuffer) // Apply the lasers VFX (Dont use for single scanner, use buffer for main scanner especially for little optimization)
        {
            _vfx.SetVector3("Target", _target);
            _vfx.SetFloat("LifeTime", _lifetime);
            if (_color != Color.white) _vfx.SetVector4("Color", _color); // Use Color.White if you do not want custom color
            _vfx.SetBool("UseBuffer", _usebuffer);
            _vfx.SendEvent("OnPlay");
        }

        ColorType GetParticleTag(RaycastHit _hit) // Get particle type based on object tag
        {
            var _ID = selectedPalette - 1;
            
            if (selectedPalette > particlesConfig.colorPalette.Count - 1)
                _ID = particlesConfig.colorPalette.Count - 1;
            
            var _palette = particlesConfig.colorPalette[_ID];
            var _type = _palette.colorType[0];
            for (int i = 0; i < _palette.colorType.Count; i++)
            {
                if (_hit.transform.CompareTag(_palette.colorType[i].tagName))
                    _type = _palette.colorType[i];
            }
            return _type;
        }

        Color GetParticleColor(ColorType _type) // Get particle color based on object tag
        {
            Color _color;
            var _rand = Random.Range(-_type.range, _type.range);
            var _range = new Color(_rand, _rand, _rand);
            _color = _type.color + _range;

            return _color;
        }

        void UpdateVFXProperties(VisualEffect _vfx) // Update vfx with new properties
        {
            _vfx.SetVector2("Particles Size", particlesSize);
            _vfx.SetTexture("Particle Texture", particlesTexture);
            _vfx.SetBool("FlatParticles", scanUseNormal);
            _vfx.SetBool("FadeParticles", scanUseFade);
            _vfx.SetVector2("FadeDistance", fadeDistanceMinMax);
            _vfx.SetFloat("FadeMinimumAlpha", fadeMinimumAlpha);
            _vfx.SetAnimationCurve("FadeCurve", fadeCurve);
        }

        void ResetList() // Reset position and color list
        {
            positionList = new List<Vector3>(bufferInitialCapacity);
            normalList = new List<Vector3>(bufferInitialCapacity);
            colorList = new List<Color>(bufferInitialCapacity);
        }

        void CreateNewVFX() // Create the vfx and assign the current vfx
        {
            ReleaseBuffer(ref positionBuffer);
            ReleaseBuffer(ref normalBuffer);
            ReleaseBuffer(ref colorBuffer);
            ResetList();
            var _vfx = new GameObject("Particle Effect").AddComponent<VisualEffect>().GetComponent<VisualEffect>();
            _vfx.visualEffectAsset = vfx;
            _vfx.transform.parent = GameManager.instance.Player.transform;
            vfxs.Add(_vfx);
            currentVFX = vfxs[vfxs.Count - 1];
        }

        void OnDestroy() // Release on destroy
        {
            ReleaseBuffer(ref positionBuffer);
            ReleaseBuffer(ref normalBuffer);
            ReleaseBuffer(ref colorBuffer);
        }

        public void ReleaseBuffer(ref GraphicsBuffer buffer) // Release
        {
            // Buffer memory must be released
            buffer?.Release();
            buffer = null;
        }

        public IEnumerator PlaySound(AudioSource _sound, float _volume) // Play Sound
        {
            _sound.pitch = Random.Range(0.90f, 0.94f);
            _sound.Play();
            yield return new WaitForSeconds(0.005f);
            _sound.volume = _volume;
        }

        public IEnumerator StopSound(AudioSource _sound) // Stop Sound
        {
            _sound.volume = 0;
            yield return new WaitForSeconds(0.01f);
            _sound.Stop();
        }
    }

    [System.Serializable]
    public class ParticlesColorConfig
    {
        public List<ColorPalette> colorPalette = new List<ColorPalette>();  // Color Palette
    }

    [System.Serializable]
    public class ColorPalette
    {
        public string typeName;                                             // Name of the palette
        public List<ColorType> colorType = new List<ColorType>();           // All Colors with Tag
    }

    [System.Serializable]
    public struct ColorType
    {
        [TagField] public string tagName;                                   // Tag name of the color
        [ColorUsage(true, true)] public Color color;                        // The color
        public float range;                                                 // Add/Remove range of the color
    }

}