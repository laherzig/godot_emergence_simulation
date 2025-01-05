#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

struct Species {
    float moveSpeed;
    float turnSpeed;
    float sensorAngle;
    float sensorDistance;
    float sensorSize;
    vec4 color;
};

layout(set = 2, binding = 0, rgba8) uniform restrict readonly image2D diffmap;

layout(set = 3, binding = 0, rgba8) uniform restrict writeonly image2D colormap;

layout(set = 4, binding = 0, std430) restrict readonly buffer SpeciesSettings {
    Species species[];
};

layout(set = 5, binding = 0, std430) buffer Debug { float debug[]; };

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
    ivec2 texel = ivec2(gl_GlobalInvocationID.xy);

    // check if out of bounds
    if (texel.x < 0 || texel.x >= params.texSize.x || texel.y < 0 ||
        texel.y >= params.texSize.y) {
        return;
    }

    vec4 map = imageLoad(diffmap, texel);

    vec4 color = vec4(0);

    for (int i = 0; i < species.length(); i++) {

        color += species[i].color * map[i];
    }

    imageStore(colormap, texel, color);
}