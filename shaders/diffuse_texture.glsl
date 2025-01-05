#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 2, binding = 0, rgba8) uniform
    restrict writeonly image2D diffTexture;

layout(set = 1, binding = 0, rgba8) uniform restrict readonly image2D trailmap;

layout(push_constant, std430) uniform Params {
    ivec2 texSize;
    // int numAgents;
    // int numSpecies;
    float trailWeight;
    float deltaTime;
    uint ticks;
    float decaySpeed;
    float diffuseSpeed;
}
params;

void main() {
    vec2 pos = vec2(gl_GlobalInvocationID.xy);
    // if out of bounds, do nothing
    if (pos.x < 0 || pos.x >= params.texSize.x || pos.y < 0 ||
        pos.y >= params.texSize.y) {
        return;
    }

    ivec2 texel = ivec2(gl_GlobalInvocationID.xy);

    //? maybe full zero vec4
    // vec4 sum = vec4(0.0, 0.0, 0.0, 1.0);
    vec4 sum = vec4(0.0);

    // 3x3 diffuse
    for (int i = -1; i <= 1; i++) {
        for (int j = -1; j <= 1; j++) {
            ivec2 offset = ivec2(i, j);

            // clamp to edge
            ivec2 neighbor =
                clamp(texel + offset, ivec2(0, 0), ivec2(params.texSize - 1));

            vec4 neighborTrail = imageLoad(trailmap, neighbor);
            sum += neighborTrail;
        }
    }

    sum /= 9.0;

    vec4 trail = imageLoad(trailmap, texel);
    trail = mix(trail, sum, params.diffuseSpeed * params.deltaTime);

    vec4 dim = vec4(params.decaySpeed);
    // vec4(params.decaySpeed, params.decaySpeed, params.decaySpeed, 0.0);

    // maybe full zero vec4
    trail = max(vec4(0), trail - dim * params.deltaTime);

    imageStore(diffTexture, texel, trail);
}