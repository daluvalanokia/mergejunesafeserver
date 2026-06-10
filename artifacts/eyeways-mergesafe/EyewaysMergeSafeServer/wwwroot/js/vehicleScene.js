// ── EyewaysMSS Vehicle Registry & Three.js Scene Helpers ────────────────────
// Shared by Traffic/Index and Traffic3D/Index views.
// Requires Three.js r134 loaded before this script.

var VEHICLE_REGISTRY = [
    // ── Sedans ──────────────────────────────────────────────────────────────
    { type:'sedan', make:'Toyota',   model:'Camry',      size:'medium', icon:'🚗', colors:['#c0392b','#2c3e50','#bdc3c7','#e8d5b7','#16a085'] },
    { type:'sedan', make:'Honda',    model:'Civic',      size:'small',  icon:'🚗', colors:['#3498db','#e74c3c','#2ecc71','#ecf0f1','#9b59b6'] },
    { type:'sedan', make:'Ford',     model:'Fusion',     size:'medium', icon:'🚗', colors:['#2980b9','#7f8c8d','#c0392b','#f39c12','#1a1a2e'] },
    { type:'sedan', make:'Chevrolet',model:'Malibu',     size:'medium', icon:'🚗', colors:['#d35400','#8e44ad','#16a085','#bdc3c7','#2c2c54'] },
    { type:'sedan', make:'BMW',      model:'3 Series',   size:'medium', icon:'🚗', colors:['#2c2c2c','#f5f5f5','#a0522d','#4169e1','#708090'] },
    { type:'sedan', make:'Mercedes', model:'C-Class',    size:'medium', icon:'🚗', colors:['#1a1a2e','#c0c0c0','#000080','#8b0000','#f5f5f5'] },
    // ── SUVs ────────────────────────────────────────────────────────────────
    { type:'suv', make:'Ford',       model:'Explorer',   size:'large',  icon:'🚙', colors:['#1a1a2e','#4682b4','#8b4513','#696969','#006400'] },
    { type:'suv', make:'Chevrolet',  model:'Tahoe',      size:'large',  icon:'🚙', colors:['#1c1c1c','#f5f5dc','#556b2f','#8b0000','#4169e1'] },
    { type:'suv', make:'Toyota',     model:'RAV4',       size:'medium', icon:'🚙', colors:['#cc0000','#1a1a2e','#808080','#f0f0f0','#2e8b57'] },
    { type:'suv', make:'Honda',      model:'CR-V',       size:'medium', icon:'🚙', colors:['#b22222','#708080','#2f4f4f','#ffd700','#4682b4'] },
    { type:'suv', make:'Jeep',       model:'Wrangler',   size:'medium', icon:'🚙', colors:['#ff4500','#2f4f4f','#f5f5f5','#ffd700','#1a1a2e'] },
    { type:'suv', make:'Tesla',      model:'Model X',    size:'large',  icon:'🚙', colors:['#f5f5f5','#cc0000','#1a1a2e','#808080','#000000'] },
    // ── Trucks ──────────────────────────────────────────────────────────────
    { type:'truck', make:'Ford',     model:'F-150',      size:'large',  icon:'🛻', colors:['#1a1a2e','#cc0000','#696969','#f5f5dc','#006400'] },
    { type:'truck', make:'Chevrolet',model:'Silverado',  size:'large',  icon:'🛻', colors:['#c0392b','#2c3e50','#bdc3c7','#8b4513','#1abc9c'] },
    { type:'truck', make:'Ram',      model:'1500',       size:'large',  icon:'🛻', colors:['#1a1a2e','#cc0000','#808080','#f5f5f5','#8b4513'] },
    { type:'truck', make:'Toyota',   model:'Tacoma',     size:'medium', icon:'🛻', colors:['#cc0000','#696969','#f5f5dc','#2f4f4f','#1a1a2e'] },
    { type:'truck', make:'GMC',      model:'Sierra',     size:'large',  icon:'🛻', colors:['#1a1a2e','#8b0000','#bdc3c7','#5c4033','#2e8b57'] },
    // ── Motorcycles ─────────────────────────────────────────────────────────
    { type:'motorcycle', make:'Harley-Davidson', model:'Street Glide', size:'medium', icon:'🏍', colors:['#1a1a2e','#cc0000','#f5f5f5','#ffd700','#696969'] },
    { type:'motorcycle', make:'Honda',    model:'CBR600',    size:'small',  icon:'🏍', colors:['#cc0000','#1a1a2e','#f5f5f5','#ffa500','#0000cd'] },
    { type:'motorcycle', make:'Yamaha',   model:'R1',        size:'small',  icon:'🏍', colors:['#1a1a2e','#cc0000','#696969','#0000cd','#f5f5f5'] },
    { type:'motorcycle', make:'Kawasaki', model:'Ninja 400', size:'small',  icon:'🏍', colors:['#228b22','#1a1a2e','#ff4500','#f5f5f5','#696969'] },
    { type:'motorcycle', make:'Ducati',   model:'Monster',   size:'medium', icon:'🏍', colors:['#cc0000','#1a1a2e','#f5f5f5','#ffd700','#696969'] },
    // ── Vans ────────────────────────────────────────────────────────────────
    { type:'van', make:'Toyota',   model:'Sienna',   size:'large', icon:'🚐', colors:['#f5f5f5','#696969','#cc0000','#1a1a2e','#ffd700'] },
    { type:'van', make:'Honda',    model:'Odyssey',  size:'large', icon:'🚐', colors:['#c0c0c0','#1a1a2e','#cc0000','#f5f5f5','#696969'] },
    { type:'van', make:'Ford',     model:'Transit',  size:'large', icon:'🚐', colors:['#f5f5f5','#1a1a2e','#ffd700','#cc0000','#808080'] },
    { type:'van', make:'Mercedes', model:'Sprinter', size:'large', icon:'🚐', colors:['#f5f5f5','#c0c0c0','#1a1a2e','#808080','#696969'] },
];

