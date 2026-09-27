import ChaosGraphics.Sprite;

vec4 color;

void fs_sample(
    vec2 inTex : TEXCOORD0,
    out vec4 outColor : COLOR0
) {
    outColor = texture(tex, inTex) * color;
    outColor.a = clamp(outColor.a, 0.0, 1.0);
}
