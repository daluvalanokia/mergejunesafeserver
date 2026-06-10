// ── EyewaysMSS Scene Worker ──────────────────────────────────────────────────
// Runs vehicle physics/position ticks on a separate thread.
// Main thread owns Three.js (WebGL). This worker owns position math only.
// Protocol:
//   main → worker : { type:'init',   vehicles:[{id,x,z,speed,dir,lane}], roadLen }
//   main → worker : { type:'tick' }
//   main → worker : { type:'setSpeed', mult }
//   main → worker : { type:'setDensity', count }
//   main → worker : { type:'addVehicle', vehicle }
//   main → worker : { type:'removeAll' }
//   worker → main : { type:'positions', vehicles:[{id,x,z,rot}] }
// ─────────────────────────────────────────────────────────────────────────────

var vehicles   = [];
var speedMult  = 1.0;
var ROAD_LEN   = 120;
var LANE_WIDTH = 4.0;
var LANES      = 4;

function laneX(lane) { return (lane - (LANES + 1) / 2) * LANE_WIDTH; }

function dirToRot(dir) {
    switch (dir) {
        case 'N': return Math.PI;
        case 'S': return 0;
        case 'E': return -Math.PI / 2;
        case 'W': return  Math.PI / 2;
        default:  return 0;
    }
}

function dirToVec(dir) {
    switch (dir) {
        case 'N': return { dz: -1, dx: 0 };
        case 'S': return { dz:  1, dx: 0 };
        case 'E': return { dz:  0, dx:  1 };
        case 'W': return { dz:  0, dx: -1 };
        default:  return { dz:  1, dx: 0 };
    }
}

self.onmessage = function(e) {
    var msg = e.data;

    if (msg.type === 'init') {
        ROAD_LEN  = msg.roadLen || 120;
        vehicles  = (msg.vehicles || []).map(function(v) { return Object.assign({}, v); });
        speedMult = msg.speedMult || 1.0;
    }

    else if (msg.type === 'tick') {
        var dt = 0.016; // ~60fps
        vehicles.forEach(function(v) {
            var vec  = dirToVec(v.dir || 'S');
            var spd  = (v.speed || 30) * speedMult * dt * 0.3;
            v.x += vec.dx * spd;
            v.z += vec.dz * spd;
            // Wrap around road bounds
            var half = ROAD_LEN / 2;
            if (v.z >  half) v.z = -half;
            if (v.z < -half) v.z =  half;
            if (v.x >  half) v.x = -half;
            if (v.x < -half) v.x =  half;
        });

        self.postMessage({
            type: 'positions',
            vehicles: vehicles.map(function(v) {
                return { id: v.id, x: v.x, z: v.z, rot: dirToRot(v.dir || 'S') };
            })
        });
    }

    else if (msg.type === 'setSpeed') {
        speedMult = msg.mult || 1.0;
    }

    else if (msg.type === 'setDensity') {
        // Caller handles vehicle list; worker just notes count
    }

    else if (msg.type === 'addVehicle') {
        vehicles.push(Object.assign({}, msg.vehicle));
    }

    else if (msg.type === 'removeAll') {
        vehicles = [];
    }
};