function vehicleGetRandom(typeFilter) {
    var pool = typeFilter
        ? VEHICLE_REGISTRY.filter(function(v) { return v.type === typeFilter; })
        : VEHICLE_REGISTRY;
    if (!pool.length) pool = VEHICLE_REGISTRY;
    return pool[Math.floor(Math.random() * pool.length)];
}

function vehicleHexInt(hex) {
    return parseInt((hex || '#888888').replace('#', ''), 16);
}

// ─────────────────────────────────────────────────────────────────────────────
// Three.js mesh builders
// ─────────────────────────────────────────────────────────────────────────────

function vehicleBuildMesh(vSpec) {
    var group  = new THREE.Group();
    var type   = vSpec.type || 'sedan';
    var cHex   = vSpec.colors[Math.floor(Math.random() * vSpec.colors.length)];
    var body   = new THREE.MeshLambertMaterial({ color: vehicleHexInt(cHex) });
    var dark   = new THREE.MeshLambertMaterial({ color: 0x0a0a0a });
    var glass  = new THREE.MeshLambertMaterial({ color: 0x1e3a5f, transparent: true, opacity: 0.72 });
    var chrome = new THREE.MeshLambertMaterial({ color: 0xcccccc });
    var light  = new THREE.MeshLambertMaterial({ color: 0xffffc0, emissive: 0xffffc0, emissiveIntensity: 0.35 });

    if (type === 'sedan') {
        var b  = new THREE.Mesh(new THREE.BoxGeometry(1.80, 0.42, 4.00), body);  b.position.y = 0.38;
        var cb = new THREE.Mesh(new THREE.BoxGeometry(1.52, 0.40, 2.10), body);  cb.position.set(0, 0.80, -0.15);
        var fg = new THREE.Mesh(new THREE.BoxGeometry(1.50, 0.38, 0.06), glass); fg.position.set(0, 0.80,  0.93); fg.rotation.x =  0.28;
        var rg = new THREE.Mesh(new THREE.BoxGeometry(1.50, 0.38, 0.06), glass); rg.position.set(0, 0.80, -1.23); rg.rotation.x = -0.28;
        var sl = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.32, 1.80), glass); sl.position.set(-0.755, 0.82, -0.10);
        var sr = sl.clone(); sr.position.x = 0.755;
        var hl = new THREE.Mesh(new THREE.BoxGeometry(0.28, 0.12, 0.06), light); hl.position.set(-0.58, 0.38,  2.03);
        var hr = hl.clone(); hr.position.x = 0.58;
        var gr = new THREE.Mesh(new THREE.BoxGeometry(0.90, 0.18, 0.04), dark);  gr.position.set(0, 0.25, 2.03);
        _vWheels(group, dark, chrome, 1.86, 0.28, 1.35);
        group.add(b, cb, fg, rg, sl, sr, hl, hr, gr);

    } else if (type === 'suv') {
        var b  = new THREE.Mesh(new THREE.BoxGeometry(2.00, 0.55, 4.50), body);  b.position.y = 0.48;
        var tp = new THREE.Mesh(new THREE.BoxGeometry(1.88, 0.58, 3.40), body);  tp.position.set(0, 1.02, -0.20);
        var fg = new THREE.Mesh(new THREE.BoxGeometry(1.85, 0.55, 0.06), glass); fg.position.set(0, 1.00,  1.53); fg.rotation.x =  0.22;
        var rg = new THREE.Mesh(new THREE.BoxGeometry(1.85, 0.55, 0.06), glass); rg.position.set(0, 1.00, -1.92); rg.rotation.x = -0.22;
        var sl = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.50, 2.90), glass); sl.position.set(-0.935, 1.02, -0.18);
        var sr = sl.clone(); sr.position.x = 0.935;
        var rl = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.06, 3.20), chrome); rl.position.set(-0.82, 1.35, -0.10);
        var rr = rl.clone(); rr.position.x = 0.82;
        var hl = new THREE.Mesh(new THREE.BoxGeometry(0.36, 0.18, 0.06), light); hl.position.set(-0.68, 0.55, 2.28);
        var hr = hl.clone(); hr.position.x = 0.68;
        _vWheels(group, dark, chrome, 2.06, 0.35, 1.55);
        group.add(b, tp, fg, rg, sl, sr, rl, rr, hl, hr);

    } else if (type === 'truck') {
        var cab  = new THREE.Mesh(new THREE.BoxGeometry(2.00, 1.10, 2.10), body); cab.position.set(0, 0.72, 1.35);
        var roof = new THREE.Mesh(new THREE.BoxGeometry(1.88, 0.52, 1.90), body); roof.position.set(0, 1.38, 1.35);
        var ws   = new THREE.Mesh(new THREE.BoxGeometry(1.85, 0.48, 0.06), glass); ws.position.set(0, 1.35, 2.34); ws.rotation.x = 0.22;
        var bf   = new THREE.Mesh(new THREE.BoxGeometry(1.96, 0.10, 2.80), body); bf.position.set(0, 0.22, -1.05);
        var bl   = new THREE.Mesh(new THREE.BoxGeometry(0.10, 0.45, 2.80), body); bl.position.set(-0.93, 0.49, -1.05);
        var br   = bl.clone(); br.position.x = 0.93;
        var bw   = new THREE.Mesh(new THREE.BoxGeometry(1.96, 0.55, 0.10), body); bw.position.set(0, 0.49, 0.25);
        var hl   = new THREE.Mesh(new THREE.BoxGeometry(0.30, 0.18, 0.06), light); hl.position.set(-0.70, 0.72, 2.44);
        var hr   = hl.clone(); hr.position.x = 0.70;
        _vWheels(group, dark, chrome, 2.06, 0.35, 1.75);
        group.add(cab, roof, ws, bf, bl, br, bw, hl, hr);

    } else if (type === 'motorcycle') {
        var bd  = new THREE.Mesh(new THREE.BoxGeometry(0.50, 0.45, 1.90), body);  bd.position.y = 0.62;
        var fr  = new THREE.Mesh(new THREE.BoxGeometry(0.46, 0.32, 0.50), body);  fr.position.set(0, 0.85, 0.85);
        var st  = new THREE.Mesh(new THREE.BoxGeometry(0.36, 0.10, 0.80), dark);  st.position.set(0, 0.90, -0.10);
        var hb  = new THREE.Mesh(new THREE.BoxGeometry(0.72, 0.05, 0.05), chrome); hb.position.set(0, 1.00, 0.60);
        var wg  = new THREE.CylinderGeometry(0.30, 0.30, 0.14, 10);
        var wf  = new THREE.Mesh(wg, dark); wf.rotation.z = Math.PI/2; wf.position.set(0, 0.32,  0.75);
        var wr  = new THREE.Mesh(wg, dark); wr.rotation.z = Math.PI/2; wr.position.set(0, 0.32, -0.75);
        var ex  = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.05, 0.70, 6), chrome);
        ex.rotation.z = Math.PI/2; ex.position.set(0.28, 0.42, -0.50);
        group.add(bd, fr, st, hb, wf, wr, ex);

    } else { // van / default
        var bd  = new THREE.Mesh(new THREE.BoxGeometry(2.10, 1.65, 5.00), body); bd.position.y = 0.97;
        var fg  = new THREE.Mesh(new THREE.BoxGeometry(2.05, 0.80, 0.06), glass); fg.position.set(0, 1.20, 2.52); fg.rotation.x = 0.15;
        var rg  = new THREE.Mesh(new THREE.BoxGeometry(2.05, 0.80, 0.06), glass); rg.position.set(0, 1.20,-2.52); rg.rotation.x =-0.15;
        var sl  = new THREE.Mesh(new THREE.BoxGeometry(0.06, 1.50, 4.60), glass); sl.position.set(-1.025, 1.00, 0);
        var sr  = sl.clone(); sr.position.x = 1.025;
        var hl  = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.22, 0.06), light); hl.position.set(-0.72, 0.85, 2.52);
        var hr  = hl.clone(); hr.position.x = 0.72;
        _vWheels(group, dark, chrome, 2.16, 0.38, 1.80);
        group.add(bd, fg, rg, sl, sr, hl, hr);
    }

    group.traverse(function(c) { if (c.isMesh) c.castShadow = true; });
    return group;
}

