import ChaosGraphics.Sprite;

vec4 color;

void fs_sample(
    vec2 inTex : TEXCOORD0,
    out vec4 outColor : COLOR0
) {
    outColor = textureLod(tex, inTex, 0.0) * color;
}
