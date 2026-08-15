using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QualityController : UnitySingleton<QualityController>
{
    UniversalRenderPipelineAsset urpAsset;
    public float targetScale = 0.8f;

    public override void Awake()
    {
        base.Awake();
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        urpAsset.renderScale = targetScale;
    }
    public void ResetRenderScale()
    {
        urpAsset.renderScale = targetScale;
    }
}
