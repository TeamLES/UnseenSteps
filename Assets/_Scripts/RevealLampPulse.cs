using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RevealLampPulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform targetSprite;          
    [SerializeField] Light2D lampLight;              
    [SerializeField] LineRenderer outlineLine;        
    [SerializeField] Sprite onSprite;
    [SerializeField] Sprite offSprite;

    [Header("Oscillation")] 
    [SerializeField] bool useUnscaledTime = false;
    [SerializeField] float frequency = 0.35f;         
    [SerializeField] float phaseOffset = 0f;

    [Header("Scale" )]
    [SerializeField] float scaleAmplitude = 0.08f;    
    
    [Header("Light Radius")]
    [SerializeField] float radiusAmplitude = 0.08f;   
    [SerializeField] bool lockRadiusToScale = true;  

    [Header("Light Intensity")]
    [SerializeField] float intensityAmplitude = 0.1f; 

    [Header("Corrupted Flicker")]
    [SerializeField] bool corruptedLamp = false;
    [SerializeField] Vector2 flickerIntervalRange = new Vector2(0.6f, 1.6f);
    bool corruptedVisible = true;
    float flickerTimer;

    [Header("Outline Line")]
    [SerializeField] bool enableOutlineLine = false;
    [SerializeField] float outlineBaseRadius = 2.5f;
    [SerializeField] float outlineScaleOffset = 0.05f; 
    [SerializeField] int outlineSegments = 64;
    [SerializeField] float outlineWidth = 0.05f;
    [SerializeField] Color outlineColor = new Color(1f, 1f, 1f, 0.25f);

    Vector3 baseScale = Vector3.one;
    float baseInnerRadius;
    float baseOuterRadius;
    float baseIntensity;
    bool initialized;
    SpriteRenderer targetRenderer;
    SpriteRenderer parentRenderer;

    void Awake()
    {
        CacheBases();
        ResetFlickerTimer();
    }

    void OnValidate()
    {
        CacheBases();
        ResetFlickerTimer();
    }

    void CacheBases()
    {
        if (targetSprite != null)
        {
            baseScale = targetSprite.localScale;
            targetRenderer = targetSprite.GetComponent<SpriteRenderer>();
        }
        parentRenderer = GetComponent<SpriteRenderer>();
        if (lampLight != null)
        {
            baseInnerRadius = lampLight.pointLightInnerRadius;
            baseOuterRadius = lampLight.pointLightOuterRadius;
            baseIntensity = lampLight.intensity;
        }
        if (outlineLine != null)
        {
            outlineLine.loop = true;
            outlineLine.useWorldSpace = false;
            outlineLine.startWidth = outlineWidth;
            outlineLine.endWidth = outlineWidth;
            outlineLine.positionCount = Mathf.Max(3, outlineSegments + 1);
            outlineLine.startColor = outlineColor;
            outlineLine.endColor = outlineColor;
        }
        initialized = true;
    }

    void ResetFlickerTimer()
    {
        flickerTimer = Random.Range(flickerIntervalRange.x, flickerIntervalRange.y);
    }

    void ToggleCorruptedVisibility()
    {
        corruptedVisible = !corruptedVisible;
        if (targetSprite != null)
        {
            if (targetRenderer != null)
                targetRenderer.sprite = corruptedVisible ? (onSprite != null ? onSprite : targetRenderer.sprite)
                                                         : (offSprite != null ? offSprite : targetRenderer.sprite);
            targetSprite.gameObject.SetActive(corruptedVisible);
        }
        if (parentRenderer != null)
            parentRenderer.sprite = corruptedVisible ? (onSprite != null ? onSprite : parentRenderer.sprite)
                                                     : (offSprite != null ? offSprite : parentRenderer.sprite);
        if (outlineLine != null)
            outlineLine.enabled = corruptedVisible && enableOutlineLine;
        if (lampLight != null)
            lampLight.enabled = corruptedVisible;
    }

    void Update()
    {
        if (!initialized) CacheBases();
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float wave = Mathf.Sin((time * frequency * Mathf.PI * 2f) + phaseOffset);

        if (corruptedLamp)
        {
            flickerTimer -= delta;
            if (flickerTimer <= 0f)
            {
                ToggleCorruptedVisibility();
                ResetFlickerTimer();
            }
        }
        else if (!corruptedVisible)
        {
            corruptedVisible = true;
            if (targetSprite != null) targetSprite.gameObject.SetActive(true);
            if (targetRenderer != null && onSprite != null) targetRenderer.sprite = onSprite;
            if (parentRenderer != null && onSprite != null) parentRenderer.sprite = onSprite;
            if (outlineLine != null) outlineLine.enabled = enableOutlineLine;
            if (lampLight != null) lampLight.enabled = true;
        }

        if (!corruptedVisible) return;

        float mult = 1f + wave * scaleAmplitude;

        if (targetSprite != null)
            targetSprite.localScale = baseScale * mult;

        if (lampLight != null)
        {
            float amp = lockRadiusToScale ? scaleAmplitude : radiusAmplitude;
            float radiusMult = 1f + wave * amp;
            lampLight.pointLightInnerRadius = Mathf.Max(0f, baseInnerRadius * radiusMult);
            lampLight.pointLightOuterRadius = Mathf.Max(0f, baseOuterRadius * radiusMult);
            float intensityMult = 1f + wave * intensityAmplitude;
            lampLight.intensity = Mathf.Max(0f, baseIntensity * intensityMult);
        }

        if (enableOutlineLine && outlineLine != null)
        {
            float radius = outlineBaseRadius * (1f + outlineScaleOffset) * mult;
            float step = Mathf.PI * 2f / outlineSegments;
            int count = Mathf.Max(3, outlineSegments);
            outlineLine.positionCount = count + 1;
            for (int i = 0; i <= count; i++)
            {
                float a = step * i;
                outlineLine.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }
    }
}
