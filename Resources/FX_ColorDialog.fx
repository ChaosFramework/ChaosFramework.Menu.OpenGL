import viewProj, transform from ChaosGraphics.VertexShaders;
import positions, texCoords from ChaosGraphics.VertexShaders;
import vs_createSprite, vs_transformPosition from ChaosGraphics.VertexShaders;

#define Pi 3.1415926535897932384626433832795

const float INV_2_PI = 1 / (2 * Pi);
const vec3 CHECK_DARK = vec3(0.1, 0.1, 0.1);
const vec3 CHECK_BRIGHT = vec3(0.8, 0.8, 0.8);
const float CHECK_SZ = 80.0;
const vec2 HUE_ZERO = vec2(0, -1);
const vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);

vec4 hsva;
vec4 rgba;
vec4 buttonCol;
float invScreenHeight;

sampler2D buttonTex;

vec3 hsv2rgb(
    vec3 c
) {
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float alpha(
    vec2 texCoord
) {
    return texCoord.y > 0.5 ? 1.0 : rgba.a;
}

vec3 checkerBoard()
{
    vec2 uv = gl_FragCoord.xy * CHECK_SZ * invScreenHeight;
    float f = max(0.0, sign(mod(floor(uv.x) + floor(uv.y), 2.0)));
    return CHECK_DARK + (CHECK_BRIGHT - CHECK_DARK) * f;
}

vec4 getCol(
    vec2 texCoord,
    vec3 rgb
) {
    vec3 checker = checkerBoard();
    vec3 col = checker + (rgb - checker) * alpha(texCoord);
    return vec4(col, 1.0);
}

void fs_H(vec2 tex : TEXCOORD0, out vec4 col : COLOR0) { col = getCol(tex, hsv2rgb(vec3(tex.x, hsva.yz))); }
void fs_S(vec2 tex : TEXCOORD0, out vec4 col : COLOR0) { col = getCol(tex, hsv2rgb(vec3(hsva.x, tex.x, hsva.z))); }
void fs_V(vec2 tex : TEXCOORD0, out vec4 col : COLOR0) { col = getCol(tex, hsv2rgb(vec3(hsva.xy, tex.x))); }
void fs_R(vec2 tex : TEXCOORD0, out vec4 col : COLOR0) { col = getCol(tex, vec3(tex.x, rgba.g, rgba.b)); }
void fs_G(vec2 tex : TEXCOORD0, out vec4 col : COLOR0) { col = getCol(tex, vec3(rgba.r, tex.x, rgba.b)); }
void fs_B(vec2 tex : TEXCOORD0, out vec4 col : COLOR0) { col = getCol(tex, vec3(rgba.r, rgba.g, tex.x)); }

void fs_A(
    vec2 tex : TEXCOORD0,
    out vec4 col : COLOR0
) {
    vec3 check = checkerBoard();
    col = vec4(check + (rgba.rgb - check) * tex.x, 1.0);
}

void fs_Button(
    vec2 tex : TEXCOORD0,
    out vec4 col : COLOR0
) {
    vec4 c = texture(buttonTex, tex);
    col = vec4(c.rgb * rgba.rgb, 1);
    col = c + (col - c) * rgba.a;
    col *= buttonCol;
}

void fs_Circle(
    vec2 tex : TEXCOORD0,
    out vec4 col : COLOR0
) {
    tex -= 0.5;
    tex *= 2;

    float saturation = length(tex);
    if (saturation > 1)
        discard;

    tex /= saturation;

    float hue = acos(dot(tex, HUE_ZERO)) * INV_2_PI;
    if (tex.x > 0)
        hue  = 1 - hue;

    vec3 rgb = hsv2rgb(vec3(hue, saturation, hsva.z));
    rgb = rgb + (1 - rgb) * (1 - hsva.a);
    col = vec4(rgb, 1);
}

void fs_Preview(
    vec2 tex : TEXCOORD0,
    out vec4 col : COLOR0
) {
    col = getCol(tex, rgba.rgb);
}

Pass H { VertexShader = vs_createSprite; FragmentShader = fs_H; }
Pass S { VertexShader = vs_createSprite; FragmentShader = fs_S; }
Pass V { VertexShader = vs_createSprite; FragmentShader = fs_V; }
Pass R { VertexShader = vs_createSprite; FragmentShader = fs_R; }
Pass G { VertexShader = vs_createSprite; FragmentShader = fs_G; }
Pass B { VertexShader = vs_createSprite; FragmentShader = fs_B; }
Pass A { VertexShader = vs_createSprite; FragmentShader = fs_A; }

Pass Button { VertexShader = vs_createSprite; FragmentShader = fs_Button; }
Pass Circle { VertexShader = vs_createSprite; FragmentShader = fs_Circle; }
Pass Preview { VertexShader = vs_createSprite; FragmentShader = fs_Preview; }
