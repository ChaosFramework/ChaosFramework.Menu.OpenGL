import ChaosGraphics.VertexShaders;

const vec2 verts[5] = vec2[] (
    vec2(-1.0, -1.0),
    vec2( 1.0, -1.0),
    vec2( 1.0,  1.0),
    vec2(-1.0,  1.0),
    vec2(-1.0, -1.0)
);

float thickness;

vec2 scale = vec2(1.0, 1.0);
sampler2D tex;

void fs_sample(
    vec2 inTex : TEXCOORD0,
    out vec4 col : COLOR0
) {
    col = texture(tex, inTex);
    col.rgb *= col.a;
}

void vs_createFrame(
    out vec4 outPosition : gl_Position,
    out vec2 texCoord : TEXCOORD0
) {
    int index = gl_VertexID / 2;
    float inflateFac = gl_VertexID % 2;
    float inflate = (inflateFac - 1.0) * thickness;
    vec2 pos = verts[index] * scale + inflate * verts[index];
    vs_transformPosition(vec3(pos, 0.0), outPosition);

    float texelSizeY = 1.0 / textureSize(tex, 0).y;
    texCoord = vec2(float(index) / 4.0, inflateFac == 0.0 ? texelSizeY : 1.0 - texelSizeY);
}

Pass Frame {
    Enable(Blend, true);
    BlendFuncSeperate(One, OneMinusSrcAlpha, OneMinusDstAlpha, One);
    Enable(CullFace, false);
    Enable(DepthTest, false);
    VertexShader = vs_createFrame;
    FragmentShader = fs_sample;
}
