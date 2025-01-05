#[compute]
#version 450

#define PI 3.141592653589793

// %dim
layout(local_size_x = 16, local_size_y = 1, local_size_z = 1) in;

struct Agent {
    vec2 pos;
    float angle;
    int species;
    vec4 speciesMask;
};

layout(set = 0, binding = 0, std430) restrict buffer Agents { Agent agents[]; };

layout(push_constant, std430) uniform Params {
    ivec2 texSize;
    int numAgents;
    int numSpecies;
    float deltaTime;
    uint ticks;
    float decaySpeed;
    float diffuseSpeed;
}
params;

void main() { agents[gl_GlobalInvocationID.x].pos = vec2(100, 100); }