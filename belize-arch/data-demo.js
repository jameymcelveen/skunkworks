/* Data walkthrough — trail drill + checkout + route map. */

const CLOUD_SVG = `<svg class="cloud-ico" viewBox="0 0 24 24" aria-hidden="true">
  <path d="M7.5 18h9.2a4.3 4.3 0 0 0 .6-8.55 5.5 5.5 0 0 0-10.55 1.7A3.8 3.8 0 0 0 7.5 18z"
        fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>
</svg>`;

const HUB_FADE_MS = 3500;      // hub materialize (2–5s range)
const BEAM_BREAK_MS = 450;     // after hub settles, kill the stream
const MAP_DRAW_MS = 3800;
const MAP_HOLD_MS = 500;
const MAP_FADE_MS = 700;
const HUB_COLLECT_MS = 700;
const HUB_DROP_MS = 750;
const MAP_SCALE = 1.55;        // zoom into the travel corridor
const MAP_W = 1018;
const MAP_H = 1024;

const FLORENCE = { x: 395, y: 328 };
const ATLANTA = { x: 340, y: 325 };
const BELIZE = { x: 295, y: 650 };

const STEPS = [
  {
    caption: `<b>ChristMedical</b> — one cloud database. Churches, trips, and patients all live here at rest.`,
    phase: 'solo',
    trail: ['ChristMedical']
  },
  {
    caption: `<b>Open it.</b> Tenants are churches. Drill into <b>Cornerstone</b>; First Baptist shows other churches share the same DB behind their own walls.`,
    phase: 'tenants',
    trail: ['Cornerstone', 'ChristMedical']
  },
  {
    caption: `<b>Inside Cornerstone.</b> First Baptist steps aside. Shared band sits above the clinics. <b>Belize</b> and <b>Honduras</b> are mission trips — patients stay in one clinic.`,
    phase: 'clinics',
    trail: ['Cornerstone', 'ChristMedical']
  },
  {
    caption: `<b>Belize checkout scope.</b> Honduras stays home. Shared collapses to a pill — formulary, workers, and treatments still ride along as a snapshot.`,
    phase: 'belize',
    trail: ['Belize', 'Cornerstone', 'ChristMedical']
  },
  {
    caption: `<b>Checkout.</b> A replicant copy beams to the <b>Belize Hub</b> in Florence — clinic slice plus shared snapshot. When the hub is solid, the link breaks.`,
    phase: 'checkout',
    trail: ['Belize', 'Cornerstone', 'ChristMedical']
  },
  {
    caption: `<b>Checked out.</b> Cloud Belize is marked checked-out; the hub sits in <b>Florence</b>, packed for the trip. Same data, two places.`,
    phase: 'paired',
    trail: ['Belize', 'Cornerstone', 'ChristMedical']
  },
  {
    caption: `<b>The hub travels.</b> Florence → Atlanta → Belize. When it lands, the hub is <b>in Belize</b> — clinic week begins.`,
    phase: 'map',
    trail: ['Belize', 'Cornerstone', 'ChristMedical']
  },
  {
    caption: `<b>Cloud stays home.</b> Checked-out Belize slides off. The hub centers on-site — it is the trip master now.`,
    phase: 'field',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Local network.</b> A travel router pops up under the hub. Sage packets ride the LAN both ways — request up, answer down. No internet required.`,
    phase: 'lan',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Field team.</b> Receptionist and nurse on laptops; doctor and minister on tablets. Everyone talks to the hub over the same Wi‑Fi.`,
    phase: 'staff',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Check-in.</b> A hurt patient arrives at reception. The laptop swells with activity — registration bits hop to the router and hub.`,
    phase: 'visit-rx',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Vitals.</b> Nurse station next. Thermometer in, vitals out — same LAN path, different laptop.`,
    phase: 'visit-nurse',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Exam.</b> With the doctor now. Bandage off, frown softening — the chart on the tablet already knows the path so far.`,
    phase: 'visit-dr',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Send-off.</b> Minister closes the visit with a smile — tear still there for a beat. Then patient exits right. Cross-device care, one hub.`,
    phase: 'visit-out',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Close the shop.</b> Staff and Wi‑Fi reverse out. The hub stays. Cloud Belize drops back in above it — still marked checked-out.`,
    phase: 'closeout',
    trail: ['Belize Hub']
  },
  {
    caption: `<b>Check-in link.</b> A thick path opens between hub and cloud. Clinical data wins on return; shared rows merge. Bits climb and fall until they agree.`,
    phase: 'checkin',
    trail: ['Belize', 'ChristMedical']
  },
  {
    caption: `<b>Home.</b> Link drops. Hub vanishes. Checked-out lifts — Belize is cloud-master again.`,
    phase: 'home',
    trail: ['Belize', 'ChristMedical']
  }
];

const reduceMotion = () =>
  window.matchMedia('(prefers-reduced-motion: reduce)').matches;

let step = 0;
let animating = false;

function el(id) { return document.getElementById(id); }

function renderTrail(parts) {
  const trail = el('trail');
  if (!trail) return;
  trail.innerHTML = parts.map((name, i) => {
    const current = i === 0 ? ' current' : '';
    const sep = i < parts.length - 1
      ? `<span class="trail-sep" aria-hidden="true">→</span>`
      : '';
    return `<span class="trail-seg${current}">${CLOUD_SVG}<span class="trail-name">${name}</span></span>${sep}`;
  }).join('');
}

function clearAnim() {
  document.querySelectorAll(
    '.cloud-key, .trail-panel, .panel-view, .drill-shared, .shared-pill, .checkout-zone, .hub-replicant, .data-stream, .paired-keys, .indy-map, .indy-route, .clinic-router, .bust'
  ).forEach(n => {
    n.classList.remove(
      'mashing', 'leaving', 'entering', 'exiting', 'is-active', 'pulse',
      'streaming', 'materializing', 'source-slide', 'broken', 'shrinking',
      'indy-show', 'indy-draw', 'indy-hide',
      'hub-collected', 'cloud-exit-left', 'pop-in', 'is-hot'
    );
  });
  hideFlyer();
  resetPlane();
}

function resetClinic() {
  const field = el('clinic-field');
  if (!field) return;
  field.hidden = true;
  field.classList.remove(
    'is-raised', 'is-linked', 'is-staffed', 'is-closing',
    'has-cloud-back', 'is-reconciling', 'line-gone', 'hub-gone'
  );
  field.removeAttribute('data-hot');
  field.dataset.beat = 'hub';
  ['clinic-spine', 'clinic-router', 'clinic-fan', 'clinic-staff', 'clinic-patient',
   'reconcile-cloud-wrap', 'reconcile-bridge'].forEach(id => {
    const n = el(id);
    if (!n) return;
    n.hidden = true;
    n.classList.remove('pop-in', 'is-live');
  });
  document.querySelectorAll('.bust').forEach(b => {
    b.classList.remove('is-hot');
    b.style.animation = '';
    b.style.opacity = '';
    b.style.transform = '';
  });
  const patient = el('clinic-patient');
  if (patient) {
    patient.dataset.mood = 'hurt';
    patient.dataset.slot = 'off-left';
  }
  el('clinic-router')?.classList.remove('pop-in');
  const hub = el('clinic-hub');
  if (hub) {
    hub.style.opacity = '';
    hub.style.transform = '';
  }
  const top = el('clinic-top');
  if (top) top.hidden = false;
  const cloud = el('reconcile-cloud');
  if (cloud) {
    cloud.classList.add('checked-out');
    cloud.classList.remove('is-home');
  }
  const stamp = el('reconcile-stamp');
  if (stamp) {
    stamp.hidden = false;
    stamp.style.opacity = '';
    stamp.style.transform = '';
  }
  const sub = el('reconcile-cloud-sub');
  if (sub) sub.textContent = 'cloud · checked out';
  const hubSub = el('clinic-hub-sub');
  if (hubSub) hubSub.textContent = 'in Belize · trip master';
}

function setHotStaff(role) {
  document.querySelectorAll('.bust').forEach(b => {
    b.classList.toggle('is-hot', role != null && b.dataset.role === role);
  });
  const field = el('clinic-field');
  if (!field) return;
  if (role) field.setAttribute('data-hot', role);
  else field.removeAttribute('data-hot');
}

function showClinicBeat(beat) {
  const field = el('clinic-field');
  const paired = el('paired-keys');
  const panel = el('trail-panel');
  const checkout = el('checkout-zone');
  const solo = el('key-christmed');

  solo.hidden = true;
  panel.hidden = true;
  checkout.hidden = true;
  paired.hidden = true;
  field.hidden = false;
  setHubLocation('belize');
  field.dataset.beat = beat;

  const spine = el('clinic-spine');
  const router = el('clinic-router');
  const fan = el('clinic-fan');
  const staff = el('clinic-staff');
  const patient = el('clinic-patient');

  // progressive reveal by beat
  const linked = ['lan', 'staff', 'visit-rx', 'visit-nurse', 'visit-dr', 'visit-out'].includes(beat);
  const staffed = ['staff', 'visit-rx', 'visit-nurse', 'visit-dr', 'visit-out'].includes(beat);
  const visiting = beat.startsWith('visit-');
  const closing = beat === 'closeout';
  const checking = beat === 'checkin';
  const home = beat === 'home';

  field.classList.toggle('is-raised', !closing && !checking && !home);
  field.classList.toggle('is-linked', linked);
  field.classList.toggle('is-staffed', staffed);
  field.classList.toggle('is-closing', closing || checking || home);
  field.classList.toggle('has-cloud-back', closing || checking || home);
  field.classList.toggle('is-reconciling', checking || home);
  field.classList.toggle('line-gone', home);
  field.classList.toggle('hub-gone', home);

  const cloudWrap = el('reconcile-cloud-wrap');
  const bridge = el('reconcile-bridge');
  const topHub = el('clinic-top');

  if (closing || checking || home) {
    // field LAN gone; hub (+ optional cloud/bridge) remains
    spine.hidden = true;
    router.hidden = true;
    fan.hidden = true;
    staff.hidden = true;
    patient.hidden = true;
    if (topHub) topHub.hidden = false;
    cloudWrap.hidden = false;
    bridge.hidden = !(checking || home);
    setHotStaff(null);

    if (home) {
      const cloud = el('reconcile-cloud');
      cloud.classList.remove('checked-out');
      cloud.classList.add('is-home');
      el('reconcile-stamp').hidden = true;
      el('reconcile-cloud-sub').textContent = 'cloud · home';
      bridge.hidden = true;
    } else {
      const cloud = el('reconcile-cloud');
      cloud.classList.add('checked-out');
      cloud.classList.remove('is-home');
      el('reconcile-stamp').hidden = false;
      el('reconcile-cloud-sub').textContent = 'cloud · checked out';
      if (checking) el('clinic-hub-sub').textContent = 'in Belize · reconciling';
    }
    return;
  }

  cloudWrap.hidden = true;
  bridge.hidden = true;
  if (topHub) topHub.hidden = false;

  spine.hidden = !linked;
  router.hidden = !linked;
  fan.hidden = !staffed;
  staff.hidden = !staffed;
  patient.hidden = !visiting;

  if (linked) {
    router.classList.add('pop-in');
  }

  // settle staff visible when jumping via Back (no pop anim)
  if (staffed && !animating) {
    document.querySelectorAll('.bust').forEach(b => {
      b.style.opacity = '1';
      b.style.transform = 'scale(1)';
      b.style.animation = 'none';
    });
  }

  if (beat === 'field') {
    setHotStaff(null);
  } else if (beat === 'lan') {
    setHotStaff(null);
  } else if (beat === 'staff') {
    setHotStaff(null);
    patient.dataset.slot = 'off-left';
    patient.dataset.mood = 'hurt';
  } else if (beat === 'visit-rx') {
    patient.hidden = false;
    patient.dataset.mood = 'hurt';
    patient.dataset.slot = 'rx';
    setHotStaff('rx');
  } else if (beat === 'visit-nurse') {
    patient.hidden = false;
    patient.dataset.mood = 'vitals';
    patient.dataset.slot = 'nurse';
    setHotStaff('nurse');
  } else if (beat === 'visit-dr') {
    patient.hidden = false;
    patient.dataset.mood = 'better';
    patient.dataset.slot = 'dr';
    setHotStaff('dr');
  } else if (beat === 'visit-out') {
    patient.hidden = false;
    patient.dataset.mood = 'departed';
    patient.dataset.slot = 'off-right';
    setHotStaff(null);
  }
}

function hideFlyer() {
  const flyer = el('hub-flyer');
  if (!flyer) return;
  flyer.classList.remove('is-on');
  flyer.style.transition = 'none';
  flyer.style.transform = '';
  flyer.style.left = '';
  flyer.style.top = '';
  flyer.style.width = '';
  flyer.style.opacity = '';
  flyer.setAttribute('aria-hidden', 'true');
}

function resetPlane() {
  const plane = el('indy-plane');
  const route = el('indy-route');
  const shadow = el('indy-route-shadow');
  const world = el('indy-world');
  if (plane) {
    plane.setAttribute('opacity', '0');
    plane.setAttribute('transform', `translate(${FLORENCE.x} ${FLORENCE.y}) rotate(-95)`);
  }
  [route, shadow].forEach(r => {
    if (!r) return;
    r.classList.remove('indy-draw');
    r.style.strokeDashoffset = '';
  });
  if (world) {
    world.style.transition = 'none';
    world.style.transform = '';
  }
}

function panMapTo(x, y, { animate } = { animate: false }) {
  const vp = el('indy-viewport');
  const world = el('indy-world');
  if (!vp || !world) return;
  const vw = vp.clientWidth;
  const vh = vp.clientHeight;
  const s = MAP_SCALE;
  let tx = vw / 2 - x * s;
  let ty = vh / 2 - y * s;
  const minX = vw - MAP_W * s;
  const minY = vh - MAP_H * s;
  tx = Math.min(0, Math.max(minX, tx));
  ty = Math.min(0, Math.max(minY, ty));
  world.style.transition = animate ? 'transform .55s ease-out' : 'none';
  world.style.transform = `translate(${tx}px, ${ty}px) scale(${s})`;
}

function svgToPage(svg, x, y) {
  const pt = svg.createSVGPoint();
  pt.x = x;
  pt.y = y;
  const ctm = svg.getScreenCTM();
  if (!ctm) return { x: 0, y: 0 };
  const p = pt.matrixTransform(ctm);
  return { x: p.x, y: p.y };
}

function easeInOut(t) {
  return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
}

function setPlaneAt(path, plane, dist) {
  const len = path.getTotalLength();
  const d = Math.max(0, Math.min(len, dist));
  const pt = path.getPointAtLength(d);
  const look = path.getPointAtLength(Math.min(len, d + 2));
  const angle = Math.atan2(look.y - pt.y, look.x - pt.x) * 180 / Math.PI;
  plane.setAttribute('transform', `translate(${pt.x} ${pt.y}) rotate(${angle})`);
  plane.setAttribute('opacity', '1');
  panMapTo(pt.x, pt.y);
}

function parkFlyerOnHub() {
  const hub = el('key-hub-btn');
  const flyer = el('hub-flyer');
  const r = hub.getBoundingClientRect();
  flyer.style.transition = 'none';
  flyer.style.left = `${r.left}px`;
  flyer.style.top = `${r.top}px`;
  flyer.style.width = `${r.width}px`;
  flyer.style.transform = 'translate(0,0) scale(1)';
  flyer.style.opacity = '1';
  flyer.classList.add('is-on');
  flyer.setAttribute('aria-hidden', 'false');
  void flyer.offsetWidth;
  flyer.style.transition = '';
}

function flyFlyerTo(pageX, pageY, scale, opacity) {
  const flyer = el('hub-flyer');
  const r = flyer.getBoundingClientRect();
  // convert absolute target to transform relative to current left/top
  const cx = parseFloat(flyer.style.left) + r.width / 2;
  const cy = parseFloat(flyer.style.top) + r.height / 2;
  const dx = pageX - cx;
  const dy = pageY - cy;
  flyer.style.transform = `translate(${dx}px, ${dy}px) scale(${scale})`;
  flyer.style.opacity = String(opacity);
}

function showView(id) {
  ['view-tenants', 'view-clinics', 'view-belize'].forEach(vid => {
    el(vid).hidden = vid !== id;
  });
}

function setHubLocation(where) {
  const loc = where === 'belize' ? 'in Belize' : 'in Florence';
  const sub = el('hub-replicant-loc');
  const key = el('hub-key-loc');
  if (sub) sub.textContent = loc;
  if (key) key.textContent = loc;
  const btn = el('key-hub-btn');
  if (btn) btn.setAttribute('aria-label', `Belize Hub ${loc}`);
}

function setPhase(phase) {
  const solo = el('key-christmed');
  const panel = el('trail-panel');
  const checkout = el('checkout-zone');
  const paired = el('paired-keys');
  const map = el('indy-map');
  const keyCs = el('key-cornerstone');
  const keyFb = el('key-firstbaptist');
  const keyBz = el('key-belize');
  const keyHn = el('key-honduras');
  const sharedBand = el('shared-band');
  const sharedPill = el('shared-pill');

  el('drill').dataset.phase = phase;
  clearAnim();
  resetClinic();

  // defaults
  map.hidden = true;
  map.setAttribute('aria-hidden', 'true');

  if (phase === 'solo') {
    solo.hidden = false;
    panel.hidden = true;
    checkout.hidden = true;
    paired.hidden = true;
    setHubLocation('florence');
    solo.classList.add('is-active', 'pulse');
    return;
  }

  solo.hidden = true;

  if (phase === 'tenants') {
    panel.hidden = false;
    checkout.hidden = true;
    paired.hidden = true;
    setHubLocation('florence');
    showView('view-tenants');
    keyFb.hidden = false;
    keyCs.classList.add('is-active', 'pulse');
  } else if (phase === 'clinics') {
    panel.hidden = false;
    checkout.hidden = true;
    paired.hidden = true;
    setHubLocation('florence');
    showView('view-clinics');
    keyHn.hidden = false;
    sharedBand.hidden = false;
    keyBz.classList.add('is-active');
  } else if (phase === 'belize') {
    panel.hidden = false;
    checkout.hidden = true;
    paired.hidden = true;
    setHubLocation('florence');
    showView('view-belize');
    sharedPill.hidden = false;
  } else if (phase === 'checkout') {
    panel.hidden = false;
    checkout.hidden = false;
    paired.hidden = true;
    setHubLocation('florence');
    showView('view-belize');
    sharedPill.hidden = false;
    panel.classList.add('source-slide');
  } else if (phase === 'paired') {
    panel.hidden = true;
    checkout.hidden = true;
    paired.hidden = false;
    setHubLocation('florence');
    el('key-hub-btn').classList.add('is-active');
  } else if (phase === 'map') {
    panel.hidden = true;
    checkout.hidden = true;
    paired.hidden = false;
    setHubLocation('belize');
    el('key-hub-btn').classList.add('is-active');
  } else if (
    phase === 'field' || phase === 'lan' || phase === 'staff' ||
    phase === 'visit-rx' || phase === 'visit-nurse' || phase === 'visit-dr' || phase === 'visit-out' ||
    phase === 'closeout' || phase === 'checkin' || phase === 'home'
  ) {
    showClinicBeat(phase);
  }
}

function mashThen(runMash, thenPhase, done) {
  if (reduceMotion()) {
    const s = STEPS.find(x => x.phase === thenPhase);
    renderTrail(s.trail);
    setPhase(thenPhase);
    done();
    return;
  }
  animating = true;
  runMash(() => {
    const s = STEPS.find(x => x.phase === thenPhase);
    renderTrail(s.trail);
    setPhase(thenPhase);
    el('trail-panel').classList.add('entering');
    const viewId =
      thenPhase === 'tenants' ? 'view-tenants'
        : thenPhase === 'clinics' ? 'view-clinics'
          : 'view-belize';
    el(viewId).classList.add('entering');
    window.setTimeout(() => {
      el('trail-panel').classList.remove('entering');
      el(viewId).classList.remove('entering');
      animating = false;
      done();
    }, 300);
  });
}

function mashDbOpen(done) {
  mashThen((next) => {
    el('key-christmed').classList.add('mashing');
    window.setTimeout(() => {
      el('key-christmed').classList.add('leaving');
      window.setTimeout(next, 120);
    }, 140);
  }, 'tenants', done);
}

function mashClinics(done) {
  mashThen((next) => {
    el('key-cornerstone').classList.add('mashing');
    el('key-firstbaptist').classList.add('exiting');
    window.setTimeout(() => {
      el('key-cornerstone').classList.add('leaving');
      window.setTimeout(next, 160);
    }, 140);
  }, 'clinics', done);
}

function mashBelize(done) {
  mashThen((next) => {
    el('key-belize').classList.add('mashing');
    el('key-honduras').classList.add('exiting');
    el('shared-band').classList.add('exiting');
    window.setTimeout(() => {
      el('key-belize').classList.add('leaving');
      window.setTimeout(next, 160);
    }, 140);
  }, 'belize', done);
}

function runCheckout(done) {
  const s = STEPS.find(x => x.phase === 'checkout');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('checkout');
    el('hub-replicant').classList.add('materializing');
    done();
    return;
  }

  animating = true;
  setPhase('checkout');
  el('data-stream').classList.add('streaming');
  el('hub-replicant').classList.add('materializing');

  window.setTimeout(() => {
    // Hub is solid — break the link.
    el('data-stream').classList.remove('streaming');
    el('data-stream').classList.add('broken');
    window.setTimeout(() => {
      animating = false;
      done();
    }, BEAM_BREAK_MS);
  }, HUB_FADE_MS);
}

function runPaired(done) {
  const s = STEPS.find(x => x.phase === 'paired');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('paired');
    done();
    return;
  }

  animating = true;
  // Shrink the big panels, then swap to keys.
  el('trail-panel').classList.add('shrinking');
  el('checkout-zone').classList.add('shrinking');
  window.setTimeout(() => {
    setPhase('paired');
    el('paired-keys').classList.add('entering');
    window.setTimeout(() => {
      el('paired-keys').classList.remove('entering');
      animating = false;
      done();
    }, 350);
  }, 420);
}

function runMap(done) {
  const s = STEPS.find(x => x.phase === 'map');
  renderTrail(s.trail);
  setPhase('paired');
  el('paired-keys').hidden = false;
  setHubLocation('florence');
  hideFlyer();
  resetPlane();
  el('key-hub-btn').classList.remove('hub-collected');

  const map = el('indy-map');
  const svg = el('indy-svg');
  const route = el('indy-route');
  const plane = el('indy-plane');
  const hub = el('key-hub-btn');
  const flyerLoc = el('hub-flyer-loc');

  const finishInBelize = () => {
    hideFlyer();
    hub.classList.remove('hub-collected');
    setHubLocation('belize');
    el('drill').dataset.phase = 'map';
    animating = false;
    done();
  };

  if (reduceMotion()) {
    map.hidden = false;
    map.classList.add('indy-show');
    route.style.strokeDashoffset = '0';
    const shadow = el('indy-route-shadow');
    if (shadow) shadow.style.strokeDashoffset = '0';
    panMapTo(BELIZE.x, BELIZE.y);
    setPlaneAt(route, plane, route.getTotalLength());
    setHubLocation('belize');
    window.setTimeout(() => {
      map.classList.add('indy-hide');
      window.setTimeout(() => {
        map.hidden = true;
        map.classList.remove('indy-show', 'indy-hide');
        route.style.strokeDashoffset = '';
        if (shadow) shadow.style.strokeDashoffset = '';
        resetPlane();
        finishInBelize();
      }, 200);
    }, 600);
    return;
  }

  animating = true;
  map.hidden = false;
  map.setAttribute('aria-hidden', 'false');
  map.classList.add('indy-show');
  if (flyerLoc) flyerLoc.textContent = 'in Florence';

  // Layout, then frame Florence — map will slide under the plane.
  window.requestAnimationFrame(() => {
    panMapTo(FLORENCE.x, FLORENCE.y);
    setPlaneAt(route, plane, 0);
  });

  // 1) Hub coin flies into the plane
  window.setTimeout(() => {
    panMapTo(FLORENCE.x, FLORENCE.y);
    setPlaneAt(route, plane, 0);
    parkFlyerOnHub();
    hub.classList.add('hub-collected');
    const flor = svgToPage(svg, FLORENCE.x, FLORENCE.y);

    window.requestAnimationFrame(() => {
      flyFlyerTo(flor.x, flor.y, 0.12, 0);
    });

    window.setTimeout(() => {
      hideFlyer();

      // 2) Route draws + plane flies; map pans so plane stays centered
      const shadow = el('indy-route-shadow');
      [route, shadow].forEach(r => {
        if (!r) return;
        r.classList.remove('indy-draw');
        void r.getBoundingClientRect();
        r.classList.add('indy-draw');
      });

      const len = route.getTotalLength();
      const t0 = performance.now();

      const fly = (now) => {
        const p = Math.min(1, (now - t0) / MAP_DRAW_MS);
        setPlaneAt(route, plane, easeInOut(p) * len);
        if (p < 1) {
          requestAnimationFrame(fly);
          return;
        }

        // 3) Reverse: coin pops out at Belize into hub slot
        window.setTimeout(() => {
          const bel = svgToPage(svg, BELIZE.x, BELIZE.y);
          const home = hub.getBoundingClientRect();
          const flyer = el('hub-flyer');
          if (flyerLoc) flyerLoc.textContent = 'in Belize';

          flyer.style.transition = 'none';
          flyer.style.left = `${home.left}px`;
          flyer.style.top = `${home.top}px`;
          flyer.style.width = `${home.width}px`;
          const cx = home.left + home.width / 2;
          const cy = home.top + home.height / 2;
          flyer.style.transform = `translate(${bel.x - cx}px, ${bel.y - cy}px) scale(0.12)`;
          flyer.style.opacity = '0';
          flyer.classList.add('is-on');
          flyer.setAttribute('aria-hidden', 'false');
          void flyer.offsetWidth;
          flyer.style.transition = '';
          flyer.style.transform = 'translate(0,0) scale(1)';
          flyer.style.opacity = '1';

          window.setTimeout(() => {
            hideFlyer();
            hub.classList.remove('hub-collected');
            setHubLocation('belize');
            plane.setAttribute('opacity', '0');

            window.setTimeout(() => {
              map.classList.add('indy-hide');
              window.setTimeout(() => {
                map.hidden = true;
                map.setAttribute('aria-hidden', 'true');
                map.classList.remove('indy-show', 'indy-hide');
                route.classList.remove('indy-draw');
                el('indy-route-shadow')?.classList.remove('indy-draw');
                resetPlane();
                finishInBelize();
              }, MAP_FADE_MS);
            }, MAP_HOLD_MS);
          }, HUB_DROP_MS);
        }, 200);
      };
      requestAnimationFrame(fly);
    }, HUB_COLLECT_MS);
  }, 480);
}

function apply(idx, { animate } = { animate: false }) {
  const s = STEPS[idx];
  const prev = idx > 0 ? STEPS[idx - 1] : null;

  el('caption').innerHTML = s.caption;
  el('count').textContent = `${idx + 1} / ${STEPS.length}`;
  el('prev').disabled = idx <= 0;
  el('next').disabled = idx >= STEPS.length - 1;

  if (animate && s.phase === 'tenants' && prev?.phase === 'solo') {
    mashDbOpen(() => {});
    return;
  }
  if (animate && s.phase === 'clinics' && prev?.phase === 'tenants') {
    mashClinics(() => {});
    return;
  }
  if (animate && s.phase === 'belize' && prev?.phase === 'clinics') {
    mashBelize(() => {});
    return;
  }
  if (animate && s.phase === 'checkout' && prev?.phase === 'belize') {
    runCheckout(() => {});
    return;
  }
  if (animate && s.phase === 'paired' && prev?.phase === 'checkout') {
    runPaired(() => {});
    return;
  }
  if (animate && s.phase === 'map' && prev?.phase === 'paired') {
    runMap(() => {});
    return;
  }
  if (animate && s.phase === 'field' && prev?.phase === 'map') {
    runField(() => {});
    return;
  }
  if (animate && s.phase === 'lan' && prev?.phase === 'field') {
    runLan(() => {});
    return;
  }
  if (animate && s.phase === 'staff' && prev?.phase === 'lan') {
    runStaff(() => {});
    return;
  }
  if (animate && s.phase === 'visit-rx' && prev?.phase === 'staff') {
    runVisit('visit-rx', () => {});
    return;
  }
  if (animate && s.phase === 'visit-nurse' && prev?.phase === 'visit-rx') {
    runVisit('visit-nurse', () => {});
    return;
  }
  if (animate && s.phase === 'visit-dr' && prev?.phase === 'visit-nurse') {
    runVisit('visit-dr', () => {});
    return;
  }
  if (animate && s.phase === 'visit-out' && prev?.phase === 'visit-dr') {
    runVisitOut(() => {});
    return;
  }
  if (animate && s.phase === 'closeout' && prev?.phase === 'visit-out') {
    runCloseout(() => {});
    return;
  }
  if (animate && s.phase === 'checkin' && prev?.phase === 'closeout') {
    runCheckin(() => {});
    return;
  }
  if (animate && s.phase === 'home' && prev?.phase === 'checkin') {
    runHome(() => {});
    return;
  }

  renderTrail(s.trail);
  if (s.phase === 'map') {
    setPhase('map');
    el('paired-keys').hidden = false;
    el('key-hub-btn').classList.remove('hub-collected');
  } else {
    setPhase(s.phase);
  }
  if (s.phase === 'checkout') {
    el('hub-replicant').classList.add('materializing');
    el('data-stream').classList.add('broken');
  }
}

function runField(done) {
  const s = STEPS.find(x => x.phase === 'field');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('field');
    done();
    return;
  }

  animating = true;
  el('paired-keys').hidden = false;
  el('key-cloud-belize').classList.add('cloud-exit-left');
  el('key-hub-btn').style.transition = 'transform .5s cubic-bezier(.2,.8,.2,1)';
  el('key-hub-btn').style.transform = 'translateX(-36px) translateY(-18px) scale(1.04)';

  window.setTimeout(() => {
    el('key-hub-btn').style.transition = '';
    el('key-hub-btn').style.transform = '';
    el('key-cloud-belize').classList.remove('cloud-exit-left');
    el('paired-keys').hidden = true;

    resetClinic();
    showClinicBeat('field');
    el('drill').dataset.phase = 'field';
    el('clinic-field').classList.remove('is-raised');
    void el('clinic-field').offsetWidth;
    el('clinic-field').classList.add('is-raised');

    window.setTimeout(() => {
      animating = false;
      done();
    }, 560);
  }, 520);
}

function runLan(done) {
  const s = STEPS.find(x => x.phase === 'lan');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('lan');
    done();
    return;
  }

  animating = true;
  if (el('clinic-field').hidden) {
    resetClinic();
    showClinicBeat('field');
  }
  showClinicBeat('lan');
  el('drill').dataset.phase = 'lan';
  const router = el('clinic-router');
  router.classList.remove('pop-in');
  void router.offsetWidth;
  router.classList.add('pop-in');

  window.setTimeout(() => {
    animating = false;
    done();
  }, 700);
}

function runStaff(done) {
  const s = STEPS.find(x => x.phase === 'staff');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('staff');
    done();
    return;
  }

  animating = true;
  if (el('clinic-field').hidden) showClinicBeat('lan');
  // reset bust inline styles so pop anim can run
  document.querySelectorAll('.bust').forEach(b => {
    b.style.opacity = '';
    b.style.transform = '';
    b.style.animation = '';
  });
  showClinicBeat('staff');
  el('drill').dataset.phase = 'staff';

  window.setTimeout(() => {
    animating = false;
    done();
  }, 750);
}

function runVisit(beat, done) {
  const s = STEPS.find(x => x.phase === beat);
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase(beat);
    done();
    return;
  }

  animating = true;
  if (el('clinic-field').hidden) showClinicBeat('staff');

  const patient = el('clinic-patient');
  if (beat === 'visit-rx') {
    // slide in from left
    patient.hidden = false;
    patient.dataset.mood = 'hurt';
    patient.dataset.slot = 'off-left';
    el('clinic-field').classList.add('is-raised', 'is-linked', 'is-staffed');
    el('clinic-spine').hidden = false;
    el('clinic-router').hidden = false;
    el('clinic-fan').hidden = false;
    el('clinic-staff').hidden = false;
    setHotStaff(null);
    void patient.offsetWidth;
    patient.dataset.slot = 'rx';
    setHotStaff('rx');
  } else {
    showClinicBeat(beat);
  }
  el('drill').dataset.phase = beat;

  window.setTimeout(() => {
    animating = false;
    done();
  }, 800);
}

function runVisitOut(done) {
  const s = STEPS.find(x => x.phase === 'visit-out');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('visit-out');
    done();
    return;
  }

  animating = true;
  if (el('clinic-field').hidden) showClinicBeat('visit-dr');
  const patient = el('clinic-patient');
  patient.hidden = false;
  // smile + tear while with minister
  patient.dataset.mood = 'well';
  patient.dataset.slot = 'minister';
  setHotStaff('minister');
  el('clinic-field').classList.add('is-raised', 'is-linked', 'is-staffed');
  el('clinic-spine').hidden = false;
  el('clinic-router').hidden = false;
  el('clinic-fan').hidden = false;
  el('clinic-staff').hidden = false;
  el('drill').dataset.phase = 'visit-out';

  window.setTimeout(() => {
    // after minister: tear drops, then exit
    patient.dataset.mood = 'departed';
    window.setTimeout(() => {
      patient.dataset.slot = 'off-right';
      window.setTimeout(() => {
        setHotStaff(null);
        animating = false;
        done();
      }, 800);
    }, 350);
  }, 950);
}

function runCloseout(done) {
  const s = STEPS.find(x => x.phase === 'closeout');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('closeout');
    done();
    return;
  }

  animating = true;
  const field = el('clinic-field');
  if (field.hidden) showClinicBeat('visit-out');

  field.hidden = false;
  el('clinic-top').hidden = false;
  el('reconcile-cloud-wrap').hidden = true;
  el('reconcile-bridge').hidden = true;
  el('clinic-staff').hidden = false;
  el('clinic-fan').hidden = false;
  el('clinic-router').hidden = false;
  el('clinic-spine').hidden = false;
  el('clinic-patient').hidden = true;
  setHotStaff(null);

  document.querySelectorAll('.bust').forEach(b => {
    b.style.opacity = '1';
    b.style.transform = 'scale(1)';
    b.style.animation = 'none';
  });
  field.classList.remove('has-cloud-back', 'is-reconciling', 'line-gone', 'hub-gone');
  field.classList.add('is-raised', 'is-linked', 'is-staffed');
  void field.offsetWidth;
  field.classList.add('is-closing');
  el('drill').dataset.phase = 'closeout';

  window.setTimeout(() => {
    el('clinic-staff').hidden = true;
    el('clinic-fan').hidden = true;
    el('clinic-router').hidden = true;
    el('clinic-spine').hidden = true;
    // hub stays; cloud drops in above
    el('reconcile-cloud-wrap').hidden = false;
    field.classList.add('has-cloud-back');
    el('clinic-hub-sub').textContent = 'in Belize · packing up';
    window.setTimeout(() => {
      animating = false;
      done();
    }, 560);
  }, 700);
}

function runCheckin(done) {
  const s = STEPS.find(x => x.phase === 'checkin');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('checkin');
    done();
    return;
  }

  animating = true;
  const field = el('clinic-field');
  field.hidden = false;
  field.classList.add('is-closing', 'has-cloud-back');
  field.classList.remove('is-linked', 'is-staffed', 'is-raised', 'line-gone', 'hub-gone');
  ['clinic-spine', 'clinic-router', 'clinic-fan', 'clinic-staff', 'clinic-patient'].forEach(id => {
    el(id).hidden = true;
  });
  el('clinic-top').hidden = false;
  el('reconcile-cloud-wrap').hidden = false;
  el('clinic-hub-sub').textContent = 'in Belize · reconciling';

  const bridge = el('reconcile-bridge');
  bridge.hidden = false;
  field.classList.remove('is-reconciling');
  void bridge.offsetWidth;
  field.classList.add('is-reconciling');
  el('drill').dataset.phase = 'checkin';
  field.dataset.beat = 'checkin';

  window.setTimeout(() => {
    animating = false;
    done();
  }, 900);
}

function runHome(done) {
  const s = STEPS.find(x => x.phase === 'home');
  renderTrail(s.trail);

  if (reduceMotion()) {
    setPhase('home');
    done();
    return;
  }

  animating = true;
  const field = el('clinic-field');
  field.hidden = false;
  field.classList.add('has-cloud-back', 'is-reconciling', 'is-closing');
  el('reconcile-cloud-wrap').hidden = false;
  el('reconcile-bridge').hidden = false;
  el('clinic-top').hidden = false;

  // 1) line fades
  field.classList.add('line-gone');
  window.setTimeout(() => {
    el('reconcile-bridge').hidden = true;
    // 2) hub vanishes
    field.classList.add('hub-gone');
    window.setTimeout(() => {
      el('clinic-top').hidden = true;
      // 3) checked-out lifts
      const cloud = el('reconcile-cloud');
      cloud.classList.remove('checked-out');
      cloud.classList.add('is-home');
      el('reconcile-stamp').hidden = true;
      el('reconcile-cloud-sub').textContent = 'cloud · home';
      el('drill').dataset.phase = 'home';
      field.dataset.beat = 'home';
      window.setTimeout(() => {
        animating = false;
        done();
      }, 450);
    }, 550);
  }, 450);
}

function reset() {
  animating = false;
  step = 0;
  el('indy-map').hidden = true;
  el('indy-map').classList.remove('indy-show', 'indy-hide');
  hideFlyer();
  resetPlane();
  resetClinic();
  el('key-hub-btn')?.classList.remove('hub-collected');
  renderTrail(STEPS[0].trail);
  setPhase('solo');
  el('caption').innerHTML = STEPS[0].caption;
  el('count').textContent = `1 / ${STEPS.length}`;
  el('prev').disabled = true;
  el('next').disabled = false;
}

function next() {
  if (animating) return;
  if (step >= STEPS.length - 1) return;
  step++;
  apply(step, { animate: true });
}

function prev() {
  if (animating) return;
  if (step <= 0) { reset(); return; }
  step--;
  apply(step, { animate: false });
}

window.addEventListener('DOMContentLoaded', () => {
  el('next').addEventListener('click', next);
  el('prev').addEventListener('click', prev);
  el('reset').addEventListener('click', reset);
  el('key-christmed').addEventListener('click', () => { if (step === 0) next(); });
  el('key-cornerstone').addEventListener('click', () => { if (step === 1) next(); });
  el('key-belize').addEventListener('click', () => { if (step === 2) next(); });
  document.addEventListener('keydown', e => {
    if (e.code === 'Space' || e.code === 'ArrowRight') { e.preventDefault(); next(); }
    if (e.code === 'ArrowLeft') { e.preventDefault(); prev(); }
  });
  reset();
});
