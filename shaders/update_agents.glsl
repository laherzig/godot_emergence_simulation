#[compute]
#version 450

#define PI 3.141592653589793

// %dim
layout(local_size_x = 16, local_size_y = 1, local_size_z = 1) in;

// structs

struct Agent {
    vec2 pos;
    float angle;
    int species;
    vec4 speciesMask;
};

struct Species {
    float moveSpeed;
    float turnSpeed;
    float sensorAngle;
    float sensorDistance;
    float sensorSize;
    vec4 color;
};

// buffers

layout(set = 0, binding = 0, std430) restrict buffer Agents { Agent agents[]; };

layout(set = 1, binding = 0, rgba8) uniform restrict image2D trailmap;

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
    int agentBehavior;
}
params;

// Hash function www.cs.ubc.ca/~rbridson/docs/schechter-sca08-turbulence.pdf
// for pseudo-random numbers
uint hash(uint state) {
    state ^= 2747636419u;
    state *= 2654435769u;
    state ^= state >> 16;
    state *= 2654435769u;
    state ^= state >> 16;
    state *= 2654435769u;
    return state;
}

float scaleTo01(uint value) { return float(value) / float(pow(2, 32) - 1); }

float sense2(Agent a, Species s, float angleOffset) {
    float sensorAngle = a.angle + angleOffset;
    vec2 sensorDir = vec2(cos(sensorAngle), sin(sensorAngle));

    vec2 sensorPos = a.pos + sensorDir * s.sensorDistance;
    ivec2 sensorCenter = ivec2(sensorPos);

    float sum = 0;

    // value of agent species will be 1, other species -1
    vec4 senseWeight = a.speciesMask * 2 - vec4(1);

    for (int x = -int(s.sensorSize); x <= int(s.sensorSize); x++) {
        for (int y = -int(s.sensorSize); y <= int(s.sensorSize); y++) {

            ivec2 samplePos = sensorCenter + ivec2(x, y);

            vec4 smp;
            if (params.agentBehavior == 0) {
                // without clamping (out of bounds = 0)
                smp = imageLoad(trailmap, samplePos);
            } else if (params.agentBehavior == 1) {
                // out of bounds = other species (negative)
                if (samplePos.x < 0 || samplePos.x >= params.texSize.x ||
                    samplePos.y < 0 || samplePos.y >= params.texSize.y) {

                    smp = vec4(a.species != 0, a.species != 1, a.species != 2,
                               a.species != 3);
                } else {
                    smp = imageLoad(trailmap, samplePos);
                }
            } else if (params.agentBehavior == 2) {
                // with clamping
                smp = imageLoad(trailmap,
                                clamp(samplePos, ivec2(0), params.texSize - 1));
            } else if (params.agentBehavior == 3) {
                // wrap around
                smp =
                    imageLoad(trailmap, ivec2(mod(samplePos, params.texSize)));
            }

            //? sum += dot(senseWeight, imageLoad(trailmap, samplePos - 1));
            sum += dot(senseWeight, smp);

            // debug purposes
            // imageStore(trailmap, ivec2(sampleX, sampleY), vec4(0, 1, 1, 1));
        }
    }
    return sum;
}

void main() {
    // for convenience
    uint id = gl_GlobalInvocationID.x;

    // if out of bounds, do nothing
    if (id >= agents.length()) {
        return;
    }

    // get agent and species
    Agent a = agents[id];
    Species s = species[a.species];
    vec2 pos = a.pos;

    // get random number
    uint random = hash(
        uint(pos.y * params.texSize.x + pos.x + hash(id + params.ticks / 10)));

    float wF = sense2(a, s, 0);
    float wL = sense2(a, s, s.sensorAngle);
    float wR = sense2(a, s, -s.sensorAngle);

    // cause some randomness in steering
    float randSteerStrength = scaleTo01(random);
    //! this eliminates the randomness
    randSteerStrength = 1;

    float turnSpeed = s.turnSpeed * 2 * PI;
    // float turnSpeed = s.turnSpeed;

    // steering

    // continue in same direction
    if (wF > wL && wF > wR) {
        agents[id].angle += 0;
    }
    if (id < 10) {
        // debug[id] = float(mod(random, 2));
        // debug[id] = float(random & 1);
        // debug[id] = mod(float(random), 2.0);
    }
    // turn randomly
    else if (wF < wL && wF < wR) {
        // agents[id].angle +=
        //     (randSteerStrength - 0.5) * 2 * turnSpeed * params.deltaTime;
        // agents[id].angle += mod(random, 2) - 1 * turnSpeed *
        // params.deltaTime;
        // agents[id].angle +=
        //     float(int(mod(random, 2) * 2) - 1) * turnSpeed *
        //     params.deltaTime;

        agents[id].angle +=
            float(int(random & 1) * 2 - 1) * turnSpeed * params.deltaTime;

        // /*
        // ! Okay, this makes no sense at all
        // ! The mod(int(random), 2) should be the same as (random & 1)
        // ! However, the mod pretty much always returns 0
        // ! the & works, but only if this line is not used:
        //     agents[id].angle +=
        //         float((random & 1) * 2 - 1) * turnSpeed * params.deltaTime;
        // ! When isn't commented out, the & 1 always returns almost only 0
        // ! I have no idea why this is the case
        // */

        //! needed to do proper randomness when randomness is eliminated
        // float rand = scaleTo01(random);
        // agents[id].angle +=
        //     float(round(rand) * 2 - 1) * turnSpeed * params.deltaTime;

        // if (rand < 0.5) {
        //     agents[id].angle += turnSpeed * params.deltaTime;
        // } else {
        //     agents[id].angle -= turnSpeed * params.deltaTime;
        // }
    }
    // turn right
    else if (wR > wL) {
        agents[id].angle -= randSteerStrength * turnSpeed * params.deltaTime;
    }
    // turn left
    else if (wL > wR) {
        agents[id].angle += randSteerStrength * turnSpeed * params.deltaTime;
    }

    vec2 direction = vec2(cos(a.angle), sin(a.angle));
    vec2 newPos = a.pos + direction * s.moveSpeed * params.deltaTime;

    // if out of bounds
    if (newPos.x < 0 || newPos.x >= params.texSize.x || newPos.y < 0 ||
        newPos.y >= params.texSize.y) {
        if (params.agentBehavior == 3) {
            // wrap around
            newPos = mod(newPos, vec2(params.texSize));
        }

        else {
            // rehash
            random = hash(random);
            // generate new angle if out of bounds
            float randAngle = scaleTo01(random) * 2 * PI;

            // clamp to border
            // newPos = vec2(clamp(newPos.x, 0.0, params.texSize.x - 1),
            //               clamp(newPos.y, 0.0, params.texSize.y - 1));

            newPos = clamp(newPos, vec2(0), vec2(params.texSize) - 1);
            agents[id].angle = randAngle;
        }
    } else {
        ivec2 texCoord = ivec2(newPos);
        vec4 trail = imageLoad(trailmap, texCoord);
        // not sure if the trailweight is needed
        imageStore(trailmap, texCoord,
                   min(vec4(1), trail + a.speciesMask * params.trailWeight *
                                            params.deltaTime));
    }
    agents[id].pos = newPos;
}