function _vWheels(group, tireMat, rimMat, bodyWidth, r, wheelBase) {
    var wg = new THREE.CylinderGeometry(r, r, 0.28, 14);
    var rg = new THREE.CylinderGeometry(r * 0.55, r * 0.55, 0.30, 10);
    [-wheelBase, wheelBase].forEach(function(wz) {
        [-bodyWidth/2, bodyWidth/2].forEach(function(wx) {
            var tire = new THREE.Mesh(wg, tireMat); tire.rotation.z = Math.PI/2;
            tire.position.set(wx, r, wz);
            var rim  = new THREE.Mesh(rg, rimMat);  rim.rotation.z  = Math.PI/2;
            rim.position.set(wx, r, wz);
            group.add(tire, rim);
        });
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// SceneManager — owns Three.js scene + Web Worker for position ticks
// ─────────────────────────────────────────────────────────────────────────────

var SceneManager = (function() {
    var scene, camera, renderer, animId;
    var meshMap    = {};   // vehicleId → { group, data }
    var worker     = null;
    var raycaster  = new THREE.Raycaster();
    var mouse      = new THREE.Vector2();
    var selectedId = null;
    var _container, _sceneMode, _onVehicleClick;

    // Direction helpers
    function dirToRot(dir) {
        switch (dir) {
            case 'N': return Math.PI;
            case 'S': return 0;
            case 'E': return -Math.PI / 2;
            case 'W': return  Math.PI / 2;
            default:  return 0;
        }
    }

    function init(containerId, opts) {
        opts = opts || {};
        _container     = document.getElementById(containerId);
        _sceneMode     = opts.sceneMode || 'live';   // 'live' | 'simulator'
        _onVehicleClick = opts.onVehicleClick || null;

        if (!_container) return;
        var w = _container.clientWidth, h = _container.clientHeight || 500;

        // ── Renderer ──
        renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(w, h);
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type    = THREE.PCFSoftShadowMap;
        _container.appendChild(renderer.domElement);

        // ── Scene ──
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x0a0f1e);
        scene.fog        = new THREE.Fog(0x0a0f1e, 80, 150);

        // ── Camera ──
        camera = new THREE.PerspectiveCamera(50, w / h, 0.1, 300);
        camera.position.set(0, 28, 48);
        camera.lookAt(0, 0, 0);

        // ── Lights ──
        var amb = new THREE.AmbientLight(0x334466, 0.8);
        var sun = new THREE.DirectionalLight(0xffeebb, 1.2);
        sun.position.set(30, 60, 40);
        sun.castShadow = true;
        sun.shadow.mapSize.set(1024, 1024);
        scene.add(amb, sun);

        // ── Road ──
        _buildRoad();

        // ── Web Worker ──
        if (typeof Worker !== 'undefined') {
            try {
                worker = new Worker('/js/sceneWorker.js');
                worker.onmessage = _onWorkerMessage;
                worker.onerror   = function(e) { console.warn('SceneWorker error:', e.message); worker = null; };
            } catch(e) { worker = null; }
        }

        // ── Click handler ──
        renderer.domElement.addEventListener('click', _onCanvasClick);

        // ── Resize ──
        window.addEventListener('resize', function() {
            var nw = _container.clientWidth, nh = _container.clientHeight || 500;
            camera.aspect = nw / nh;
            camera.updateProjectionMatrix();
            renderer.setSize(nw, nh);
        });

        _animate();
    }

    function _buildRoad() {
        // Ground
        var ground = new THREE.Mesh(
            new THREE.PlaneGeometry(200, 200),
            new THREE.MeshLambertMaterial({ color: 0x1a1a2e })
        );
        ground.rotation.x = -Math.PI / 2;
        ground.receiveShadow = true;
        scene.add(ground);

        // Road surface
        var road = new THREE.Mesh(
            new THREE.PlaneGeometry(20, 120),
            new THREE.MeshLambertMaterial({ color: 0x2d2d2d })
        );
        road.rotation.x = -Math.PI / 2;
        road.position.y  = 0.01;
        road.receiveShadow = true;
        scene.add(road);

        // Lane markings
        for (var i = -2; i <= 2; i++) {
            for (var j = -5; j < 5; j++) {
                var mark = new THREE.Mesh(
                    new THREE.PlaneGeometry(0.15, 5),
                    new THREE.MeshLambertMaterial({ color: 0xffffff, opacity: 0.6, transparent: true })
                );
                mark.rotation.x = -Math.PI / 2;
                mark.position.set(i * 4, 0.02, j * 12);
                scene.add(mark);
            }
        }
    }

    function _animate() {
        animId = requestAnimationFrame(_animate);
        if (worker) {
            worker.postMessage({ type: 'tick' });
        } else {
            // Fallback: move vehicles on main thread
            Object.values(meshMap).forEach(function(v) {
                var speed = (v.data.speedMph || 40) * 0.016 * 0.3;
                var dir   = v.data.direction || 'S';
                if (dir === 'N' || dir === 'S') v.group.position.z += (dir === 'S' ? 1 : -1) * speed;
                if (dir === 'E' || dir === 'W') v.group.position.x += (dir === 'E' ? 1 : -1) * speed;
                if (v.group.position.z >  60) v.group.position.z = -60;
                if (v.group.position.z < -60) v.group.position.z =  60;
                if (v.group.position.x >  60) v.group.position.x = -60;
                if (v.group.position.x < -60) v.group.position.x =  60;
            });
        }
        renderer.render(scene, camera);
    }

    function _onWorkerMessage(e) {
        if (e.data.type !== 'positions') return;
        e.data.vehicles.forEach(function(pos) {
            var v = meshMap[pos.id];
            if (v) {
                v.group.position.x  = pos.x;
                v.group.position.z  = pos.z;
                v.group.rotation.y  = pos.rot;
            }
        });
    }

    function _onCanvasClick(e) {
        if (!_onVehicleClick) return;
        var rect  = renderer.domElement.getBoundingClientRect();
        mouse.x   =  ((e.clientX - rect.left)  / rect.width  ) * 2 - 1;
        mouse.y   = -((e.clientY - rect.top)   / rect.height ) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);

        var allMeshes = [];
        Object.entries(meshMap).forEach(function(pair) {
            pair[1].group.traverse(function(c) {
                if (c.isMesh) { c.__vehicleId = pair[0]; allMeshes.push(c); }
            });
        });

        var hits = raycaster.intersectObjects(allMeshes, false);
        if (hits.length > 0) {
            var vid = hits[0].object.__vehicleId;
            if (vid && meshMap[vid]) {
                selectedId = vid;
                _onVehicleClick(vid, meshMap[vid].data);
            }
        }
    }

    function addVehicle(id, data, x, z) {
        if (meshMap[id]) return;
        var vSpec = vehicleGetRandom(data.vehicleType || data.type);
        var group = vehicleBuildMesh(vSpec);
        var dir   = data.direction || 'S';
        group.position.set(x || 0, 0, z || 0);
        group.rotation.y = (dir === 'N' ? Math.PI : dir === 'E' ? -Math.PI/2 : dir === 'W' ? Math.PI/2 : 0);
        scene.add(group);
        meshMap[id] = { group: group, data: Object.assign({}, data, { vehicleSpec: vSpec }) };

        if (worker) {
            worker.postMessage({ type: 'addVehicle', vehicle: {
                id: id, x: x || 0, z: z || 0,
                speed: data.speedMph || 40,
                dir:   dir,
                lane:  data.lane || 2
            }});
        }
    }

    function removeVehicle(id) {
        if (!meshMap[id]) return;
        scene.remove(meshMap[id].group);
        delete meshMap[id];
    }

    function clearVehicles() {
        Object.keys(meshMap).forEach(removeVehicle);
        if (worker) worker.postMessage({ type: 'removeAll' });
    }

    function setSpeedMult(mult) {
        if (worker) worker.postMessage({ type: 'setSpeed', mult: mult });
    }

    function setCamAngle(deg) {
        var rad = (deg || 45) * Math.PI / 180;
        var dist = 60;
        camera.position.set(0, Math.sin(rad) * dist, Math.cos(rad) * dist);
        camera.lookAt(0, 0, 0);
    }

    function getVehicleCount() { return Object.keys(meshMap).length; }

    function getSceneMode() { return _sceneMode; }

    function setSceneMode(mode) {
        _sceneMode = mode;   // 'live' | 'simulator'
    }

    function destroy() {
        if (animId) cancelAnimationFrame(animId);
        if (worker) { worker.terminate(); worker = null; }
        if (renderer && _container) {
            _container.removeChild(renderer.domElement);
            renderer.dispose();
        }
        meshMap = {};
    }

    return {
        init:            init,
        addVehicle:      addVehicle,
        removeVehicle:   removeVehicle,
        clearVehicles:   clearVehicles,
        setSpeedMult:    setSpeedMult,
        setCamAngle:     setCamAngle,
        getVehicleCount: getVehicleCount,
        getSceneMode:    getSceneMode,
        setSceneMode:    setSceneMode,
        destroy:         destroy,
        get scene()    { return scene; },
        get camera()   { return camera; },
        get renderer() { return renderer; }
    };
})();

