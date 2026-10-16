import ChaosGraphics.Sprite;

vec2 textureScale;
vec2 screenSize;

void fs_stretch(
    vec2 inTex : TEXCOORD0,
    vec4 inColor : COLOR,
    out vec4 col : COLOR0
) {
    col = texture(tex, inTex / textureScale) * inColor;
    col.rgb *= col.a;
}

void fs_repeat(
    vec4 inColor : COLOR,
    out vec4 col : COLOR0
) {
    col = texture(tex, gl_FragCoord.xy / (textureScale * screenSize)) * inColor;
    col.rgb *= col.a ;
}

Pass Stretch
{
    Enable(Blend, true);
    BlendFuncSeperate(One, OneMinusSrcAlpha, OneMinusDstAlpha, One);
    VertexShader = vs_createSpriteInstanced, PASS(vec4 INSTANCE_COLOR COLOR);
    FragmentShader = fs_stretch;
}

Pass Repeat
{
    Enable(Blend, true);
    BlendFuncSeperate(One, OneMinusSrcAlpha, OneMinusDstAlpha, One);
    VertexShader = vs_createSpriteInstanced, PASS(vec4 INSTANCE_COLOR COLOR);
    FragmentShader = fs_repeat;
}
