FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Forward();
    Depth( S_MODE_DEPTH );
}

COMMON
{
	#include "common/shared.hlsl"
	#include "procedural.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		// Add your vertex manipulation functions here
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"

	float g_flDissolveAmount < UiType(Slider); Default(0.0); Range(-100.0, 100.0); UiGroup("Dissolve,1/Dissolve Settings,10/10"); Attribute("DissolveAmount"); >;
    float3 g_vEdgeColor < Default3(1.0, 0.5, 0.0); UiGroup("Dissolve,1/Edge Color,20/10"); >;
    float g_flNoiseScale < Default(5.0); Range(0.1, 20.0); UiGroup("Dissolve,1/Noise Settings,40/10"); >;


    float4 MainPs(PixelInput i) : SV_Target0
    {
        Material m = Material::From(i);
        
        float2 noiseUV = i.vTextureCoords.xy * g_flNoiseScale;
        float dissolveNoise = Simplex2D(noiseUV);

		float clipThreshold = g_flDissolveAmount;

        // Apply edge glow
        m.Emission = step(dissolveNoise, clipThreshold + 0.1);
        
		clip(dissolveNoise - clipThreshold);
        
        return ShadingModelStandard::Shade(i, m);
    }
}
